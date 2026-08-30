using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Game controller: owns the scene tree built in code, the state machine
/// (Menu → Playing → Won → NextLevel), level setup/teardown and input
/// routing between the bug, the sweeper and the HUD.
/// </summary>
public partial class Main : Node2D
{
	private enum GameState { Menu, Playing, Won }

	private readonly RandomNumberGenerator _rng = new();

	private Node2D _ground = null!;
	private Node2D _debrisBottom = null!;
	private Node2D _debrisTop = null!;
	private Bug _bug = null!;
	private Hud _hud = null!;
	private MainMenu _menu = null!;

	// Win overlay copy, stashed until the bug's celebration finishes.
	private string _pendingWinComment = "";
	private string _pendingWinRoundLine = "";
	private Hud.WinStat[] _pendingWinStats = new Hud.WinStat[0];

	private SaveData _save = null!;
	private LevelStats _stats = new();
	private Sweeper _sweeper = null!;
	private List<Debris> _debris = new();
	private List<GustCoin> _coins = new();
	private GameState _state = GameState.Menu;
	private Vector2 _viewSize;

	/// <summary>Gold gust coins hidden below the debris each round.</summary>
	private const int GustCoinsPerLevel = 3;

	// Round-start settle pacing (see Debris.SettleIn): the diagonal sweep
	// across the floor plus per-piece jitter caps the total at ~2.5s.
	private const float SettleSweepSeconds = 1.4f;
	private const float SettleJitterSeconds = 0.25f;

	// Ambient rustle pacing: every few seconds a stray draft brushes a
	// small patch of the litter. Cosmetic only — see Debris.Rustle.
	private const float RustleIntervalMin = 4f;   // seconds between drafts (min)
	private const float RustleIntervalMax = 8f;   // seconds between drafts (max)
	private const float RustleGroupRadius = 130f; // px: pieces this close shiver together
	private const int RustleMaxPieces = 6;        // cap so dense floors stay subtle

	// The level being set up; the bug's difficulty and the stats clock are
	// only applied once the debris has finished settling into place.
	private int _activeLevel;
	private bool _awaitingSettle;

	// Ambient rustle timer: counts down to the next stray draft while a
	// round is live (seeded fresh in OnSettleFinished).
	private float _rustleCountdown;

	// Double-tap burst tuning: two dead taps inside the window and slop
	// fire a radial gust burst — a sweep without the drag.
	private const ulong DoubleTapWindowMs = 350;
	private const float DoubleTapSlop = 65f;
	private const float TapTravelSlop = 24f;

	private bool _tapArmed;       // last gesture ended as a dead tap
	private ulong _lastTapTicks;
	private Vector2 _lastTapPos;
	private bool _awaitingTapEnd; // a press is on the floor awaiting its lift
	private Vector2 _pressWorld;

	public override void _Ready()
	{
		_rng.Randomize();

		BuildTree();
		_viewSize = GetViewportRect().Size;
		FitGround();
		GetViewport().SizeChanged += OnViewportResized;

		_save = SaveData.Load();
		_sweeper = new Sweeper(() => _debris, _rng, OnSweepCompleted);

		_menu.Refresh(_save);
		SetState(GameState.Menu);

		if (OS.GetEnvironment("LEAF_AUTOPLAY") == "1")
			RunHeadlessAutoplay();
	}

