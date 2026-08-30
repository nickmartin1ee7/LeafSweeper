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
	private BugBook _book = null!;
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
	private StormOverlay _storm = null!;
	private StormWarn _warn = null!;

	// The "Prismatic" banner that rides out a prismatic find — the storm
	// sign's mirror image (see PrismaticSign).
	private PrismaticSign _prismaticSign = null!;
	private GameState _state = GameState.Menu;
	private Vector2 _viewSize;

	// Storm round state: the weather flag follows the level (see StartLevel).
	// Every cleared patch remembers the moment it comes due — a swept patch
	// only stays clean until its own timer runs out, then fresh debris
	// falls back onto it. Oldest spots fall out of memory first.
	private bool _isStormRound;
	private readonly List<StormSpot> _clearedSpots = new();

	// Storm flood state: on top of the per-spot restoration, every 4–6s a
	// cluster of fresh debris tumbles onto random floor spots — the storm
	// doesn't just re-litter swept ground, it piles new litter on. The
	// flood abates for the round once the floor holds the cap (see
	// StormFloodCapMultiplier).
	private int _roundStartDebris;          // live pieces when the round began
	private float _clusterCountdown;        // seconds to the next cluster drop
	private bool _floodDone;                // cap reached: clusters stop
	private int _clusterPiecesDropped;      // diagnostic total for the self-test

	/// <summary>A cleared patch of floor and when its storm replacement comes due.</summary>
	private readonly record struct StormSpot(Vector2 Pos, float DueAt);

	// Round-start settle pacing (see Debris.SettleIn): the diagonal sweep
	// across the floor plus per-piece jitter caps the total at ~2.5s.
	private const float SettleSweepSeconds = 1.4f;
	private const float SettleJitterSeconds = 0.25f;

	// Ambient rustle pacing: every couple of seconds a stray draft brushes
	// a small patch of the litter. Cosmetic only — see Debris.Rustle.
	private const float RustleIntervalMin = 2f;   // seconds between drafts (min)
	private const float RustleIntervalMax = 4f;   // seconds between drafts (max)
	private const float RustleGroupRadius = 130f; // px: pieces this close shiver together
	private const int RustleClusterMin = 4;       // pieces per draft (min)
	private const int RustleClusterMax = 7;       // pieces per draft (max)

	// Storm pacing: on storm rounds every swept or gust-cleared patch is
	// re-littered 4–6 seconds after it was cleared, one fresh piece per
	// cleared piece — from the player's view, swept ground never stays
	// clean for long and the storm floor never thins out. StormSpotsCap is
	// how many patches the floor remembers.
	private const float StormSpotDelayMin = 4f; // clean-patch lifetime (s, min)
	private const float StormSpotDelayMax = 6f; // clean-patch lifetime (s, max)
	private const int StormSpotsCap = 400;      // remembered cleared spots (max)

	// Storm flood: independent of the swept-ground restoration, each 4–6s
	// gust dumps a whole cluster (6–12 pieces) of brand-new litter onto
	// random spots — swept ground starts *growing*, not just returning.
	// StormFloodCapMultiplier is how much litter the storm piles up before
	// it relents: 3× the round's starting litter makes a long storm round
	// feel like the floor is actively drowning you without ever becoming
	// infinite; spot restoration keeps going after the cap so swept
	// patches never stay clean.
	private const int StormClusterMin = 6;         // pieces per cluster (min)
	private const int StormClusterMax = 12;        // pieces per cluster (max)
	private const int StormFloodCapMultiplier = 3; // cap = this × round start

	// Menu gyre density (pieces per px²): a mid-round litter so the home
	// screen reads as "the floor, alive" without competing with the card.
	private const float MenuDebrisCoverage = 0.00065f;

	// Menu gyre speed relative to the end-of-round wind: an idle backdrop,
	// not a celebration — slow enough that pieces take ~30s+ per lap.
	private const float MenuWindSpeedScale = 0.35f;

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
		_sweeper = new Sweeper(() => _debris, _rng, OnSweepCompleted,
			RecordClearedSpot);

		_menu.Refresh(_save);
		SetState(GameState.Menu);

		if (OS.GetEnvironment("LEAF_AUTOPLAY") == "1")
			RunHeadlessAutoplay();
	}

	/// <summary>Headless self-test: plays a level end-to-end and verifies the save round-trip.</summary>
	private async void RunHeadlessAutoplay()
	{
		_save.Reset(); // deterministic: the test assumes a fresh save file
		_forcePrismatic = true; // this round must roll the rare prismatic bug
		_forceStorm = true; // this round must run the storm weather too

		// Home screen: the menu spawns a decorative litter lifted straight
		// into the gyre — it must exist and be riding before play begins,
		// and the storm weather must stay off the menu entirely.
		bool menuOk = _debris.Count > 0
			&& _debris.TrueForAll(d => IsInstanceValid(d) && d.IsRidingWind)
			&& !_storm.Active && _storm.Intensity == 0f;
		GD.Print($"AUTOPLAY menu: pieces={_debris.Count} riding={menuOk} " +
				 $"stormIdle={!_storm.Active}");

		// Catalog: 39 species × 4 variants, legacy save keys intact, every
		// texture resolves, and the id lookup round-trips.
		bool texturesOk = true;
		foreach (var v in BugTypes.AllVariants)
			if (!FileAccess.FileExists(v.TexturePath))
				texturesOk = false;
		bool catalogOk = BugTypes.All.Length == 39
			&& BugTypes.AllVariants.Length == 156
			&& BugTypes.VariantById("ladybug").DisplayName == "Ladybug"
			&& BugTypes.VariantById("ladybug_yellow").DisplayName == "Yellow Ladybug"
			&& BugTypes.VariantById("aphid_pink").Species.Id == "aphid"
			&& texturesOk;
		var catalogPick = BugTypes.RandomVariant();
		GD.Print($"AUTOPLAY catalog: species={BugTypes.All.Length} " +
				 $"variants={BugTypes.AllVariants.Length} ok={catalogOk} " +
				 $"pick={catalogPick.Id}");

		// Book model on a fresh save: all 156 entries present and unknown.
		var bookBefore = new BugBookModel(_save);
		bool bookBeforeOk = bookBefore.Entries.Count == 156
			&& bookBefore.FoundVariants == 0
			&& bookBefore.TotalBugs == 0
			&& bookBefore.Favorite == "—";

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

		// Prismatic round: the forced roll must have produced a prismatic
		// bug (rainbow shader, no camouflage) — the grandiose win and the
		// flare are checked after the win below.
		bool prismaticSpawn = _bug.IsPrismatic;
		GD.Print($"AUTOPLAY prismatic: spawned={prismaticSpawn}");

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

		// Gust coins: every round hides RoundConfig.GustCoinsForLevel coins
		// under the debris — one on normal rounds, three on storm rounds
		// (the flood keeps re-burying ground, so storms pay in gusts).
		// Collecting one banks +1 power, spending a gust takes −1.
		var coin = _coins.Find(c => IsInstanceValid(c) && !c.Collected);
		bool coinSpawned = _coins.Count == RoundConfig.GustCoinsForLevel(_activeLevel);
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

		// Storm rounds: the weather must be on, and the gust above recorded
		// the spots it vacated through the real sweep path. Every cleared
		// patch must re-litter itself when its own 4–6s timer comes due,
		// and be consumed from the pool. On the same cadence the flood
		// dumps cluster drops of brand-new debris onto random spots —
		// those pieces land off the recorded ground on purpose.
		bool stormEngaged = _storm.Active && _storm.Intensity > 0f;
		int spotsBefore = _clearedSpots.Count;
		var spotPool = new List<Vector2>();
		foreach (var s in _clearedSpots)
			spotPool.Add(s.Pos);
		var knownPieces = new HashSet<Debris>();
		foreach (var d in _debris)
			if (IsInstanceValid(d))
				knownPieces.Add(d);
		// Wait out the patch timers (4–6s) plus the tumble-in so every due
		// drop has landed before the judgment; the flood fires at least
		// once in that window too (its cadence is the same 4–6s).
		await ToSignal(GetTree().CreateTimer(8.0), SceneTreeTimer.SignalName.Timeout);
		int stormDrops = 0;
		int floodPieces = 0;
		foreach (var d in _debris)
		{
			if (!IsInstanceValid(d) || knownPieces.Contains(d) || d.Swept)
				continue;
			// Spot restorations land exactly on their remembered ground;
			// flood pieces land on random spots. The tight tolerance keeps
			// the two apart — a random spot never reproduces a recorded
			// position, a restoration always does.
			bool onClearedSpot = false;
			for (int i = 0; i < spotPool.Count; i++)
				if (spotPool[i].DistanceTo(d.Position) <= 0.5f)
				{
					spotPool.RemoveAt(i);
					onClearedSpot = true;
					break;
				}
			if (onClearedSpot) stormDrops++;
			else floodPieces++;
		}
		bool stormOk = stormEngaged && stormDrops > 0
			&& _clearedSpots.Count == spotsBefore - stormDrops;
		GD.Print($"AUTOPLAY storm: engaged={stormEngaged} drops={stormDrops} " +
				 $"onCleared={stormDrops == spotsBefore - _clearedSpots.Count} " +
				 $"spots={spotsBefore}->{_clearedSpots.Count}");

		// The flood must be engaged: at least one full cluster of fresh
		// debris landed during the window, growing the litter.
		bool floodOk = floodPieces >= StormClusterMin;
		GD.Print($"AUTOPLAY flood: pieces={floodPieces} " +
				 $"min={StormClusterMin} live={LiveDebrisCount()} ok={floodOk}");

		// The cap: shrink the round's remembered starting litter so the 3×
		// cap sits just under the live count, re-arm one cluster event, and
		// prove the flood stops — not one fresh piece may appear (the
		// spot pool is empty here, so any new unswept piece would be flood).
		// The flood must still be armed when the poke happens and end up
		// latched after it, so the probe proves the cap branch itself ran
		// instead of sliding through on an earlier latch.
		var knownPiecesAfterFlood = new HashSet<Debris>();
		foreach (var d in _debris)
			if (IsInstanceValid(d))
				knownPiecesAfterFlood.Add(d);
		int floodPiecesBeforeCap = _clusterPiecesDropped;
		bool floodWasArmed = !_floodDone;
		_roundStartDebris = Mathf.Max(1,
			(LiveDebrisCount() - 1) / StormFloodCapMultiplier);
		_clusterCountdown = StormSpotDelayMin;
		await ToSignal(GetTree().CreateTimer(7.0), SceneTreeTimer.SignalName.Timeout);
		int cappedFlood = 0;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !knownPiecesAfterFlood.Contains(d) && !d.Swept)
				cappedFlood++;
		bool capOk = floodWasArmed && _floodDone
			&& cappedFlood == 0
			&& _clusterPiecesDropped == floodPiecesBeforeCap;
		GD.Print($"AUTOPLAY flood cap: extra={cappedFlood} " +
				 $"dropped={floodPiecesBeforeCap}->{_clusterPiecesDropped} " +
				 $"armed={floodWasArmed} latched={_floodDone} ok={capOk}");

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
		// The warning sign must be up during this end-round: autoplay
		// forced the storm, so the round AFTER this one is stormy too.
		bool warnShown = _warn.Visible;
		bool warnOk = warnShown == NextRoundIsStorm();
		// The prismatic banner must be up during this same end-round: the
		// autoplay's round rolled the prismatic bug, so the shiny sign
		// rides out after the round it crowned.
		bool prismSignShown = _prismaticSign.Visible;
		GD.Print($"AUTOPLAY wind: pieces={windPieces} riding={windRiding} " +
				 $"checked={windChecked} moving={windMoving} clockwise={windClockwise} " +
				 $"warn={warnShown} warnOk={warnOk} prismSign={prismSignShown}");

		// The restart probe re-runs the handler, which restarts the save's
		// current level — not the hardcoded probe level 3 the round began
		// on — so every post-win expectation keys off the actually-played
		// level instead of a constant.
		int playedLevel = _activeLevel;
		// The book model must now show exactly one found entry, named like
		// the variant that was just found and counted once.
		var bookAfter = new BugBookModel(_save);
		BugBookModel.Entry? foundEntry = null;
		foreach (var e in bookAfter.Entries)
			if (e.Found)
				foundEntry = e;
		bool bookAfterOk = bookAfter.FoundVariants == 1
			&& bookAfter.TotalBugs == 1
			&& foundEntry != null
			&& foundEntry.Variant.Id == _bug.Variant.Id
			&& foundEntry.DisplayName == _bug.Variant.DisplayName
			&& foundEntry.Label == $"{_bug.Variant.DisplayName} (x1)";

		var reloaded = SaveData.Load();
		bool ok = blocked && uncovered && truthOk && burstOk && rustleOk
			&& coinSpawned && coinBanked && gustSpent && restartOk && windOk
			&& menuOk && stormOk && floodOk && capOk
			&& reloaded.CurrentLevel == playedLevel + 1
			&& reloaded.LevelsCleared == 1
			&& reloaded.TotalSweeps == 8
			&& reloaded.TotalGusts == 1
			&& reloaded.GustPower == SaveData.StartingGustPower
			&& reloaded.BugFindCounts.Count == 1
			&& reloaded.BugFindCounts.ContainsKey(_bug.Variant.Id)
			&& reloaded.History.Count == 1
			&& reloaded.History[0].Level == playedLevel
			&& reloaded.History[0].Gusts == 1
			&& reloaded.PrismaticFinds == 1
			&& prismaticSpawn && _flareSeen && _grandWinShown
			&& prismSignShown
			&& catalogOk
			&& bookBeforeOk
			&& bookAfterOk;

		// Storm round economy: a storm level hides StormGustCoins while the
		// normal round above hid one. Spawn the first storm level fresh and
		// count what actually landed under the litter. This must run after
		// every check above: the probe is the Next-button path with a fresh
		// round, whose settle re-rolls the bug variant the book/find checks
		// read. The save is left alone (StartLevel doesn't persist) and the
		// autoplay resets it on every run anyway.
		StartLevel(RoundConfig.StormFirstLevel);
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		int stormExpected = RoundConfig.GustCoinsForLevel(RoundConfig.StormFirstLevel);
		bool stormCoinsOk = _coins.Count == stormExpected
			&& stormExpected == RoundConfig.StormGustCoins;
		GD.Print($"AUTOPLAY coins-storm: level={RoundConfig.StormFirstLevel} " +
				 $"spawned={_coins.Count} expected={stormExpected} ok={stormCoinsOk}");
		ok &= stormCoinsOk;

		// Prismatic banner linger: starting the round after a prismatic
		// find holds the banner up for 2s, then dissolves it over 4s —
		// the storm sign's ride in reverse. Replays the StartLevel path
		// directly and awaits real frames. The mid-fade alpha check exists
		// because the banner is painted wholly by prismatic_sign.gdshader:
		// if the shader ever stops multiplying the tweened modulate into
		// its output, the fade silently turns back into a hard cut while
		// Visible still behaves.
		_prismaticSign.ShowSign();
		_prismaticSign.LingerThenFade();
		bool prismUp = _prismaticSign.Visible;
		double prismT = 0;
		while (prismT < 1.2) // inside the 2s hold: must still be up
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			prismT += GetProcessDeltaTime();
		}
		bool prismHeld = _prismaticSign.Visible;
		while (prismT < 3.0 && _prismaticSign.Visible) // fade starts at 2s
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			prismT += GetProcessDeltaTime();
		}
		bool prismFading = _prismaticSign.Visible && _prismaticSign.FadeAlpha < 1f;
		float prismAlpha = _prismaticSign.FadeAlpha;
		while (_prismaticSign.Visible && prismT < 7.5) // hold 2s + fade 4s + slack
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			prismT += GetProcessDeltaTime();
		}
		bool prismGone = !_prismaticSign.Visible;
		bool prismLingerOk = prismUp && prismHeld && prismFading && prismGone;
		GD.Print($"AUTOPLAY prismatic-sign: held={prismHeld} fading={prismFading} " +
				 $"alpha={prismAlpha:F2} gone={prismGone} ok={prismLingerOk}");
		ok &= prismLingerOk;

		// Storm warn linger: starting the warned-for round holds the sign
		// up for 2s, then dissolves it over 4s. Replays the StartLevel
		// path directly and awaits real frames — touches nothing the
		// checks above read, so it can run before the final prints. The
		// mid-fade alpha check exists because the sign is painted wholly
		// by warn_sparks.gdshader: if the shader ever stops multiplying
		// the tweened modulate into its output, the fade silently turns
		// back into a hard cut while Visible still behaves.
		_warn.ShowWarning();
		_warn.LingerThenFade();
		bool lingerUp = _warn.Visible;
		double lingerT = 0;
		while (lingerT < 1.2) // inside the 2s hold: must still be up
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			lingerT += GetProcessDeltaTime();
		}
		bool lingerHeld = _warn.Visible;
		while (lingerT < 3.0 && _warn.Visible) // fade starts at 2s: wait into it
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			lingerT += GetProcessDeltaTime();
		}
		bool lingerFading = _warn.Visible && _warn.FadeAlpha < 1f;
		float lingerAlpha = _warn.FadeAlpha;
		while (_warn.Visible && lingerT < 7.5) // hold 2s + fade 4s + slack
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			lingerT += GetProcessDeltaTime();
		}
		bool lingerGone = !_warn.Visible;
		bool lingerOk = lingerUp && lingerHeld && lingerFading && lingerGone;
		GD.Print($"AUTOPLAY warn-linger: held={lingerHeld} fading={lingerFading} " +
				 $"alpha={lingerAlpha:F2} gone={lingerGone} ok={lingerOk}");
		ok &= lingerOk;

		GD.Print($"AUTOPLAY save: level={_save.CurrentLevel} cleared={_save.LevelsCleared} " +
				 $"sweeps={_save.TotalSweeps} gusts={_save.TotalGusts} " +
				 $"bugs={_save.BugFindCounts.Count} hist={_save.History.Count}");
		GD.Print($"AUTOPLAY reload: level={reloaded.CurrentLevel} cleared={reloaded.LevelsCleared} " +
				 $"sweeps={reloaded.TotalSweeps} gusts={reloaded.TotalGusts} ok={ok}");
		GD.Print($"AUTOPLAY book: entries={bookAfter.Entries.Count} " +
				 $"beforeOk={bookBeforeOk} afterOk={bookAfterOk} " +
				 $"label={(foundEntry?.Label ?? "none")}");
		GD.Print($"AUTOPLAY prismatic win: flare={_flareSeen} grand={_grandWinShown} " +
				 $"found={reloaded.PrismaticFinds}");
		GetTree().Quit(ok ? 0 : 1);
	}

	public override void _Process(double delta)
	{
		_stats.Tick(delta);
		if (_awaitingSettle)
			CheckSettleFinished();
		TickAmbientRustle(delta);
		if (_isStormRound && _state == GameState.Playing && !_awaitingSettle)
		{
			TickStormDrops(delta);
			TickClusterDrops(delta);
		}
	}

	/// <summary>
	/// Ambient life: every 2–4s a stray draft rustles a random cluster of
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
		// Each draft lifts a random handful — about 4–7 pieces, closest
		// first — so the rustle stays a localized flicker even on dense
		// floors; sparse patches simply rustle smaller.
		group.Sort((a, b) => a.Dist.CompareTo(b.Dist));
		int count = Mathf.Min(group.Count,
			_rng.RandiRange(RustleClusterMin, RustleClusterMax));
		for (int i = 0; i < count; i++)
		{
			// Pieces nearer the epicenter sit deeper in the draft, and the
			// direction jitters per piece so the patch shears organically.
			float falloff = 1f - group[i].Dist / RustleGroupRadius * 0.6f;
			group[i].Piece.Rustle(
				draft.Rotated(_rng.RandfRange(-0.35f, 0.35f)), falloff, _rng);
		}
	}

	/// <summary>
	/// Storm weather rhythm: each remembered cleared patch re-litters itself
	/// the moment its own 4–6s timer runs out — a swept patch only stays
	/// clean for a few seconds on a storm round. Gated to live, settled
	/// rounds like the ambient rustle.
	/// </summary>
	private void TickStormDrops(double delta)
	{
		float now = StormNow;
		for (int i = _clearedSpots.Count - 1; i >= 0; i--)
		{
			if (_clearedSpots[i].DueAt > now)
				continue;
			DropStormDebris(_clearedSpots[i].Pos);
			_clearedSpots.RemoveAt(i);
		}
	}

	/// <summary>Monotonic engine clock the patch timers run on (seconds).</summary>
	private float StormNow => Time.GetTicksMsec() / 1000f;

	/// <summary>
	/// One storm drop: fresh debris tumbles back down onto a remembered
	/// cleared patch. The spot is consumed — once debris sits there again
	/// it is unswept ground, and it can only rejoin the pool by being
	/// swept once more.
	/// </summary>
	private void DropStormDebris(Vector2 spot)
	{
		Rect2 floor = PlayableArea();

		// A viewport resize mid-round can leave a remembered spot outside
		// the new playable rect; clamp it back onto the floor.
		Vector2 pos = new(
			Mathf.Clamp(spot.X, 14f, floor.Size.X - 14f),
			Mathf.Clamp(spot.Y, 14f, floor.Size.Y - 14f));

		SpawnStormDebris(pos);
	}

	/// <summary>
	/// The shared storm spawn path: one fresh unswept piece tumbles down
	/// onto <paramref name="pos"/> (SettleIn) and lands wherever it may —
	/// fresh debris follows the normal overlap rules, so it can re-cover
	/// the bug and the gust coins.
	/// </summary>
	private void SpawnStormDebris(Vector2 pos)
	{
		Debris debris = CreateDebris(pos);
		debris.SettleIn(_rng, _rng.RandfRange(0f, 0.3f));
		_debris.Add(debris);
		(_rng.Randf() < 0.35f ? _debrisTop : _debrisBottom).AddChild(debris);
	}

	/// <summary>
	/// Storm flood rhythm: every 4–6s a gust dumps a whole cluster (6–12
	/// pieces) of brand-new litter onto random floor spots — debris that
	/// was never swept, so the storm escalates instead of just undoing
	/// progress. Once the live debris count reaches the cap (3× the
	/// round's starting litter) the flood abates for the round; the
	/// per-spot restoration above keeps going regardless. Gated to live,
	/// settled rounds like the spot drops.
	/// </summary>
	private void TickClusterDrops(double delta)
	{
		if (_floodDone)
			return;
		_clusterCountdown -= (float)delta;
		if (_clusterCountdown > 0f)
			return;
		_clusterCountdown = _rng.RandfRange(StormSpotDelayMin, StormSpotDelayMax);

		int cap = _roundStartDebris * StormFloodCapMultiplier;
		int room = cap - LiveDebrisCount();
		if (room <= 0)
		{
			_floodDone = true; // the floor is as flooded as it gets
			return;
		}
		// The final cluster is truncated to the room left, so the cap is
		// exact and the flood tapers out instead of overshooting.
		DropStormCluster(Mathf.Min(
			_rng.RandiRange(StormClusterMin, StormClusterMax), room));
	}

	/// <summary>One cluster drop: <paramref name="count"/> fresh pieces tumble onto random floor spots.</summary>
	private void DropStormCluster(int count)
	{
		Rect2 floor = PlayableArea();
		for (int i = 0; i < count; i++)
		{
			Vector2 pos = new(
				_rng.RandfRange(14f, floor.Size.X - 14f),
				_rng.RandfRange(14f, floor.Size.Y - 14f));
			SpawnStormDebris(pos);
			_clusterPiecesDropped++;
		}
	}

	/// <summary>Pieces actually on the floor: swept ones fly away and freed ones are gone.</summary>
	private int LiveDebrisCount()
	{
		int count = 0;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				count++;
		return count;
	}

	/// <summary>
	/// A piece was swept (drag, burst or gust): remember the ground it
	/// vacated so storm rounds can drop fresh debris back onto it. The pool
	/// is capped — the oldest spots fall out of memory first.
	/// </summary>
	private void RecordClearedSpot(Debris debris)
	{
		if (_clearedSpots.Count >= StormSpotsCap)
			_clearedSpots.RemoveAt(0);
		// A piece swept mid-tumble was intercepted before it ever landed:
		// the ground it was falling toward is the clean spot, not wherever
		// it happened to hang in the air.
		Vector2 pos = debris.IsSettling ? debris.SettleTarget : debris.Position;
		_clearedSpots.Add(new StormSpot(pos,
			StormNow + _rng.RandfRange(StormSpotDelayMin, StormSpotDelayMax)));
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

		// Explicit canvas-layer ladder (Godot draws same-layer CanvasLayers
		// in non-deterministic order, so each owns a distinct index):
		// world 0 → storm 1 → menu 2 → hud 3 → warn 4 → prismatic 5 →
		// bug book 90. The storm veil and rain sit above the floor but
		// below every UI.
		_storm = new StormOverlay { Name = "Storm" };
		AddChild(_storm);

		// The "Storm Round" warning sign lives above the HUD (layer 4) so
		// its sparks never dim under the storm veil nor sit under UI.
		_warn = new StormWarn { Name = "StormWarn" };
		AddChild(_warn);

		// The "Prismatic" banner rides out a prismatic find the same way,
		// one rung above the storm sign so both can show at once (a
		// prismatic round immediately before a storm round seats the
		// banner just below the storm cloud — see PrismaticSign).
		_prismaticSign = new PrismaticSign { Name = "PrismaticSign" };
		AddChild(_prismaticSign);

		_hud = new Hud { Name = "Hud" };
		_hud.Layer = 3;
		_hud.NextPressed += OnNextPressed;
		_hud.MenuPressed += OnMenuPressed;
		_hud.WindPressed += OnWindPressed;
		_hud.RestartConfirmed += OnRestartConfirmed;
		AddChild(_hud);

		// The bug book sits on its own canvas layer above the HUD; its
		// overlay swallows every tap while open, so no other input guard
		// is needed while the player browses their collection.
		_book = new BugBook { Name = "BugBook" };
		_hud.BookPressed += () => _book.Open(_save);
		AddChild(_book);

		_menu = new MainMenu { Name = "Menu" };
		_menu.Layer = 2;
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
		{
			// The menu gyre orbits a fixed center; re-center it on the
			// new screen and leave the decorative litter where it rides.
			foreach (var d in _debris)
				if (IsInstanceValid(d) && d.IsRidingWind)
					d.SetWindCenter(WindCenter());
			return;
		}

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
		_clearedSpots.Clear();
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
		_isStormRound = _forceStorm
			|| OS.GetEnvironment("LEAF_STORM") == "1"
			|| RoundConfig.IsStormLevel(level);
		if (_isStormRound)
			_storm.FadeIn();
		else
			_storm.FadeOut();
		SpawnDebris(level, floor);
		_roundStartDebris = _debris.Count;
		_clusterCountdown = _rng.RandfRange(StormSpotDelayMin, StormSpotDelayMax);
		_floodDone = false;
		_awaitingSettle = true;

		_hud.ShowLevel(level);
		_hud.ShowSweeps(0);
		// INITIAL_GUSTS=<n> testing hook: top the persistent gust power up
		// to <n> at every round start, so manual playtests can spend gusts
		// freely without first banking gust coins (spends still write the
		// save — the top-up simply re-applies each round).
		if (int.TryParse(OS.GetEnvironment("INITIAL_GUSTS"), out int forcedGusts)
			&& forcedGusts > 0)
			_save.GustPower = forcedGusts;
		_hud.ShowGustPower(_save.GustPower);
		_hud.HideWin();
		// The sign warned about THIS round starting: let it ride the storm
		// round's opening (2s hold + 4s dissolve) instead of vanishing at
		// once. Normal rounds and menu returns hide it immediately.
		if (_warn.Visible && RoundConfig.IsStormLevel(level))
			_warn.LingerThenFade();
		else
			_warn.HideWarning();
		// Same ride for the prismatic banner: a round that follows a
		// prismatic find holds it over the opening, then lets it dissolve.
		if (_prismaticSign.Visible)
			_prismaticSign.LingerThenFade();
		SetState(GameState.Playing);
	}

	/// <summary>
	/// The settle-in finished: with the debris now lying where it landed,
	/// the bug and the gust coins take their new random spots underneath
	/// and the round's clock starts.
	/// </summary>
	/// <summary>
	/// Chance a fresh round hides a prismatic (rainbow sparkle) bug.
	/// Deliberately very rare — about one round in a hundred — so the find
	/// stays a story worth telling. LEAF_PRISMATIC=1 forces it for manual
	/// testing; autoplay forces it too.
	/// </summary>
	private const float PrismaticChance = 0.01f;

	private bool _forcePrismatic;
	private bool _forceStorm;
	private bool _flareSeen;
	private bool _grandWinShown;
	private SunFlare? _sunFlare;

	private void OnSettleFinished()
	{
		if (_state != GameState.Playing)
			return;

		Rect2 floor = PlayableArea();
		var bugVariant = BugTypes.RandomVariant();
		bool prismatic = _forcePrismatic
			|| OS.GetEnvironment("LEAF_PRISMATIC") == "1"
			|| _rng.Randf() < PrismaticChance;
		_bug.Setup(bugVariant, RoundConfig.BugScale(_activeLevel),
			prismatic ? 0f : RoundConfig.Camouflage(_activeLevel), prismatic);
		_bug.Position = new Vector2(
			_rng.RandfRange(180f, floor.Size.X - 180f),
			_rng.RandfRange(320f, floor.Size.Y - 200f));
		_bug.Visible = true;

		SpawnGustCoins(floor);

		_stats.Start(_activeLevel);
		_rustleCountdown = _rng.RandfRange(RustleIntervalMin, RustleIntervalMax);

		// INSTANT_WIN=1 testing hook: win the moment the floor is dressed,
		// so the whole win flow (wind, warn sign, win card, next round)
		// can be replayed without sweeping — rapid cycles also walk the
		// level counter up to the storm rounds quickly.
		if (OS.GetEnvironment("INSTANT_WIN") == "1")
			WinLevel();
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
		int count = (int)(floor.Size.X * floor.Size.Y * RoundConfig.Coverage(level));
		ScatterDebris(floor, count, dropIn: true);
	}

	/// <summary>
	/// Dresses the home screen: a decorative litter scattered across the
	/// whole viewport (the dock is hidden in the menu), lifted straight
	/// into the end-of-round wind gyre so the menu card floats over a
	/// slowly spinning floor. The wind's ease-in ramp makes the gyre
	/// pick up from still, and riding pieces never fade.
	/// </summary>
	private void SpawnMenuDebris()
	{
		Rect2 area = new(Vector2.Zero, _viewSize);
		int count = (int)(area.Size.X * area.Size.Y * MenuDebrisCoverage);
		ScatterDebris(area, count, dropIn: false);
		StartEndRoundWind(MenuWindSpeedScale);
	}

	// Distinct textures with a cozy mix; leaves dominate, heavier stuff sparser.
	private static readonly (string Path, DebrisWeight Weight, int Freq)[] DebrisPalette =
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

	/// <summary>Builds one debris piece of a random palette kind at <paramref name="pos"/>.</summary>
	private Debris CreateDebris(Vector2 pos)
	{
		int total = 0;
		foreach (var entry in DebrisPalette)
			total += entry.Freq;

		int roll = _rng.RandiRange(1, total);
		(string path, DebrisWeight weight, _) = DebrisPalette[0];
		foreach (var entry in DebrisPalette)
		{
			roll -= entry.Freq;
			if (roll <= 0)
			{
				(path, weight, _) = entry;
				break;
			}
		}

		var debris = new Debris();
		debris.Setup(path, pos,
			_rng.RandfRange(0f, 360f),
			_rng.RandfRange(1.25f, 1.9f),
			weight,
			_rng);
		return debris;
	}

	private void ScatterDebris(Rect2 floor, int count, bool dropIn)
	{
		// Jittered-grid placement: one slot per cell guarantees the whole floor
		// is covered evenly (no bare patches, no visible bug), while the jitter
		// keeps it from looking like a lattice.
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

				Debris debris = CreateDebris(pos);
				// Round-start entrance: drop in with a tumble, staggered
				// along the top-left → bottom-right diagonal. The menu
				// skips this — its pieces spawn in place and the gyre
				// lifts them immediately.
				if (dropIn)
				{
					float diag = (pos.X / floor.Size.X + pos.Y / floor.Size.Y) * 0.5f;
					debris.SettleIn(_rng, diag * SettleSweepSeconds + _rng.RandfRange(0f, SettleJitterSeconds));
				}

				_debris.Add(debris);
				(placed < topCount ? _debrisTop : _debrisBottom).AddChild(debris);
				placed++;
			}
		}
	}

	private void SpawnGustCoins(Rect2 floor)
	{
		for (int i = 0; i < RoundConfig.GustCoinsForLevel(_activeLevel); i++)
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

	// -------------------------------------------------------- game flow ---

	private void WinLevel()
	{
		if (_state != GameState.Playing)
			return;

		_stats.Stop();
		SetState(GameState.Won);

		// A prismatic find erupts in a yellow-sun lens flare right at the
		// winning tap, before the bug's golden moment begins.
		_flareSeen |= _bug.IsPrismatic;
		if (_bug.IsPrismatic)
		{
			_sunFlare = new SunFlare { Position = _bug.Position };
			AddChild(_sunFlare);
		}

		// Copy uses pre-save history so "best" refers to earlier rounds.
		bool newBest = _save.LevelsCleared > 0 && _stats.Sweeps <= _save.BestSweeps();
		string comment = _stats.Comment(_save, _bug.Variant);
		string roundLine = $"{LevelStats.FormatTime(_stats.Elapsed)} · {_stats.Sweeps} sweeps";
		if (_stats.Gusts > 0)
			roundLine += $" · {_stats.Gusts} gust{(_stats.Gusts == 1 ? "" : "s")}";
		_save.RecordClear(_stats.Level, _stats.Sweeps, (int)_stats.Elapsed,
			_bug.Variant.Id, _stats.Gusts, _bug.IsPrismatic);

		// Lifetime cells for the card's stats row (post-save, so this find counts).
		BugVariant variant = _bug.Variant;
		_save.BugFindCounts.TryGetValue(variant.Id, out int finds);
		var stats = new[]
		{
			new Hud.WinStat(_save.BestSweeps().ToString(),
				newBest ? "New best round!" : "Best round", newBest),
			new Hud.WinStat($"×{finds}",
				finds == 1 ? $"First {variant.DisplayName}!" : variant.DisplayName,
				finds == 1),
			new Hud.WinStat(_save.LevelsCleared.ToString(), "Bugs found", false),
		};

		// The bug pops above the debris, grows, then flies to the screen
		// center; the win card seats it below the title when it arrives.
		_bug.Celebrate(_viewSize / 2f);
		PetalSparkle();
		// The round is over: whatever is still on the floor gets picked up
		// by a clockwise wind and keeps circling while the card is up, and
		// the storm eases off with the weather that made the round hard.
		StartEndRoundWind();
		_storm.FadeOut();
		// The round BEFORE a storm round: while the wind carries the
		// litter away, the electrical "Storm Round" sign crackles on.
		if (NextRoundIsStorm())
			_warn.ShowWarning();
		// A prismatic find rides its own banner out: the shiny "Prismatic"
		// sign appears after the round it crowned, mirroring how the storm
		// sign arrives before a storm round. If the storm sign shares this
		// end-round, the banner yields its perch and slots in below the
		// storm cloud instead of beside it.
		if (_bug.IsPrismatic)
			_prismaticSign.ShowSign(belowStormSign: _warn.Visible);
		// The win overlay waits for the bug's golden moment.
		_pendingWinComment = comment;
		_pendingWinRoundLine = roundLine;
		_pendingWinStats = stats;
	}

	private void OnBugCelebrationFinished()
	{
		if (_state == GameState.Won)
		{
			_hud.ShowWin(_pendingWinComment, _pendingWinRoundLine, _pendingWinStats,
				_bug, grandiose: _bug.IsPrismatic);
			// The flag doubles as the autoplay's grand-card assertion: it
			// only sticks when the card actually dressed for the find
			// (lighter panel + prismatic glow).
			if (_bug.IsPrismatic)
				_grandWinShown = _hud.WinGrandActive;
		}
	}

	/// <summary>
	/// Lifts every leftover piece into the end-of-round wind gyre: a slow
	/// clockwise swirl around the floor's center that keeps the litter
	/// gently airborne while the win card is up.
	/// </summary>
	private void StartEndRoundWind(float speedScale = 1f)
	{
		Vector2 center = WindCenter();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				d.StartEndRoundWind(center, _rng, speedScale);
	}

	/// <summary>
	/// True when the NEXT round will be a storm round and the end-of-round
	/// warning sign should crackle on. The LEAF_STORM test hook (and the
	/// autoplay's forced storm) keeps every round stormy, so its warnings
	/// are always truthful.
	/// </summary>
	private bool NextRoundIsStorm() => _forceStorm
		|| OS.GetEnvironment("LEAF_STORM") == "1"
		|| RoundConfig.IsStormLevel(_activeLevel + 1);

	/// <summary>
	/// The gyre's center: the middle of the playable floor during a round
	/// (the dock is never part of it); the whole viewport's middle on the
	/// menu, where the dock is hidden and the gyre is pure decoration.
	/// </summary>
	private Vector2 WindCenter() => _state == GameState.Menu
		? new Vector2(_viewSize.X / 2f, _viewSize.Y / 2f)
		: new Vector2(_viewSize.X / 2f, Mathf.Max(1f, _viewSize.Y - Hud.DockHeight) / 2f);

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
		{
			// Gust-cleared ground joins the storm pool like swept ground:
			// on storm rounds fresh debris falls back here too, so hoarding
			// gust coins can't keep a patch clean for long.
			RecordClearedSpot(alive[i]);
			alive[i].Fling(dir * _rng.RandfRange(1500f, 2200f), _rng);
		}

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
			_storm.FadeOut();
			_warn.HideWarning();
			_prismaticSign.HideSign();
			_menu.Refresh(_save);
			SpawnMenuDebris();
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