	/// <summary>Headless self-test: plays a level end-to-end and verifies the save round-trip.</summary>
	private async void RunHeadlessAutoplay()
	{
		_save.Reset(); // deterministic: the test assumes a fresh save file
		StartLevel(3);

		// Round start: the debris falls in and settles before the bug and
		// the gust coins take their new hiding spots — wait for the floor
		// to dress before probing the round.
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// Restart (the dock button's own handler): must reshuffle the same
		// way a fresh round does — settle gate engaged again, then new
		// hiding spots underneath. Everything below probes the restarted
		// round, so the whole test covers the post-restart state too.
		OnRestartConfirmed();
		bool restartOk = _awaitingSettle;
		GD.Print($"AUTOPLAY restart: engaged={restartOk} level={_activeLevel}");
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// Ambient rustles: a triggered draft must shiver at least one
		// piece's sprite while every unswept node transform — the ground
		// truth behind coverage, sweeping and the gyre — stays exactly
		// where it was, then everything settles back at rest.
		var rustleProbe = new List<(Debris Piece, Vector2 From)>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling && !d.IsRidingWind)
				rustleProbe.Add((d, d.Position));
		TriggerAmbientRustle();
		bool rustleShivered = false;
		bool rustleGrounded = true;
		foreach (var (piece, from) in rustleProbe)
			if (IsInstanceValid(piece) && !piece.Swept && piece.Position != from)
				rustleGrounded = false;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && d.IsRustling)
				rustleShivered = true;
		await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
		bool rustleSettled = true;
		foreach (var (piece, from) in rustleProbe)
		{
			if (!IsInstanceValid(piece) || piece.Swept)
				continue;
			if (piece.IsRustling || piece.Position != from)
				rustleSettled = false;
		}
		bool rustleOk = rustleShivered && rustleGrounded && rustleSettled;
		GD.Print($"AUTOPLAY rustle: shivered={rustleShivered} grounded={rustleGrounded} settled={rustleSettled}");

		// Covered-bug rule: debris parked on the bug blocks selection, and
		// sweeping every overlapping piece away makes it selectable again.
		// Ground truth recomputes coverage straight from the blocker
		// texture's alpha channel — no cached mask, no shared mapping code —
		// so a mask or coordinate-mapping bug fails the run outright.
		var blocker = _debris.Find(d => IsInstanceValid(d) && !d.Swept);
		bool blocked = false;
		bool uncovered = false;
		bool truthOk = true;
		if (blocker != null)
		{
			blocker.Position = _bug.Position;
			blocked = BugIsCovered();
			// Positive: the blocker must cover the bug's occlusion area.
			// Negative: a far-away test point must not be covered by it.
			truthOk &= blocker.Covers(_bug.Position, _bug.OcclusionRadius)
				== CoversByTextureAlpha(blocker, _bug.Position, _bug.OcclusionRadius);
			Vector2 far = _bug.Position + new Vector2(400f, 0f);
			truthOk &= blocker.Covers(far, _bug.OcclusionRadius)
				== CoversByTextureAlpha(blocker, far, _bug.OcclusionRadius);
			foreach (var d in _debris)
				if (IsInstanceValid(d) && !d.Swept
					&& d.Position.DistanceTo(_bug.Position) <= _bug.OcclusionRadius + d.ExtentRadius)
					d.Fling(Vector2.Right * 2000f, _rng);
			uncovered = !BugIsCovered();
		}
		GD.Print($"AUTOPLAY uncover: blocked={blocked} cleared={uncovered} truthOk={truthOk}");

		// Double-tap burst: two dead taps at a cluttered spot fling the
		// nearby debris radially — sweep semantics (cap, free, counted)
		// without the drag. The burst center must sit clear of the bug and
		// the coins so the synthetic taps fall through to sweeping.
		Vector2? burstAt = null;
		foreach (var d in _debris)
		{
			if (!IsInstanceValid(d) || d.Swept
				|| d.Position.DistanceTo(_bug.Position) <= 300f)
				continue;
			bool nearCoin = false;
			foreach (var c in _coins)
				if (IsInstanceValid(c) && c.Position.DistanceTo(d.Position) <= 220f)
					nearCoin = true;
			if (!nearCoin)
			{
				burstAt = d.Position;
				break;
			}
		}
		bool burstOk = false;
		if (burstAt != null)
		{
			Vector2 center = burstAt.Value;
			int sweepsBefore = _stats.Sweeps;
			int sweptBefore = 0;
			int halo = 0;
			foreach (var d in _debris)
			{
				if (!IsInstanceValid(d)
					|| d.Position.DistanceTo(center) > Sweeper.BurstRadius)
					continue;
				if (d.Swept) sweptBefore++;
				else halo++;
			}
			// Tap 1 down+up, then tap 2 down+up at the same spot.
			_UnhandledInput(SyntheticTouch(true, center));
			_UnhandledInput(SyntheticTouch(false, center));
			_UnhandledInput(SyntheticTouch(true, center));
			_UnhandledInput(SyntheticTouch(false, center));
			int sweptAfter = 0;
			foreach (var d in _debris)
				if (IsInstanceValid(d) && d.Swept
					&& d.Position.DistanceTo(center) <= Sweeper.BurstRadius)
					sweptAfter++;
			// Exactly the nearest halo pieces fling (capped like a sweep) and
			// the burst counts as exactly one sweep.
			burstOk = halo > 0
				&& sweptAfter == sweptBefore + Mathf.Min(halo, Sweeper.MaxDebrisPerSweep)
				&& _stats.Sweeps == sweepsBefore + 1;
		}
		GD.Print($"AUTOPLAY burst: found={burstAt != null} ok={burstOk}");

		// Gust coins: three hide below the debris each round; collecting one
		// banks +1 power, spending a gust takes −1.
		var coin = _coins.Find(c => IsInstanceValid(c) && !c.Collected);
		bool coinSpawned = _coins.Count == GustCoinsPerLevel;
		bool coinBanked = false;
		if (coin != null)
		{
			CollectCoin(coin);
			// The power is banked only when the coin reaches the dock button.
			await ToSignal(coin, GustCoin.SignalName.CollectionFlightFinished);
			coinBanked = _save.GustPower == SaveData.StartingGustPower + 1;
		}
		GD.Print($"AUTOPLAY coins: spawned={_coins.Count} banked={coinBanked} " +
				 $"power={_save.GustPower}");

		for (int i = 0; i < 7; i++)
		{
			_stats.Tick(1.0);
			_stats.CountSweep();
		}
		OnWindPressed(); // spends one gust power and counts the use
		bool gustSpent = _save.GustPower == SaveData.StartingGustPower;
		WinLevel();

		// End-of-round wind: winning must pick up every leftover piece into
		// a clockwise gyre around the floor's center. Snapshot positions,
		// let the gyre run, then check each outer piece moved clockwise.
		int windPieces = 0;
		bool windRiding = true;
		var windSnapshot = new List<(Debris Piece, Vector2 From)>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
			{
				windPieces++;
				windRiding &= d.IsRidingWind;
				windSnapshot.Add((d, d.Position));
			}
		// The gyre eases in over ~1.8s; give it enough time to visibly move.
		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		Vector2 gyreCenter = WindCenter();
		bool windMoving = true;
		bool windClockwise = true;
		int windChecked = 0;
		foreach (var (piece, from) in windSnapshot)
		{
			// Inner pieces barely travel (arc ≈ radius × angle) — only the
			// outer ring proves the motion and its direction.
			if (!IsInstanceValid(piece) || from.DistanceTo(gyreCenter) < 200f)
				continue;
			windChecked++;
			if (from.DistanceTo(piece.Position) < 20f)
				windMoving = false;
			// Clockwise on screen (y down): the cross of the start and end
			// offsets from the center is positive when the angle increased.
			Vector2 a = from - gyreCenter;
			Vector2 b = piece.Position - gyreCenter;
			if (a.X * b.Y - a.Y * b.X <= 0f)
				windClockwise = false;
		}
		bool windOk = windPieces > 0 && windRiding
			&& (windChecked == 0 || (windMoving && windClockwise));
		GD.Print($"AUTOPLAY wind: pieces={windPieces} riding={windRiding} " +
				 $"checked={windChecked} moving={windMoving} clockwise={windClockwise}");

		// The restart probe re-runs the handler, which restarts the save's
		// current level — not the hardcoded probe level 3 the round began
		// on — so every post-win expectation keys off the actually-played
		// level instead of a constant.
		int playedLevel = _activeLevel;
		var reloaded = SaveData.Load();
		bool ok = blocked && uncovered && truthOk && burstOk && rustleOk
			&& coinSpawned && coinBanked && gustSpent && restartOk && windOk
			&& reloaded.CurrentLevel == playedLevel + 1
			&& reloaded.LevelsCleared == 1
			&& reloaded.TotalSweeps == 8
			&& reloaded.TotalGusts == 1
			&& reloaded.GustPower == SaveData.StartingGustPower
			&& reloaded.BugFindCounts.Count == 1
			&& reloaded.History.Count == 1
			&& reloaded.History[0].Level == playedLevel
			&& reloaded.History[0].Gusts == 1;

		GD.Print($"AUTOPLAY save: level={_save.CurrentLevel} cleared={_save.LevelsCleared} " +
				 $"sweeps={_save.TotalSweeps} gusts={_save.TotalGusts} " +
				 $"bugs={_save.BugFindCounts.Count} hist={_save.History.Count}");
		GD.Print($"AUTOPLAY reload: level={reloaded.CurrentLevel} cleared={reloaded.LevelsCleared} " +
				 $"sweeps={reloaded.TotalSweeps} gusts={reloaded.TotalGusts} ok={ok}");
		GetTree().Quit(ok ? 0 : 1);
	}

	public override void _Process(double delta)
	{
		_stats.Tick(delta);
		if (_awaitingSettle)
			CheckSettleFinished();
		TickAmbientRustle(delta);
	}

	/// <summary>
	/// Ambient life: every 4–8s a stray draft rustles a random patch of
	/// the litter. Purely cosmetic (Debris.Rustle only wiggles sprites)
	/// and gated to live, settled rounds so it can't interfere with the
	/// settle-in, the win wind or the autoplay probes.
	/// </summary>
	private void TickAmbientRustle(double delta)
	{
		if (_state != GameState.Playing || _awaitingSettle)
			return;
		_rustleCountdown -= (float)delta;
		if (_rustleCountdown > 0f)
			return;
		_rustleCountdown = _rng.RandfRange(RustleIntervalMin, RustleIntervalMax);
		TriggerAmbientRustle();
	}

	/// <summary>
	/// One draft: a random at-rest piece is the epicenter and its close
	/// neighbors shiver with it along the same draft direction — a gust
	/// that combs a small patch, not a popcorn field.
	/// </summary>
	private void TriggerAmbientRustle()
	{
		// Only pieces at rest take the draft: swept pieces are flying,
		// settling pieces are falling in, wind riders are mid-gyre, and
		// pieces already shivering keep their own wobble.
		var rest = new List<Debris>(_debris.Count);
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling
				&& !d.IsRidingWind && !d.IsRustling)
				rest.Add(d);
		if (rest.Count == 0)
			return;

		Debris epicenter = rest[_rng.RandiRange(0, rest.Count - 1)];
		Vector2 draft = Vector2.Right.Rotated(_rng.RandfRange(0f, Mathf.Tau));

		var group = new List<(Debris Piece, float Dist)>();
		foreach (var d in rest)
		{
			float dist = d.Position.DistanceTo(epicenter.Position);
			if (dist <= RustleGroupRadius)
				group.Add((d, dist));
		}
		// Dense floors hold far more neighbors than one draft should lift:
		// keep only the closest few so the rustle stays a flicker.
		group.Sort((a, b) => a.Dist.CompareTo(b.Dist));
		int count = Mathf.Min(group.Count, RustleMaxPieces);
		for (int i = 0; i < count; i++)
		{
			// Pieces nearer the epicenter sit deeper in the draft, and the
			// direction jitters per piece so the patch shears organically.
			float falloff = 1f - group[i].Dist / RustleGroupRadius * 0.6f;
			group[i].Piece.Rustle(
				draft.Rotated(_rng.RandfRange(-0.35f, 0.35f)), falloff, _rng);
		}
	}

	/// <summary>Unlocks play once every piece has landed from the settle-in.</summary>
	private void CheckSettleFinished()
	{
		foreach (var d in _debris)
			if (IsInstanceValid(d) && d.IsSettling)
				return;
		_awaitingSettle = false;
		OnSettleFinished();
	}

	// ------------------------------------------------------------- input --

	public override void _UnhandledInput(InputEvent @event)
	{
		// Touches stay locked while the round-start settle is in flight.
		if (_state != GameState.Playing || _awaitingSettle)
			return;

		switch (@event)
		{
			case InputEventScreenTouch { Pressed: true } touch:
			{
				Vector2 world = ToWorld(touch.Position);
				// Gold gust coins act like the bug: hidden below the debris
				// until uncovered. An uncovered coin is collected instead of
				// starting a sweep; a covered one falls through to sweeping.
				GustCoin coin = SelectableCoinAt(world);
				if (coin != null)
				{
					_tapArmed = false; // selection taps never chain into a burst
					CollectCoin(coin);
					return;
				}
				// The bug hides below the debris: while any unswept piece
				// overlaps its visible body a tap just starts sweeping there.
				if (_bug.ContainsPoint(world) && !BugIsCovered())
				{
					_tapArmed = false;
					WinLevel();
					return;
				}
				_awaitingTapEnd = true;
				_pressWorld = world;
				_sweeper.Begin(world, Time.GetTicksUsec() * 1000UL);
				break;
			}
			case InputEventScreenTouch { Pressed: false } t:
				if (t.Canceled)
				{
					_sweeper.Cancel();
					_awaitingTapEnd = false;
					_tapArmed = false;
				}
				else
				{
					_sweeper.End();
					OnTouchLifted();
				}
				break;
			case InputEventScreenDrag drag:
			{
				Vector2 world = ToWorld(drag.Position);
				// A gesture that wanders off is a sweep, not a tap: drop it
				// out of the double-tap chain.
				if (_awaitingTapEnd && _pressWorld.DistanceTo(world) > TapTravelSlop)
				{
					_awaitingTapEnd = false;
					_tapArmed = false;
				}
				_sweeper.Drag(world, Time.GetTicksUsec() * 1000UL);
				break;
			}
		}
	}

	private Vector2 ToWorld(Vector2 screenPos) =>
		GetCanvasTransform().AffineInverse() * screenPos;

	private void OnSweepCompleted()
	{
		if (_state != GameState.Playing)
			return;
		_stats.CountSweep();
		_hud.ShowSweeps(_stats.Sweeps);
	}

	/// <summary>
	/// A touch lifted: a "dead tap" (nothing selected, nothing swept, and it
	/// never wandered) arms a double-tap window; a second dead tap within
	/// <see cref="DoubleTapWindowMs"/> and <see cref="DoubleTapSlop"/> fires a
	/// radial gust burst — a sweep without the drag, for clearing the clutter
	/// around a buried item.
	/// </summary>
	private void OnTouchLifted()
	{
		if (!_awaitingTapEnd)
			return;
		_awaitingTapEnd = false;

		ulong now = Time.GetTicksMsec();
		bool deadTap = !_sweeper.SweptThisGesture;
		if (deadTap && _tapArmed
			&& now - _lastTapTicks <= DoubleTapWindowMs
			&& _lastTapPos.DistanceTo(_pressWorld) <= DoubleTapSlop)
		{
			_tapArmed = false;
			_sweeper.Burst(_pressWorld); // reports via OnSweepCompleted
			return;
		}
		_tapArmed = deadTap;
		_lastTapTicks = now;
		_lastTapPos = _pressWorld;
	}

	/// <summary>Synthetic tap for the autoplay self-test (world → screen).</summary>
	private InputEventScreenTouch SyntheticTouch(bool pressed, Vector2 world) => new()
	{
		Pressed = pressed,
		Position = GetCanvasTransform() * world,
	};

	/// <summary>
	/// Ground truth for the autoplay self-test: recomputes whether a debris
	/// piece covers a point by sampling the texture's alpha directly along
	/// rays around the test point — independent of the cached 4px mask and
	/// of <see cref="Debris.Covers"/>' mapping code.
	/// </summary>
	private static bool CoversByTextureAlpha(Debris d, Vector2 point, float radius)
	{
		Image img = d.Texture.GetImage();
		if (img == null || (img.IsCompressed() && img.Decompress() != Error.Ok))
			return false;
		float s = d.SpriteScale;
		// World offset from the piece center → texture-aligned local offset
		// (undo node rotation, undo sprite scale) → texture pixels via the
		// half-size origin of the centered sprite.
		Vector2 texPoint = (point - d.Position).Rotated(-d.Rotation) / s
			+ new Vector2(img.GetWidth(), img.GetHeight()) * 0.5f;
		float texRadius = radius / s;

		for (int i = 0; i < 16; i++)
		{
			Vector2 dir = Vector2.Right.Rotated(i * Mathf.Tau / 16f);
			for (float r = 0f; r <= texRadius; r += 2f)
			{
				Vector2 p = texPoint + dir * r;
				int x = (int)p.X;
				int y = (int)p.Y;
				if (x < 0 || y < 0 || x >= img.GetWidth() || y >= img.GetHeight())
					continue;
				if (img.GetPixel(x, y).A > Debris.AlphaThreshold)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// True while any unswept debris's opaque pixels overlap the circular
	/// area (<see cref="Debris.Covers"/>) — the pixel-accurate covered rule
	/// shared by the bug and gust coins, which both hide below the debris.
	/// </summary>
	private bool DebrisOverlaps(Vector2 pos, float radius)
	{
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && d.Covers(pos, radius))
				return true;
		return false;
	}

	/// <summary>
	/// True while any unswept debris still overlaps the bug's visible body
	/// (OcclusionRadius — much tighter than the forgiving TapRadius): the
	/// bug can only be selected (and the round won) once it's uncovered.
	/// </summary>
	private bool BugIsCovered() => DebrisOverlaps(_bug.Position, _bug.OcclusionRadius);

	/// <summary>The nearest uncovered gust coin under a tap, or null.</summary>
	private GustCoin SelectableCoinAt(Vector2 world)
	{
		GustCoin best = null;
		float bestDist = float.MaxValue;
		foreach (var c in _coins)
		{
			if (!IsInstanceValid(c) || c.Collected || !c.ContainsPoint(world))
				continue;
			// A covered coin can't be collected — like a covered bug, the
			// tap starts sweeping there instead. Coverage is judged against
			// the coin's visible disk (OcclusionRadius), not its tap area.
			if (DebrisOverlaps(c.Position, c.OcclusionRadius))
				continue;
			float dist = c.Position.DistanceSquaredTo(world);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = c;
			}
		}
		return best;
	}

	/// <summary>
	/// An uncovered gust coin was tapped: lift it onto the HUD's canvas layer
	/// so the flight passes above everything — debris and the dock itself —
	/// then let it spiral into the gust button.
	/// </summary>
	private void CollectCoin(GustCoin coin)
	{
		Vector2 screenPos = GetCanvasTransform() * coin.Position;
		coin.Reparent(_hud);
		coin.Position = screenPos;
		coin.Collect(_hud.WindButtonCenter);
	}

	/// <summary>
	/// A coin reached the gust button: bank the power, persist it, and make
	/// the counter burst as it ticks up.
	/// </summary>
	private void OnCoinCollectionFinished()
	{
		_save.GustPower++;
		_save.Save();
		_hud.ShowGustPower(_save.GustPower);
		_hud.PulseGustPower();
	}

	// ------------------------------------------------------------- setup --

	private void BuildTree()
	{
		_ground = new Node2D { Name = "Ground" };
		var groundSprite = new Sprite2D { Texture = GD.Load<Texture2D>("res://assets/textures/ground.svg") };
		groundSprite.Name = "Sprite";
		_ground.AddChild(groundSprite);
		AddChild(_ground);

		_bug = new Bug { Name = "Bug" };
		// The bug node is reused across levels, so connect the celebration
		// signal once here rather than on every win.
		_bug.CelebrationFinished += OnBugCelebrationFinished;
		AddChild(_bug);

		_debrisBottom = new Node2D { Name = "DebrisBottom" };
		// Explicit Z layering: the bug (Z 0) is always below every debris
		// piece until it's tapped and Celebrate() raises it above everything.
		_debrisBottom.ZIndex = 1;
		AddChild(_debrisBottom);

		_debrisTop = new Node2D { Name = "DebrisTop" };
		_debrisTop.ZIndex = 2;
		AddChild(_debrisTop);

		_hud = new Hud { Name = "Hud" };
		_hud.NextPressed += OnNextPressed;
		_hud.MenuPressed += OnMenuPressed;
		_hud.WindPressed += OnWindPressed;
		_hud.RestartConfirmed += OnRestartConfirmed;
		AddChild(_hud);

		_menu = new MainMenu { Name = "Menu" };
		_menu.PlayPressed += OnPlayPressed;
		_menu.NewGamePressed += OnNewGamePressed;
		AddChild(_menu);
	}

	/// <summary>Scales the ground texture to always cover the visible area.</summary>
	private void FitGround()
	{
		Rect2 view = GetViewportRect();
		var sprite = _ground.GetChild<Sprite2D>(0);
		Vector2 texSize = sprite.Texture.GetSize();
		float s = Mathf.Max(view.Size.X / texSize.X, view.Size.Y / texSize.Y) * 1.05f;
		sprite.Scale = new Vector2(s, s);
		_ground.Position = view.GetCenter();
	}

	/// <summary>
	/// Keeps the ground and a live round covering the viewport when the
	/// window dimensions change (desktop resize, rotation, split-screen).
	/// </summary>
	private void OnViewportResized()
	{
		Vector2 newSize = GetViewportRect().Size;
		if (newSize == _viewSize || newSize == Vector2.Zero)
			return;

		Vector2 oldSize = _viewSize;
		_viewSize = newSize;
		FitGround();

		if (_state == GameState.Menu)
			return;

		// Stretch the live round's layout from the old playable rect onto
		// the new one so the floor never shows bare background mid-level.
		// The dock has a fixed height, so only the free space above it
		// scales vertically.
		if (oldSize.X <= 0f || oldSize.Y <= Hud.DockHeight)
			return;
		Vector2 floorRatio = new(
			newSize.X / oldSize.X,
			(newSize.Y - Hud.DockHeight) / (oldSize.Y - Hud.DockHeight));
		// While the bug is seated on the win card it lives in the HUD and the
		// containers position it — only stretch it while it's in the world.
		bool bugInWorld = IsInstanceValid(_bug) && _bug.Visible && _bug.GetParent() == this;
		if (bugInWorld)
			_bug.Position *= floorRatio;
		Vector2 windCenter = WindCenter();
		foreach (var d in _debris)
		{
			if (!IsInstanceValid(d) || d.Swept)
				continue;
			d.Position *= floorRatio;
			// Mid-settle pieces must land on the resized floor; wind riders
			// keep circling the resized floor's center.
			d.ScaleSettle(floorRatio);
			d.SetWindCenter(windCenter);
		}
		// Coins still hiding in the debris stretch along; collected ones are
		// mid-flight toward the dock and are left alone.
		foreach (var c in _coins)
			if (IsInstanceValid(c) && !c.Collected)
				c.Position *= floorRatio;
	}

	private void ClearLevel()
	{
		_awaitingSettle = false;
		foreach (var d in _debris)
			if (IsInstanceValid(d))
				d.QueueFree();
		_debris.Clear();
		foreach (var c in _coins)
			if (IsInstanceValid(c))
				c.QueueFree();
		_coins.Clear();
		_bug.Visible = false;
	}

	private void StartLevel(int level)
	{
		ClearLevel();
		FitGround();

		// The bug may still be seated on the win card from the last round;
		// take it back into the world before the next round hides it again.
		if (_bug.GetParent() != this)
			_bug.Reparent(this);
		_bug.Visible = false;

		// The floor is everything above the HUD dock — the dock is never
		// covered by debris or the bug.
		Rect2 floor = PlayableArea();

		// Debris first: it falls in and settles, and only then do the bug
		// and the gust coins take their new random spots underneath it
		// (OnSettleFinished). Touches stay locked until the floor is set.
		_activeLevel = level;
		SpawnDebris(level, floor);
		_awaitingSettle = true;

		_hud.ShowLevel(level);
		_hud.ShowSweeps(0);
		_hud.ShowGustPower(_save.GustPower);
		_hud.HideWin();
		SetState(GameState.Playing);
	}

	/// <summary>
	/// The settle-in finished: with the debris now lying where it landed,
	/// the bug and the gust coins take their new random spots underneath
	/// and the round's clock starts.
	/// </summary>
	private void OnSettleFinished()
	{
		if (_state != GameState.Playing)
			return;

		Rect2 floor = PlayableArea();
		var bugType = BugTypes.Random();
		_bug.Setup(bugType, RoundConfig.BugScale(_activeLevel),
			RoundConfig.Camouflage(_activeLevel));
		_bug.Position = new Vector2(
			_rng.RandfRange(180f, floor.Size.X - 180f),
			_rng.RandfRange(320f, floor.Size.Y - 200f));
		_bug.Visible = true;

		SpawnGustCoins(floor);

		_stats.Start(_activeLevel);
		_rustleCountdown = _rng.RandfRange(RustleIntervalMin, RustleIntervalMax);
	}

	/// <summary>
	/// The interactive region: the full viewport minus the bottom HUD dock.
	/// Debris, the bug and the ground clamp all live inside it.
	/// </summary>
	private Rect2 PlayableArea() => new(
		Vector2.Zero,
		new Vector2(_viewSize.X, Mathf.Max(1f, _viewSize.Y - Hud.DockHeight)));

	private void SpawnDebris(int level, Rect2 floor)
	{
		// Distinct textures with a cozy mix; leaves dominate, heavier stuff sparser.
		(string path, DebrisWeight weight, int freq)[] palette =
		{
			("res://assets/textures/leaf_red.svg", DebrisWeight.Light, 16),
			("res://assets/textures/leaf_red2.svg", DebrisWeight.Light, 14),
			("res://assets/textures/leaf_yellow.svg", DebrisWeight.Light, 14),
			("res://assets/textures/leaf_green.svg", DebrisWeight.Light, 14),
			("res://assets/textures/petal_pink.svg", DebrisWeight.Light, 8),
			("res://assets/textures/petal_white.svg", DebrisWeight.Light, 7),
			("res://assets/textures/petal_purple.svg", DebrisWeight.Light, 6),
			("res://assets/textures/moss.svg", DebrisWeight.Medium, 8),
			("res://assets/textures/stick.svg", DebrisWeight.Heavy, 7),
			("res://assets/textures/rock.svg", DebrisWeight.Heavy, 3),
			("res://assets/textures/rock2.svg", DebrisWeight.Heavy, 3),
		};

		int total = 0;
		foreach (var entry in palette)
			total += entry.freq;

		// Jittered-grid placement: one slot per cell guarantees the whole floor
		// is covered evenly (no bare patches, no visible bug), while the jitter
		// keeps it from looking like a lattice. Count = floor area × coverage.
		int count = (int)(floor.Size.X * floor.Size.Y * RoundConfig.Coverage(level));
		float cell = Mathf.Sqrt(floor.Size.X * floor.Size.Y / Mathf.Max(count, 1));
		int topCount = count * 35 / 100; // 35% drawn above the rest for depth

		int placed = 0;
		for (float y = cell * 0.5f; y < floor.Size.Y && placed < count; y += cell)
		{
			for (float x = cell * 0.5f; x < floor.Size.X && placed < count; x += cell)
			{
				Vector2 pos = new(
					Mathf.Clamp(x + _rng.RandfRange(-0.45f, 0.45f) * cell, 14f, floor.Size.X - 14f),
					Mathf.Clamp(y + _rng.RandfRange(-0.45f, 0.45f) * cell, 14f, floor.Size.Y - 14f));

				int roll = _rng.RandiRange(1, total);
				(string path, DebrisWeight weight, _) = Pick(palette, roll);

				var debris = new Debris();
				debris.Setup(
					path,
					pos,
					_rng.RandfRange(0f, 360f),
					_rng.RandfRange(1.25f, 1.9f),
					weight,
					_rng);
				// Round-start entrance: drop in with a tumble, staggered
				// along the top-left → bottom-right diagonal.
				float diag = (pos.X / floor.Size.X + pos.Y / floor.Size.Y) * 0.5f;
				debris.SettleIn(_rng, diag * SettleSweepSeconds + _rng.RandfRange(0f, SettleJitterSeconds));

				_debris.Add(debris);
				(placed < topCount ? _debrisTop : _debrisBottom).AddChild(debris);
				placed++;
			}
		}
	}

	private void SpawnGustCoins(Rect2 floor)
	{
		for (int i = 0; i < GustCoinsPerLevel; i++)
		{
			var coin = new GustCoin { Name = $"GustCoin{i}" };
			coin.Setup(_rng.RandfRange(84f, 100f), _rng);
			coin.Position = CoinSpot(floor);
			// The coin banks its power when the spiral flight ends, then
			// frees itself.
			coin.CollectionFlightFinished += OnCoinCollectionFinished;
			AddChild(coin);
			_coins.Add(coin);
		}
	}

	/// <summary>A spread-out coin spot: inside the floor, away from the bug and other coins.</summary>
	private Vector2 CoinSpot(Rect2 floor)
	{
		Vector2 pos = default;
		for (int attempt = 0; attempt < 40; attempt++)
		{
			pos = new Vector2(
				_rng.RandfRange(150f, floor.Size.X - 150f),
				_rng.RandfRange(260f, floor.Size.Y - 170f));
			if (pos.DistanceTo(_bug.Position) < 280f)
				continue;
			bool tooClose = false;
			foreach (var c in _coins)
				if (c.Position.DistanceTo(pos) < 300f)
				{
					tooClose = true;
					break;
				}
			if (!tooClose)
				return pos;
		}
		return pos; // crowded floor: the last roll is good enough
	}

	private static (string, DebrisWeight, int) Pick(
		(string path, DebrisWeight weight, int freq)[] palette, int roll)
	{
		foreach (var entry in palette)
		{
			roll -= entry.freq;
			if (roll <= 0)
				return entry;
		}
		return palette[0];
	}

	// -------------------------------------------------------- game flow ---

	private void WinLevel()
	{
		if (_state != GameState.Playing)
			return;

		_stats.Stop();
		SetState(GameState.Won);

		// Copy uses pre-save history so "best" refers to earlier rounds.
		bool newBest = _save.LevelsCleared > 0 && _stats.Sweeps <= _save.BestSweeps();
		string comment = _stats.Comment(_save, _bug.Type);
		string roundLine = $"{LevelStats.FormatTime(_stats.Elapsed)} · {_stats.Sweeps} sweeps";
		if (_stats.Gusts > 0)
			roundLine += $" · {_stats.Gusts} gust{(_stats.Gusts == 1 ? "" : "s")}";
		_save.RecordClear(_stats.Level, _stats.Sweeps, (int)_stats.Elapsed, _bug.Type.Id, _stats.Gusts);

		// Lifetime cells for the card's stats row (post-save, so this find counts).
		BugType type = _bug.Type;
		_save.BugFindCounts.TryGetValue(type.Id, out int speciesFinds);
		var stats = new[]
		{
			new Hud.WinStat(_save.BestSweeps().ToString(),
				newBest ? "New best round!" : "Best round", newBest),
			new Hud.WinStat($"×{speciesFinds}",
				speciesFinds == 1 ? $"First {type.DisplayName}!" : type.DisplayName,
				speciesFinds == 1),
			new Hud.WinStat(_save.LevelsCleared.ToString(), "Bugs found", false),
		};

		// The bug pops above the debris, grows, then flies to the screen
		// center; the win card seats it below the title when it arrives.
		_bug.Celebrate(_viewSize / 2f);
		PetalSparkle();
		// The round is over: whatever is still on the floor gets picked up
		// by a clockwise wind and keeps circling while the card is up.
		StartEndRoundWind();
		// The win overlay waits for the bug's golden moment.
		_pendingWinComment = comment;
		_pendingWinRoundLine = roundLine;
		_pendingWinStats = stats;
	}

	private void OnBugCelebrationFinished()
	{
		if (_state == GameState.Won)
			_hud.ShowWin(_pendingWinComment, _pendingWinRoundLine, _pendingWinStats, _bug);
	}

	/// <summary>
	/// Lifts every leftover piece into the end-of-round wind gyre: a slow
	/// clockwise swirl around the floor's center that keeps the litter
	/// gently airborne while the win card is up.
	/// </summary>
	private void StartEndRoundWind()
	{
		Vector2 center = WindCenter();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				d.StartEndRoundWind(center, _rng);
	}

	/// <summary>
	/// The gyre's center: the middle of the playable floor (the dock is
	/// never part of the round).
	/// </summary>
	private Vector2 WindCenter() =>
		new(_viewSize.X / 2f, Mathf.Max(1f, _viewSize.Y - Hud.DockHeight) / 2f);

	/// <summary>Blows away 25% of the remaining debris with a gusty fling.</summary>
	private void OnWindPressed()
	{
		if (_state != GameState.Playing || _awaitingSettle)
			return;

		var alive = new List<Debris>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				alive.Add(d);
		if (alive.Count == 0)
			return;

		// Spend one gust from the persistent power counter; the button is
		// disabled at zero, but guard anyway so nothing can go negative.
		if (_save.GustPower <= 0)
			return;
		_save.GustPower--;
		_save.Save();
		_hud.ShowGustPower(_save.GustPower);

		// A gust is only spent once it actually blows anything away.
		_stats.CountGust();

		// Shuffle a copy so the ~25% sample is scattered across the floor,
		// not clustered in one grid region.
		for (int i = alive.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(alive[i], alive[j]) = (alive[j], alive[i]);
		}

		int count = Mathf.Max(1, alive.Count / 4);
		Vector2 dir = Vector2.Right.Rotated(_rng.RandfRange(0f, Mathf.Tau));
		for (int i = 0; i < count; i++)
			alive[i].Fling(dir * _rng.RandfRange(1500f, 2200f), _rng);

		WindGust(dir);
	}

	/// <summary>White streaks sweeping across the floor sell the gust.</summary>
	private void WindGust(Vector2 dir)
	{
		Rect2 view = GetViewportRect();
		Vector2 perp = dir.Orthogonal();
		float reach = view.Size.Length() * 0.5f + 240f;

		for (int i = 0; i < 8; i++)
		{
			Vector2 start = view.GetCenter() - dir * reach
				+ perp * _rng.RandfRange(-0.6f, 0.6f) * view.Size.Y;
			var streak = new Line2D
			{
				Width = _rng.RandfRange(3f, 8f),
				DefaultColor = new Color(1f, 1f, 1f, 0.55f),
				Position = start,
				Rotation = dir.Angle(),
				// Ride above the debris layers (Z 1/2) while blowing.
				ZIndex = 3,
				Points = new[] { Vector2.Zero, Vector2.Right * _rng.RandfRange(140f, 320f) },
				BeginCapMode = Line2D.LineCapMode.Round,
				EndCapMode = Line2D.LineCapMode.Round,
			};
			AddChild(streak);

			var tween = CreateTween().SetParallel();
			tween.TweenProperty(streak, "position", start + dir * reach * 2.2f, 0.55f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(streak, "modulate:a", 0f, 0.55f);
			tween.Chain().TweenCallback(Callable.From(streak.QueueFree));
		}
	}

	private void OnRestartConfirmed()
	{
		if (_state != GameState.Playing)
			return;
		StartLevel(_save.CurrentLevel);
	}

	private void PetalSparkle()
	{
		var petalTex = GD.Load<Texture2D>("res://assets/textures/petal_pink.svg");
		for (int i = 0; i < 10; i++)
		{
			var sparkle = new Sprite2D { Texture = petalTex, Position = _bug.Position, ZIndex = 3 };
			sparkle.Scale = Vector2.One * _rng.RandfRange(0.35f, 0.6f);
			AddChild(sparkle);

			Vector2 target = sparkle.Position +
				Vector2.Right.Rotated(_rng.RandfRange(0f, Mathf.Tau)) * _rng.RandfRange(120f, 320f);
			var tween = CreateTween().SetParallel();
			tween.TweenProperty(sparkle, "position", target, 0.9f)
				.SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sparkle, "rotation", _rng.RandfRange(-2f, 2f), 0.9f);
			tween.TweenProperty(sparkle, "modulate:a", 0f, 0.9f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.Chain().TweenCallback(Callable.From(sparkle.QueueFree));
		}
	}

	private void SetState(GameState state)
	{
		_state = state;
		_menu.Visible = state == GameState.Menu;
		_hud.SetDockVisible(state != GameState.Menu);
		if (state == GameState.Menu)
		{
			ClearLevel();
			_menu.Refresh(_save);
		}
	}

	private void OnPlayPressed() => StartLevel(_save.CurrentLevel);

	private void OnNewGamePressed()
	{
		_save.Reset();
		StartLevel(1);
	}

	private void OnNextPressed() => StartLevel(_save.CurrentLevel);

	private void OnMenuPressed()
	{
		_hud.HideWin();
		SetState(GameState.Menu);
	}
}
