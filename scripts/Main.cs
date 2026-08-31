using System;
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
	// Ground textures for the winter swap (loaded once in BuildTree).
	private Texture2D _summerGround = null!;
	private Texture2D _winterGround = null!;
	// The summer tornado prop: telegraphs, then crosses the floor while
	// ShuffleRound moves the litter (see Tornado).
	private Tornado _tornado = null!;
	private WaterStream _waterStream = null!;
	// The frozen-bug rescue (blizzard rounds): wraps the bug in ice.
	private IceBlock _ice = null!;
	// The churn prop currently on screen (tornado or water stream); set
	// by TriggerSeasonEvent, polled at touchdown.
	private FloorChurn _activeChurn = null!;

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

	// The seasonal vibe grade: full-floor color grade for the level's
	// season, sitting between the world and the storm veil (see
	// SeasonGrade).
	private SeasonGrade _seasonGrade = null!;

	// The season-intro banner: calm announcement card for season changes
	// and the year-loop bonus (see SeasonBanner).
	private SeasonBanner _seasonBanner = null!;

	// Season announcements fire once per session per season (and once per
	// loop restart). They must NOT reset on ClearLevel, or every round
	// would re-announce; a fresh app boot re-announces the current season
	// once, which is intended — and so does New Game, which resets them
	// (a second completed year must re-earn its bonus card).
	private RoundConfig.Season? _announcedSeason;
	private int _announcedLoop = -1;

	// Debris mix season: vibe only (see EffectiveFrequency). Defaults to
	// the spring mix for the menu's decorative litter; StartLevel sets it
	// to the round's resolved season.
	private RoundConfig.Season _debrisSeason = RoundConfig.Season.Spring;
	private GameState _state = GameState.Menu;

	// Winter storm rounds are blizzards: the storm overlay runs its snow
	// mode and every fresh storm drop is a snow pile. Resolved with the
	// storm round itself in StartLevel; cleared on the menu.
	private bool _blizzardRound;

	// The blizzard rescue key: one mallet per blizzard round, buried in
	// the litter like the coins. Null on every other round. Collecting
	// it parks a floating power-up at the top middle of the screen that
	// arms the three-tap ice crack (see Hammer).
	private Hammer? _hammer;
	private bool _hammerArmed;

	// Season-event pacing: the tornado/stream countdown (seeded when the
	// floor dresses), the pending touchdown (telegraph done → shuffle
	// starts) and the shuffle lock (touches stay locked while the floor
	// is mid-flight; the telegraph itself stays playable). The triggering
	// season rides along to touchdown — the autoplay hand-trigger can run
	// on any level, and the fraction (half vs everything) must follow the
	// event, not the level.
	private float _seasonEventCountdown;
	private bool _seasonEventTouchdownPending;
	private bool _shuffleInFlight;
	private RoundConfig.Season _activeEventSeason = RoundConfig.Season.Summer;

	// Bug/coin relocation glides in flight (see GlideNode).
	private readonly List<RelocationGlide> _glides = new();
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

	// Storm event timers (both 10–20s, both cosmetic): the spiral gust
	// shivers a small clockwise swirl through the litter, and the drift
	// sends a decorative raft of debris across the screen. The suppress
	// flag is the autoplay's determinism hook — the self-test triggers
	// all three cosmetic events by hand so its probes stay deterministic.
	private float _spiralCountdown;         // seconds to the next spiral gust
	private float _driftCountdown;          // seconds to the next drift raft
	private StormDrift? _stormDrift;        // the current drift raft, if any
	private bool _suppressStormEvents;      // autoplay: event timers off

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
	// how many patches the floor remembers. It must outsize the worst
	// burst of cleared ground inside one restore window: a spam of gusts
	// flings 25% of the floor per click, and the flood caps total live
	// debris at 3× the round's starting litter, so clearing the whole
	// flooded floor in one 4–6s window records ≈3× the round's starting
	// litter — ≈6.4k spots at the level-200 litter (each restore consumes
	// a spot, so live + pending stays bounded by start + flood pieces).
	// Anything smaller evicts the oldest pending spots first (the ones
	// due soonest), and their debris silently never returns; 16384 keeps
	// the eviction branch unreachable in live play with ~2.6× headroom
	// while still bounding the pool (~12B/spot ≈ 192KB steady at cap).
	private const float StormSpotDelayMin = 4f; // clean-patch lifetime (s, min)
	private const float StormSpotDelayMax = 6f; // clean-patch lifetime (s, max)
	private const int StormSpotsCap = 16384;    // remembered cleared spots (max)

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

	// Storm rustle rate: storm drafts comb the litter 3× as often as the
	// ambient 2–4s cadence — the storm floor never sits still.
	private const float StormRustleRateScale = 3f;

	// Storm spiral gust: an independent 10–20s timer tightens the wind into
	// a small cyclone — a clockwise wave of shivers that sweeps once around
	// an epicenter. The swirl never exceeds a fifth of the screen: the wave
	// radius is a tenth of the playable floor's smaller dimension.
	private const float SpiralIntervalMin = 10f;   // seconds between cyclones (min)
	private const float SpiralIntervalMax = 20f;   // seconds between cyclones (max)
	private const float SpiralRadiusFraction = 0.1f; // radius = this × min floor dimension
	private const float SpiralSweepRadPerSec = 7f; // wave's clockwise angular speed

	// Storm drift: a second, independent 10–20s timer sends a raft of
	// decorative debris spiraling across the screen (offscreen left →
	// offscreen right). Pure atmosphere — the raft never lands, so the
	// litter economy never notices it.
	private const float DriftIntervalMin = 10f;    // seconds between rafts (min)
	private const float DriftIntervalMax = 20f;    // seconds between rafts (max)
	private const int DriftPiecesMin = 6;          // pieces per raft (min)
	private const int DriftPiecesMax = 10;         // pieces per raft (max)

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

	/// <summary>
	/// Flushes C#-side wrappers before the engine's ObjectDB leak check:
	/// managed GodotObjects that dropped out of scope (transient Images,
	/// StyleBoxes) still pin native references until the GC finalizes
	/// them, which otherwise reads as leaked instances at exit.
	/// </summary>
	public override void _ExitTree()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	/// <summary>Headless self-test: plays a level end-to-end and verifies the save round-trip.</summary>
	private async void RunHeadlessAutoplay()
	{
		_save.Reset(); // deterministic: the test assumes a fresh save file
		_forcePrismatic = true; // this round must roll the rare prismatic bug
		_forceStorm = true; // this round must run the storm weather too
		// The cosmetic storm events (rustle pacing, spiral gusts, drift
		// rafts) run on 10–20s timers that would make the probes flaky —
		// the self-test triggers every one of them by hand instead.
		_suppressStormEvents = true;

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

		// Storm spiral gust: the swirl must shiver a clockwise wave through
		// the litter — at least one piece's sprite wobbles, no unswept node
		// transform moves, and everything (including the delayed tail of
		// the wave) settles back at rest. The drift raft below rides out
		// its crossing during the same wait.
		var spiralProbe = new List<(Debris Piece, Vector2 From)>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling && !d.IsRidingWind)
				spiralProbe.Add((d, d.Position));
		TriggerSpiralRustle();
		bool spiralShivered = false;
		bool spiralGrounded = true;
		foreach (var (piece, from) in spiralProbe)
			if (IsInstanceValid(piece) && !piece.Swept && piece.Position != from)
				spiralGrounded = false;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && d.IsRustling)
				spiralShivered = true;
		StormDrift drift = SpawnStormDrift();
		int driftPieces = drift.PieceCount;
		int liveBeforeDrift = LiveDebrisCount();
		Vector2 driftFrom = drift.ProbePosition;
		// Silence every storm rhythm for the raft's crossing so the
		// live-count delta isolates the raft: no spot drops, no flood
		// clusters — the raft alone must add nothing to the litter.
		bool stormWasRound = _isStormRound;
		_isStormRound = false;
		// Wave delay (≤ one revolution at the sweep rate) + shiver length.
		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		bool spiralSettled = true;
		foreach (var (piece, from) in spiralProbe)
		{
			if (!IsInstanceValid(piece) || piece.Swept)
				continue;
			if (piece.IsRustling || piece.Position != from)
				spiralSettled = false;
		}
		bool spiralOk = spiralShivered && spiralGrounded && spiralSettled;
		GD.Print($"AUTOPLAY spiral: shivered={spiralShivered} " +
				 $"grounded={spiralGrounded} settled={spiralSettled}");

		// Storm drift: by now the raft has visibly advanced (~1.9s of its
		// 2.4s crossing), the gameplay litter count is untouched, and the
		// raft frees itself once it has crossed offscreen.
		bool driftMoved = IsInstanceValid(drift)
			&& drift.ProbePosition.X > driftFrom.X + 60f;
		await ToSignal(GetTree().CreateTimer(3.0), SceneTreeTimer.SignalName.Timeout);
		bool driftFreed = !IsInstanceValid(drift);
		_isStormRound = stormWasRound;
		bool driftOk = driftPieces >= DriftPiecesMin && driftPieces <= DriftPiecesMax
			&& driftMoved && driftFreed
			&& LiveDebrisCount() == liveBeforeDrift;
		GD.Print($"AUTOPLAY drift: pieces={driftPieces} moved={driftMoved} " +
				 $"freed={driftFreed} liveDelta={LiveDebrisCount() - liveBeforeDrift} " +
				 $"ok={driftOk}");

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
				&& sweptAfter == sweptBefore + Mathf.Min(halo, _sweeper.MaxDebrisPerSweep)
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

		// Gust spam: rapid-fire gusts fling 25% of the floor per click, so
		// a handful of clicks records hundreds of pending storm spots in
		// well under a second. Every blown piece must leave exactly one
		// remembered spot — a pool too small evicts the oldest spots first
		// (the ones due soonest) and their debris never comes back. The
		// spam mirrors the playtest finding: 30 clicks milliseconds apart.
		// The storm probe below waits out the same window and proves every
		// one of those spots re-litters itself and leaves the pool.
		const int SpamClicks = 30;
		const int SpamPower = SaveData.StartingGustPower + SpamClicks + 1;
		_save.GustPower = SpamPower;
		_hud.ShowGustPower(_save.GustPower);
		int spotsBeforeSpam = _clearedSpots.Count;
		int spamBlown = 0;
		int spamLanded = 0; // clicks that actually blew something away
		for (int i = 0; i < SpamClicks; i++)
		{
			int liveBefore = LiveDebrisCount();
			OnWindPressed();
			int blown = liveBefore - LiveDebrisCount();
			spamBlown += blown;
			// Once the floor empties mid-spam, dry clicks spend and count
			// nothing — a gust is only spent when it blows something away.
			if (blown > 0)
				spamLanded++;
		}
		// Every flung piece must still be pending: nothing evicted, nothing
		// dropped early (the restore timers are 4–6s, none due inside the
		// same-frame spam).
		bool spamOk = spamLanded > 0
			&& _clearedSpots.Count == spotsBeforeSpam + spamBlown;
		GD.Print($"AUTOPLAY gust-spam: clicks={SpamClicks} landed={spamLanded} " +
				 $"blown={spamBlown} " +
				 $"spots={spotsBeforeSpam}->{_clearedSpots.Count} ok={spamOk}");

		// Storm rounds: the weather must be on, and the gusts above recorded
		// the spots they vacated through the real gust path — one pending
		// spot per blown piece, spam included. Every cleared patch must
		// re-litter itself when its own 4–6s timer comes due, and be
		// consumed from the pool. On the same cadence the flood dumps
		// cluster drops of brand-new debris onto random spots — those
		// pieces land off the recorded ground on purpose.
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
		// The celebration tint must be fully ramped in while the win card
		// is up: the find started the 0.45s ramp, and the next StartLevel
		// releases it — so this is the only window where it reads 1.
		double goldT = 0;
		while (CelebrationGoldMix < 0.99f && goldT < 2.0)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			goldT += GetProcessDeltaTime();
		}
		bool goldShown = CelebrationGoldMix >= 0.99f;
		GD.Print($"AUTOPLAY wind: pieces={windPieces} riding={windRiding} " +
				 $"checked={windChecked} moving={windMoving} clockwise={windClockwise} " +
				 $"warn={warnShown} warnOk={warnOk} prismSign={prismSignShown} " +
				 $"gold={goldShown}");

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
			&& spiralOk && driftOk
			&& coinSpawned && coinBanked && gustSpent && spamOk
			&& restartOk && windOk
			&& menuOk && stormOk && floodOk && capOk
			&& reloaded.CurrentLevel == playedLevel + 1
			&& reloaded.LevelsCleared == 1
			&& reloaded.TotalSweeps == 8
			&& reloaded.TotalGusts == 1 + spamLanded
			&& reloaded.GustPower == SpamPower - spamLanded
			&& reloaded.BugFindCounts.Count == 1
			&& reloaded.BugFindCounts.ContainsKey(_bug.Variant.Id)
			&& reloaded.History.Count == 1
			&& reloaded.History[0].Level == playedLevel
			&& reloaded.History[0].Gusts == 1 + spamLanded
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
		_prismaticSign.ShowSign(belowStormSign: _warn.Visible);
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

		// Celebration tint: replay the ramp/release cycle on this probe's
		// own clock (the real StartLevel release is timing-coupled to the
		// settle, so a direct read could straddle its 4s window). The
		// mid-fade check (strictly between 0 and 1) proves the release is
		// a tween rather than a snap, and the tail check proves it lands.
		bool goldOk = goldShown;
		float goldPeak, goldMid = 0f;
		CelebrateGold();
		{
			double goldProbeT = 0;
			while (CelebrationGoldMix < 0.99f && goldProbeT < 2.0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				goldProbeT += GetProcessDeltaTime();
			}
			goldPeak = CelebrationGoldMix;
			ReleaseGold();
			goldProbeT = 0; // anchor the mid/tail reads to the release itself
			while (goldProbeT < 1.5) // release t=1.5/4: must be mid-fade
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				goldProbeT += GetProcessDeltaTime();
			}
			goldMid = CelebrationGoldMix;
			while (goldProbeT < 5.0) // release is 4s + slack
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				goldProbeT += GetProcessDeltaTime();
			}
		}
		float goldEnd = CelebrationGoldMix;
		goldOk &= goldPeak >= 0.99f;
		goldOk &= goldMid > 0f && goldMid < 1f;
		goldOk &= goldEnd <= 0.01f;
		GD.Print($"AUTOPLAY prismatic-gold: peak={goldPeak:F2} mid={goldMid:F2} " +
				 $"end={goldEnd:F2} ok={goldOk}");
		ok &= goldOk;

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

		// Seasons: pure math plus the banner/tag surface. All deterministic
		// — no waits beyond one frame for the banner tween.
		(int Level, RoundConfig.Season Season)[] seasonCases =
		{
			(1, RoundConfig.Season.Spring), (99, RoundConfig.Season.Spring),
			(100, RoundConfig.Season.Summer), (199, RoundConfig.Season.Summer),
			(200, RoundConfig.Season.Fall), (300, RoundConfig.Season.Winter),
			(399, RoundConfig.Season.Winter), (400, RoundConfig.Season.Spring),
			(799, RoundConfig.Season.Winter), (800, RoundConfig.Season.Spring),
		};
		bool seasonMathOk = true;
		foreach (var (lvl, want) in seasonCases)
			seasonMathOk &= RoundConfig.SeasonForLevel(lvl) == want;
		seasonMathOk &= RoundConfig.LoopIndex(1) == 0
			&& RoundConfig.LoopIndex(399) == 0
			&& RoundConfig.LoopIndex(400) == 1
			&& RoundConfig.LoopIndex(799) == 1
			&& RoundConfig.LoopIndex(800) == 2;
		seasonMathOk &= RoundConfig.SweepPowerForLevel(1) == 12
			&& RoundConfig.SweepPowerForLevel(400) == 14
			&& RoundConfig.SweepPowerForLevel(800) == 16;
		seasonMathOk &= RoundConfig.GustCoinsForLevel(1) == 1
			&& RoundConfig.GustCoinsForLevel(10) == 3
			&& RoundConfig.GustCoinsForLevel(401) == 2
			&& RoundConfig.GustCoinsForLevel(410) == 4;
		_seasonBanner.ShowBanner("Test", "sub", Colors.White);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool bannerShown = _seasonBanner.Visible && _seasonBanner.FadeAlpha > 0f;
		_seasonBanner.HideBanner();
		// Grade uniforms follow the season: winter is the cold pale look
		// (desaturated, cool tint), spring the fresh clear one; the grade
		// shows both instantly via Current, then the look is restored.
		var springGrade = _seasonGrade.Current;
		_seasonGrade.ShowSeason(RoundConfig.Season.Winter);
		var winterGrade = _seasonGrade.Current;
		bool gradeOk = _seasonGrade.Visible
			&& winterGrade == SeasonGrade.Grades[3]
			&& winterGrade.Saturation < springGrade.Saturation
			&& winterGrade.Tint.B > springGrade.Tint.B;
		_seasonGrade.ShowSeason(RoundConfig.Season.Spring);
		// The autoplay's round starts are all low levels — every tag reads
		// "Level N · Spring".
		bool seasonOk = seasonMathOk && bannerShown && !_seasonBanner.Visible
			&& gradeOk && _hud.LevelText.Contains("Spring");
		GD.Print($"AUTOPLAY seasons: math={seasonMathOk} banner={bannerShown} " +
				 $"grade={gradeOk} tag={_hud.LevelText} ok={seasonOk}");
		ok &= seasonOk;

		// Summer tornado: hand-triggered (the season timers are suppressed
		// in autoplay) on the still-live storm round. The funnel must
		// telegraph untouched first, then half the at-rest litter, the bug
		// and every uncollected coin relocate to fresh spots — animated,
		// never teleported.
		var restBefore = new List<(Debris Piece, Vector2 Pos)>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling && !d.IsRidingWind)
				restBefore.Add((d, d.Position));
		Vector2 bugBefore = _bug.Position;
		var coinsBefore = new List<(GustCoin Coin, Vector2 Pos)>();
		foreach (var c in _coins)
			if (IsInstanceValid(c) && !c.Collected)
				coinsBefore.Add((c, c.Position));
		TriggerSeasonEvent(RoundConfig.Season.Summer);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool telegraphOk = _tornado.Active && _tornado.Telegraphing;
		double tornadoT = 0;
		while ((_tornado.Active || _shuffleInFlight) && tornadoT < 12)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			tornadoT += GetProcessDeltaTime();
		}
		int expectedMoves = (int)MathF.Round(restBefore.Count * TornadoShuffleFraction);
		int moved = 0;
		foreach (var (d, pos) in restBefore)
			if (d.Position.DistanceTo(pos) > 8f)
				moved++;
		int coinsMoved = 0;
		foreach (var (c, pos) in coinsBefore)
			if (c.Position.DistanceTo(pos) > 8f)
				coinsMoved++;
		bool bugMoved = _bug.Position.DistanceTo(bugBefore) > 8f;
		bool tornadoOk = telegraphOk && !_tornado.Active && !_shuffleInFlight
			&& bugMoved
			&& coinsMoved == coinsBefore.Count && coinsBefore.Count > 0
			// Storm cluster drops landing mid-telegraph join the mover
			// pool after restBefore was captured; their random picks
			// skew the count by up to one fresh batch (≤ StormClusterMax).
			&& Mathf.Abs(moved - expectedMoves) <= 2 + StormClusterMax;
		GD.Print($"AUTOPLAY tornado: telegraph={telegraphOk} atRest={restBefore.Count} " +
				 $"moved={moved}/{expectedMoves} bug={bugMoved} " +
				 $"coins={coinsMoved}/{coinsBefore.Count} ok={tornadoOk}");
		ok &= tornadoOk;

		// Fall streams: hand-triggered on the same live storm round. The
		// floor must shimmer untouched first, then every at-rest piece,
		// the bug and all coins wash to fresh spots along the stream.
		var restFall = new List<(Debris Piece, Vector2 Pos)>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling && !d.IsRidingWind)
				restFall.Add((d, d.Position));
		Vector2 bugFall = _bug.Position;
		var coinsFall = new List<(GustCoin Coin, Vector2 Pos)>();
		foreach (var c in _coins)
			if (IsInstanceValid(c) && !c.Collected)
				coinsFall.Add((c, c.Position));
		TriggerSeasonEvent(RoundConfig.Season.Fall);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool shimmerOk = _waterStream.Active && _waterStream.Telegraphing;
		double streamT = 0;
		while ((_waterStream.Active || _shuffleInFlight) && streamT < 14)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			streamT += GetProcessDeltaTime();
		}
		int movedFall = 0;
		foreach (var (d, pos) in restFall)
			if (d.Position.DistanceTo(pos) > 8f)
				movedFall++;
		int coinsMovedFall = 0;
		foreach (var (c, pos) in coinsFall)
			if (c.Position.DistanceTo(pos) > 8f)
				coinsMovedFall++;
		bool bugMovedFall = _bug.Position.DistanceTo(bugFall) > 8f;
		bool streamOk = shimmerOk && !_waterStream.Active && !_shuffleInFlight
			&& bugMovedFall
			&& coinsMovedFall == coinsFall.Count && coinsFall.Count > 0
			&& movedFall == restFall.Count && restFall.Count > 0;
		GD.Print($"AUTOPLAY streams: shimmer={shimmerOk} atRest={restFall.Count} " +
				 $"moved={movedFall}/{restFall.Count} bug={bugMovedFall} " +
				 $"coins={coinsMovedFall}/{coinsFall.Count} ok={streamOk}");
		ok &= streamOk;

		// Winter blizzard: level 310 is a true winter storm round, so the
		// weather must run its snow mode (flakes, no lightning, heavier
		// fog) and the flood dump must be white snow piles — the same
		// sweeping action, the same re-hiding, just cold. The per-spot
		// backfill is ordinary winter litter instead (the piles are the
		// flood's signature, not every replacement piece), proven here by
		// a hand backfill drop far from the bug. The piles floor forgives
		// the flood pieces the wider ice shelter (BlizzardRescueClearance,
		// ~12% of the floor) diverts away from the dig.
		StartLevel(310);
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(10.0), SceneTreeTimer.SignalName.Timeout);
		bool blizzardWeather = _storm.Active && _storm.IsSnow
			&& _storm.Intensity > 0f;
		int piles = 0;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept
				&& d.Texture.ResourcePath.Contains("snow_pile"))
				piles++;
		// (60, 60) sits outside the bug's spawn margins, so this spot can
		// never fall inside the rescue zone and get skipped.
		int before = _debris.Count;
		DropStormDebris(new Vector2(60f, 60f));
		bool backfillSpawned = _debris.Count == before + 1;
		bool backfillLitter = backfillSpawned
			&& !_debris[_debris.Count - 1].Texture.ResourcePath
				.Contains("snow_pile");
		bool blizzardOk = blizzardWeather && _blizzardRound
			&& piles >= WinterFloodMin * 3 / 4 && backfillLitter;
		GD.Print($"AUTOPLAY blizzard: weather={blizzardWeather} piles={piles} " +
				 $"min={WinterFloodMin} backfillLitter={backfillLitter} ok={blizzardOk}");
		ok &= blizzardOk;

		// Frozen-bug rescue: a fresh blizzard round wraps the bug in ice
		// AND buries one hammer in the litter. The lock is proven first:
		// without the hammer power-up an ice tap is refused (no crack —
		// just the locked pulse). Then the hammer is collected through
		// its real path (clockwise spiral up to the top-middle power-up
		// slot, where it floats) which arms the crack; three cracks
		// shatter the block and pick up the bug — the round is won. The
		// probe runs inside the flood's first window (~4s), before new
		// piles can land, and the flood skips the rescue zone anyway.
		StartLevel(310);
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool iceUp = _ice.Active && _ice.ContainsPoint(_bug.Position)
			&& BugIsCovered();
		bool hammerBuried = _hammer != null && IsInstanceValid(_hammer)
			&& !_hammer.Collected && !_hammer.Floating;
		bool tappedLocked = TryIceCrackTap();
		bool lockedWithoutHammer = !tappedLocked && _ice.Hits == 0
			&& _ice.Active;
		// Fling every piece whose alpha covers the ice — Burst's 130px
		// center radius can't reach big snow piles that still overlap the
		// chunk (players drag sweeps through them instead).
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && d.Covers(_ice.Position, IceBlock.BlockerRadius))
				d.Fling(Vector2.Right * 1800f, _rng);
		bool iceClear = !_hammerArmed && _ice.Active && !_ice.Shattered
			&& !DebrisOverlaps(_ice.Position, IceBlock.BlockerRadius);
		if (_hammer != null && IsInstanceValid(_hammer))
			CollectHammer(_hammer);
		// Bounded like the other waits: a broken collect→arm chain fails
		// the armed assertion below instead of hanging the run.
		for (int i = 0; i < 600 && !_hammerArmed; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool hammerArmed = _hammerArmed && _hammer != null
			&& IsInstanceValid(_hammer) && _hammer.Floating;
		// The crack's shockwave: the first armed crack flings the litter
		// off the dig (a radial dispersal at twice the burst radius) and
		// records NO backfill spots — its ground stays clean, so the
		// storm can't re-litter the rescue zone it just worked.
		int litterBefore = 0;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				litterBefore++;
		bool crack1 = TryIceCrackTap();
		int litterAfter = 0;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept)
				litterAfter++;
		int dispersed = litterBefore - litterAfter;
		bool shockwave = crack1 && dispersed > 0;
		bool noBackfillSpots = crack1 && _clearedSpots.Count == 0;
		bool stage1 = crack1 && _ice.Hits == 1 && !_ice.Shattered;
		bool crack2 = TryIceCrackTap();
		bool stage2 = crack2 && _ice.Hits == 2 && !_ice.Shattered;
		bool crack3 = TryIceCrackTap();
		bool bugPicked = crack3 && _ice.Shattered && !_ice.Active
			&& _state == GameState.Won;
		bool iceOk = iceUp && hammerBuried && lockedWithoutHammer && iceClear
			&& hammerArmed && shockwave && noBackfillSpots && stage1
			&& stage2 && bugPicked;
		GD.Print($"AUTOPLAY ice: wrapped={iceUp} buried={hammerBuried} " +
				 $"locked={lockedWithoutHammer} clear={iceClear} armed={hammerArmed} " +
				 $"flung={dispersed} noBackfill={noBackfillSpots} " +
				 $"stage1={stage1} stage2={stage2} win={bugPicked} ok={iceOk}");
		ok &= iceOk;

		// Year-loop bonus: level 401 is the first Spring of year 2 — the
		// sweep clears 14 pieces (12 + 2) and every round buries 2 gust
		// coins (1 + 1), and the banner shows the bonus card instead of a
		// season intro.
		StartLevel(401);
		while (_awaitingSettle)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool loopPower = _sweeper.MaxDebrisPerSweep
			== RoundConfig.SweepPowerForLevel(401)
			&& _sweeper.MaxDebrisPerSweep == 14;
		bool loopCoins = _coins.Count == RoundConfig.GustCoinsForLevel(401)
			&& _coins.Count == 2;
		bool loopBanner = _announcedLoop == RoundConfig.LoopIndex(401);
		bool loopOk = loopPower && loopCoins && loopBanner;
		GD.Print($"AUTOPLAY loop-bonus: power={_sweeper.MaxDebrisPerSweep} " +
				 $"coins={_coins.Count} banner={loopBanner} ok={loopOk}");
		ok &= loopOk;

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
		TickGlides((float)delta);
		UpdateShuffleLock();
		TickAmbientRustle(delta);
		if (_isStormRound && _state == GameState.Playing && !_awaitingSettle)
		{
			TickStormDrops(delta);
			TickClusterDrops(delta);
			TickSpiralRustle(delta);
			TickStormDrift(delta);
			TickSeasonEvent(delta);
			CheckSeasonEventTouchdown();
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
		if (_suppressStormEvents || _state != GameState.Playing || _awaitingSettle)
			return;
		_rustleCountdown -= (float)delta;
		if (_rustleCountdown > 0f)
			return;
		_rustleCountdown = NextRustleDelay();
		TriggerAmbientRustle();
	}

	/// <summary>
	/// Seconds to the next stray draft: storms comb the litter 3× as often
	/// as the ambient 2–4s cadence.
	/// </summary>
	private float NextRustleDelay() =>
		_rng.RandfRange(RustleIntervalMin, RustleIntervalMax)
		/ (_isStormRound ? StormRustleRateScale : 1f);

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
	/// Storm spiral gust: every 10–20s the storm tightens into a small
	/// cyclone. The at-rest litter within a capped circle (radius = 1/10 of
	/// the playable floor's smaller dimension, so the swirl never exceeds
	/// a fifth of the screen) shivers along the clockwise tangent, and each
	/// piece's shiver is delayed by its clockwise distance from 12 o'clock —
	/// a wave that visibly spins once through the patch. Purely cosmetic.
	/// </summary>
	private void TickSpiralRustle(double delta)
	{
		if (_suppressStormEvents)
			return;
		_spiralCountdown -= (float)delta;
		if (_spiralCountdown > 0f)
			return;
		_spiralCountdown = _rng.RandfRange(SpiralIntervalMin, SpiralIntervalMax);
		TriggerSpiralRustle();
	}

	/// <summary>
	/// One spiral gust: the epicenter is inset from the floor edges by the
	/// swirl radius so the whole spiral stays on-screen, and every close
	/// piece's shiver is delayed by its clockwise angle from the wave's
	/// start — the swirl reads as one rotating gust arm.
	/// </summary>
	private void TriggerSpiralRustle()
	{
		Rect2 floor = PlayableArea();
		float radius = Mathf.Min(floor.Size.X, floor.Size.Y) * SpiralRadiusFraction;
		Vector2 epicenter = new(
			_rng.RandfRange(radius, floor.Size.X - radius),
			_rng.RandfRange(radius, floor.Size.Y - radius));

		foreach (var d in _debris)
		{
			if (!IsInstanceValid(d) || d.Swept || d.IsSettling
				|| d.IsRidingWind || d.IsRustling)
				continue;
			Vector2 offset = d.Position - epicenter;
			float dist = offset.Length();
			if (dist > radius)
				continue;
			// Clockwise on screen (y down): the tangent for an increasing
			// angle is the angle plus a quarter turn.
			Vector2 tangent = Vector2.Right.Rotated(offset.Angle() + Mathf.Pi / 2f);
			// The wave waits on the piece's clockwise distance from the
			// 12-o'clock start (screen up = angle -π/2), so the shiver
			// travels around the swirl.
			float clockwise = Mathf.PosMod(offset.Angle() + Mathf.Pi / 2f, Mathf.Tau);
			float falloff = 1f - dist / radius * 0.6f;
			d.Rustle(
				tangent.Rotated(_rng.RandfRange(-0.35f, 0.35f)),
				falloff, _rng, clockwise / SpiralSweepRadPerSec);
		}
	}

	/// <summary>
	/// Storm drift rhythm: an independent 10–20s timer launches a raft of
	/// decorative debris that spirals across the screen, offscreen left to
	/// offscreen right. Purely cosmetic — the raft never lands, so the
	/// litter economy never notices it. Gated like the other storm events.
	/// </summary>
	private void TickStormDrift(double delta)
	{
		if (_suppressStormEvents)
			return;
		_driftCountdown -= (float)delta;
		if (_driftCountdown > 0f)
			return;
		_driftCountdown = _rng.RandfRange(DriftIntervalMin, DriftIntervalMax);
		SpawnStormDrift();
	}

	/// <summary>
	/// One drift raft: loose litter tumbles across the screen in spiral-y
	/// loops and exits offscreen. Nothing here touches the gameplay debris
	/// list — the raft cleans itself up when it has crossed.
	/// </summary>
	private StormDrift SpawnStormDrift()
	{
		var raft = new StormDrift(new Rect2(Vector2.Zero, _viewSize),
			AirborneTextures, _rng, _rng.RandiRange(DriftPiecesMin, DriftPiecesMax));
		// Explicit Z ladder: the raft rides just above the top debris layer
		// (bug 0 → debris 1/2 → drift 3) so it reads as airborne litter,
		// still under the storm veil on canvas layer 1.
		raft.ZIndex = 3;
		_stormDrift = raft;
		AddChild(raft);
		return raft;
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

	// Season event pacing. The tornado (summer) and the water streams
	// (fall) re-churn a settled storm round on their fixed cadence; the
	// autoplay suppresses the timers and hand-triggers the events for
	// deterministic probes.
	private const float TornadoShuffleFraction = 0.5f; // half the floor
	// Per-piece lift delay: 30ms when few pieces move, but the whole
	// stagger window is capped at ~0.9s so a dense floor still churns as
	// one wave inside the funnel's ~2s crossing.
	private const float ShuffleStaggerSeconds = 0.03f;
	private const float ShuffleStaggerWindow = 0.9f;
	// Smallest shuffle displacement that reads as a real move (and keeps
	// the autoplay's moved-everything assertion deterministic).
	private const float ShuffleMinMove = 32f;
	private const float GlideSeconds = 1.1f;           // bug/coin relocation glide
	private const float GlideArcHeight = 180f;         // tornado arc apex

	// Blizzard rescue zone: storm drops never land this close to the bug
	// while a blizzard round runs. The round-start litter still buries the
	// ice — the dig is the challenge — but the blizzard must not re-bury
	// the one spot the player is actively excavating, or the rescue turns
	// into an unwinnable race against the flood.
	// Widest litter bounding radius: a 100px texture at the max spawn
	// scale (1.9) — the upper bound of Debris.ExtentRadius.
	private const float MaxDebrisExtent = 95f;

	// The ice dig's weather shelter: no flood pile or backfill may land
	// close enough for its pixels to reach the ice's visible edge
	// (BlockerRadius). A piece's pixels can only reach that ring if its
	// CENTER sits within BlockerRadius + its bounding extent, so the
	// shelter is that reach plus a margin. Anything less and the flood
	// keeps re-burying the dig from just outside the ring — the rescue
	// becomes an unwinnable race no sweep can win.
	private const float BlizzardRescueClearance =
		IceBlock.BlockerRadius + MaxDebrisExtent + 20f;

	// Blizzard floods hit far harder than a summer/fall cluster: the
	// winter storm's exhale dumps a huge drift of snow at once, so the
	// floor visibly vanishes under white in a single beat (the same
	// 3× flood cap still tapers the round out).
	private const int WinterFloodMin = 20;
	private const int WinterFloodMax = 34;

	// The hammer crack's shockwave: each crack tap blasts the litter off
	// the rescue dig with a radial dispersal twice the double-tap burst's
	// radius — and that dispersal never joins the backfill pool, so the
	// storm can't reclaim the ground the hammer worked.
	private const float CrackDispersalRadius = Sweeper.BurstRadius * 2f;

	// The collected hammer's power-up slot: top middle of the screen,
	// below the level-label strip, clear of the dock.
	private const float HammerFloatHeight = 150f;

	/// <summary>
	/// Season event timer: on storm rounds of the event's season (summer →
	/// tornado, fall → streams), past the season's debut grace, the floor
	/// churns itself every fixed interval. The season debut level stays a
	/// celebration round (see RoundConfig.SeasonEventAllowed).
	/// </summary>
	private void TickSeasonEvent(double delta)
	{
		if (_suppressStormEvents || _shuffleInFlight)
			return;
		if (!RoundConfig.SeasonEventAllowed(_activeLevel))
			return;
		RoundConfig.Season season = RoundConfig.SeasonForLevel(_activeLevel);
		if (season is not (RoundConfig.Season.Summer or RoundConfig.Season.Fall))
			return;
		_seasonEventCountdown -= (float)delta;
		if (_seasonEventCountdown > 0f)
			return;
		_seasonEventCountdown = season == RoundConfig.Season.Summer
			? RoundConfig.TornadoInterval
			: RoundConfig.StreamInterval;
		TriggerSeasonEvent(season);
	}

	/// <summary>
	/// Starts the season's churn: the tornado telegraphs its crossing
	/// (summer) or the shimmer wash rolls in (fall), and the shuffle fires
	/// when the telegraph touches down.
	/// </summary>
	private void TriggerSeasonEvent(RoundConfig.Season season)
	{
		if (_tornado.Active || _waterStream.Active || _glides.Count > 0)
			return; // a churn is already in flight
		_activeEventSeason = season;
		Rect2 floor = PlayableArea();
		if (season == RoundConfig.Season.Summer)
		{
			bool leftToRight = _rng.Randf() < 0.5f;
			float y1 = _rng.RandfRange(floor.Size.Y * 0.35f, floor.Size.Y * 0.8f);
			float y2 = _rng.RandfRange(floor.Size.Y * 0.35f, floor.Size.Y * 0.8f);
			Vector2 from = new(leftToRight ? -60f : floor.Size.X + 60f, y1);
			Vector2 to = new(leftToRight ? floor.Size.X + 60f : -60f, y2);
			_tornado.Begin(from, to);
			_activeChurn = _tornado;
		}
		else
		{
			_waterStream.Begin(Vector2.Zero, floor.Size);
			_activeChurn = _waterStream;
		}
		_seasonEventTouchdownPending = true;
	}

	/// <summary>
	/// The shared churn core behind the tornado (half the floor) and the
	/// fall streams (everything): picks a fraction of the at-rest unswept
	/// debris plus the bug and every uncollected gust coin and sends them
	/// to fresh random floor spots — debris lifts and tumbles, the bug and
	/// coins glide. ZIndex never changes: bug and coins stay beneath the
	/// litter, and the covered rule re-evaluates per tap, so a shuffled
	/// bug may surface uncovered (fun, allowed).
	/// </summary>
	private void ShuffleRound(float fraction, bool slide, float direction = 0f)
	{
		var rest = new List<Debris>();
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept && !d.IsSettling && !d.IsRidingWind)
				rest.Add(d);
		// Fisher–Yates the rest list, then take the leading fraction as
		// the movers — a random, unbiased sample of the floor.
		for (int i = rest.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(rest[i], rest[j]) = (rest[j], rest[i]);
		}
		int movers = (int)MathF.Round(rest.Count * fraction);
		Rect2 floor = PlayableArea();
		float stagger = movers > 1
			? Mathf.Min(ShuffleStaggerSeconds, ShuffleStaggerWindow / (movers - 1))
			: 0f;
		float delay = 0f;
		Vector2 SpotFor()
		{
			Vector2 s = RandomFloorSpot(floor, 14f);
			if (slide)
			{
				// Bias the target downstream so the wash reads: pieces
				// slide along the stream, then spread onto fresh spots.
				float shift = _rng.RandfRange(0.15f, 0.5f) * floor.Size.X * direction;
				s.X = Mathf.Wrap(s.X + shift - 14f, 0f, floor.Size.X - 28f) + 14f;
			}
			return s;
		}
		for (int i = 0; i < movers; i++)
		{
			// A "relocated" piece that lands within a leaf's width of its
			// old spot reads as a glitch (and once broke the autoplay's
			// moved-everything assertion as a rare random near-hit) —
			// resample until the move is visibly real.
			Vector2 spot = SpotFor();
			for (int guard = 0;
				spot.DistanceTo(rest[i].Position) < ShuffleMinMove && guard < 8;
				guard++)
			{
				spot = SpotFor();
			}
			rest[i].RelocateTo(spot, _rng, delay, slide);
			delay += stagger;
		}
		// The bug and the uncollected coins ride the churn too, gliding to
		// their usual spawn-margin spots — never a teleport.
		if (_bug.Visible)
		{
			Vector2 bugSpot = RandomBugSpot(floor);
			for (int guard = 0;
				bugSpot.DistanceTo(_bug.Position) < ShuffleMinMove && guard < 8;
				guard++)
				bugSpot = RandomBugSpot(floor);
			GlideNode(_bug, bugSpot, slide);
		}
		foreach (var c in _coins)
		{
			if (!IsInstanceValid(c) || c.Collected)
				continue;
			// Same near-hit guard as the debris above: a glide that lands
			// within a leaf's width of the origin reads as a glitch (and
			// would flake the autoplay's moved-everything assertion).
			Vector2 coinSpot = RandomFloorSpot(floor, 150f);
			for (int guard = 0;
				coinSpot.DistanceTo(c.Position) < ShuffleMinMove && guard < 8;
				guard++)
				coinSpot = RandomFloorSpot(floor, 150f);
			GlideNode(c, coinSpot, slide);
		}
		_shuffleInFlight = true;
	}

	/// <summary>A random spot inside the floor with a uniform margin.</summary>
	private Vector2 RandomFloorSpot(Rect2 floor, float margin)
	{
		margin = Mathf.Min(margin, Mathf.Min(floor.Size.X, floor.Size.Y) / 2f);
		return new Vector2(
			_rng.RandfRange(margin, floor.Size.X - margin),
			_rng.RandfRange(margin, floor.Size.Y - margin));
	}

	/// <summary>The bug's spawn-margin spot (same bounds as round start).</summary>
	private Vector2 RandomBugSpot(Rect2 floor) => new(
		_rng.RandfRange(180f, floor.Size.X - 180f),
		_rng.RandfRange(320f, floor.Size.Y - 200f));

	/// <summary>
	/// One bug/coin relocation: an eased glide from its spot to the target
	/// — a raised arc for the tornado (up through the funnel), a flat
	/// slide for the fall streams. Updated by hand in _Process so the
	/// motion is exact and cancellable with no tween bookkeeping.
	/// </summary>
	private void GlideNode(Node2D node, Vector2 target, bool slide)
	{
		_glides.Add(new RelocationGlide
		{
			Node = node,
			From = node.Position,
			To = target,
			Age = 0f,
			Seconds = GlideSeconds * _rng.RandfRange(0.9f, 1.15f),
			Slide = slide,
		});
	}

	private void TickGlides(float delta)
	{
		for (int i = _glides.Count - 1; i >= 0; i--)
		{
			RelocationGlide g = _glides[i];
			if (!IsInstanceValid(g.Node))
			{
				_glides.RemoveAt(i);
				continue;
			}
			g.Age += delta;
			float t = Mathf.Clamp(g.Age / g.Seconds, 0f, 1f);
			float eased = Mathf.SmoothStep(0f, 1f, t);
			Vector2 pos = g.From.Lerp(g.To, eased);
			if (!g.Slide)
				pos += Vector2.Up * Mathf.Sin(Mathf.Pi * eased) * GlideArcHeight;
			g.Node.Position = pos;
			if (t >= 1f)
				_glides.RemoveAt(i);
		}
	}

	/// <summary>
	/// Touchdown: the telegraph finished, the churn begins. Runs on the
	/// storm gate so the shuffle only starts on a live, settled round.
	/// </summary>
	private void CheckSeasonEventTouchdown()
	{
		if (!_seasonEventTouchdownPending)
			return;
		if (_activeChurn == null || !_activeChurn.Active || _activeChurn.Telegraphing)
			return;
		_seasonEventTouchdownPending = false;
		// Summer's tornado takes half the floor with it; fall's streams
		// wash everything downstream.
		bool slide = _activeEventSeason == RoundConfig.Season.Fall;
		ShuffleRound(
			slide ? 1f : TornadoShuffleFraction,
			slide,
			direction: slide ? _waterStream.Direction : 0f);
	}

	/// <summary>
	/// The shuffle lock: touches stay locked from the churn's first lift
	/// until every relocated piece has landed and every glide finished.
	/// </summary>
	private void UpdateShuffleLock()
	{
		if (!_shuffleInFlight)
			return;
		if (_glides.Count > 0)
			return;
		foreach (var d in _debris)
			if (IsInstanceValid(d) && d.IsSettling)
				return;
		_shuffleInFlight = false;
	}

	/// <summary>Kills any churn in flight (win / menu / restart).</summary>
	private void CancelShuffle()
	{
		_glides.Clear();
		_shuffleInFlight = false;
		_seasonEventTouchdownPending = false;
		_tornado.EndShow();
		_waterStream.EndShow();
	}

	private class RelocationGlide
	{
		public Node2D Node = null!;
		public Vector2 From;
		public Vector2 To;
		public float Age;
		public float Seconds;
		public bool Slide;
	}

	/// <summary>
	/// One storm drop: fresh debris tumbles back down onto a remembered
	/// cleared patch. The spot is consumed — once debris sits there again
	/// it is unswept ground, and it can only rejoin the pool by being
	/// swept once more. The backfill is the round's ordinary litter even
	/// on blizzard rounds — the solid-white piles are the flood's
	/// signature, not every replacement piece — and it never lands in the
	/// ice rescue zone.
	/// </summary>
	private void DropStormDebris(Vector2 spot)
	{
		Rect2 floor = PlayableArea();

		// A viewport resize mid-round can leave a remembered spot outside
		// the new playable rect; clamp it back onto the floor.
		Vector2 pos = new(
			Mathf.Clamp(spot.X, 14f, floor.Size.X - 14f),
			Mathf.Clamp(spot.Y, 14f, floor.Size.Y - 14f));

		// The blizzard never piles onto the rescue: a remembered cleared
		// patch that falls inside the ice zone stays clean (the spot is
		// consumed either way — the wind can't reach it there).
		if (_blizzardRound
			&& pos.DistanceTo(_bug.Position) <= BlizzardRescueClearance)
			return;

		SpawnStormDebris(pos, snowPile: false);
	}

	/// <summary>
	/// The shared storm spawn path: one fresh unswept piece tumbles down
	/// onto <paramref name="pos"/> (SettleIn) and lands wherever it may —
	/// fresh debris follows the normal overlap rules, so it can re-cover
	/// the bug and the gust coins. Blizzard rounds split the look: the
	/// flood dump (<paramref name="snowPile"/>) drops its signature white
	/// snow piles, while the per-spot backfill restores ordinary winter
	/// litter. Sweeping either is the same action and both re-hide the
	/// bug and coins exactly like any other piece.
	/// </summary>
	private void SpawnStormDebris(Vector2 pos, bool snowPile)
	{
		Debris debris = snowPile && _blizzardRound
			? NewSnowPile(pos)
			: CreateDebris(pos);
		debris.SettleIn(_rng, _rng.RandfRange(0f, 0.3f));
		_debris.Add(debris);
		(_rng.Randf() < 0.35f ? _debrisTop : _debrisBottom).AddChild(debris);
	}

	/// <summary>A blizzard flood piece: a fresh white snow pile.</summary>
	private Debris NewSnowPile(Vector2 pos)
	{
		var debris = new Debris();
		debris.Setup(SnowPilePath, pos,
			_rng.RandfRange(0f, 360f),
			_rng.RandfRange(1.25f, 1.9f),
			DebrisWeight.Medium,
			_rng);
		return debris;
	}

	/// <summary>
	/// Storm flood rhythm: every 4–6s a gust dumps a whole cluster (6–12
	/// pieces) of brand-new litter onto random floor spots — debris that
	/// was never swept, so the storm escalates instead of just undoing
	/// progress (on blizzard rounds the cluster is the white snow-pile
	/// dump, kept clear of the ice rescue zone). Once the live debris
	/// count reaches the cap (3× the round's starting litter) the flood
	/// abates for the round; the per-spot restoration above keeps going
	/// regardless. Gated to live, settled rounds like the spot drops.
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
		// Blizzard rounds drop their own, far heavier burst size.
		int clusterMin = _blizzardRound ? WinterFloodMin : StormClusterMin;
		int clusterMax = _blizzardRound ? WinterFloodMax : StormClusterMax;
		DropStormCluster(Mathf.Min(
			_rng.RandiRange(clusterMin, clusterMax), room));
	}

	/// <summary>
	/// One cluster drop: <paramref name="count"/> fresh pieces tumble onto
	/// random floor spots. Blizzard rounds drop snow piles — the flood is
	/// the blizzard's signature event — re-rolled out of the ice rescue
	/// zone.
	/// </summary>
	private void DropStormCluster(int count)
	{
		Rect2 floor = PlayableArea();
		for (int i = 0; i < count; i++)
		{
			Vector2 pos = new(
				_rng.RandfRange(14f, floor.Size.X - 14f),
				_rng.RandfRange(14f, floor.Size.Y - 14f));
			// The flood skips the ice rescue zone: a pile landing on the
			// block the player is excavating reads as the game fighting
			// the rescue, not as weather.
			for (int guard = 0;
				_blizzardRound
				&& pos.DistanceTo(_bug.Position) <= BlizzardRescueClearance
				&& guard < 8;
				guard++)
			{
				pos = new(
					_rng.RandfRange(14f, floor.Size.X - 14f),
					_rng.RandfRange(14f, floor.Size.Y - 14f));
			}
			if (_blizzardRound
				&& pos.DistanceTo(_bug.Position) <= BlizzardRescueClearance)
				continue;
			SpawnStormDebris(pos, snowPile: true);
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
		// Touches stay locked while the round-start settle is in flight,
		// and while a season churn (tornado / streams) has the floor
		// mid-flight.
		if (_state != GameState.Playing || _awaitingSettle || _shuffleInFlight)
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
				// The hammer power-up: hidden below the debris on blizzard
				// rounds like the coins. An uncovered hammer is collected
				// instead of starting a sweep; a covered one falls through
				// to sweeping.
				Hammer hammer = SelectableHammerAt(world);
				if (hammer != null)
				{
					_tapArmed = false; // selection taps never chain into a burst
					CollectHammer(hammer);
					return;
				}
				// The frozen-bug rescue: with the hammer power-up armed, a
				// tap on cleared ice cracks the block (three taps shatter
				// it and pick up the bug). Debris still over the ice falls
				// through to sweeping — but the tap pulses the offending
				// pieces first; without the hammer the chunk itself pulses
				// a red "locked" cue, so a refusal always has a reason.
				if (_ice.Active && _ice.ContainsPoint(world) && TryIceCrackTap())
					return;
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
		// GetImage allocates a fresh native Image per call; dispose it when
		// the probe is done so it can't read as a leak at engine exit.
		using Image img = d.Texture.GetImage();
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
	/// True while the bug can't be selected: unswept debris overlaps its
	/// visible body (OcclusionRadius — much tighter than the forgiving
	/// TapRadius), or a blizzard's ice block still wraps it — the ice is
	/// itself cover until it shatters.
	/// </summary>
	private bool BugIsCovered() =>
		_ice.Active || DebrisOverlaps(_bug.Position, _bug.OcclusionRadius);

	/// <summary>
	/// Pulses every piece still covering the ice: the blocked rescue tap's
	/// answer to "why didn't that crack?" — the offenders light up warm.
	/// </summary>
	private void FlashIceBlockers()
	{
		foreach (var d in _debris)
			if (IsInstanceValid(d) && !d.Swept
				&& d.Covers(_ice.Position, IceBlock.BlockerRadius))
				d.FlashBlocker();
	}

	/// <summary>
	/// One tap on the ice chunk: with the hammer power-up armed and the
	/// debris cleared it cracks the block — the third tap shatters it and
	/// picks up the bug, winning the round ("crack it and pick up the
	/// bug", one less hunt after the rescue). Every crack tap also fires
	/// the hammer's shockwave: a radial dispersal of the litter around
	/// the dig, twice the double-tap burst's radius, whose cleared ground
	/// never backfills. Without the hammer the tap is refused: the chunk
	/// flares a red locked cue, and debris still over it pulses amber.
	/// Returns true only when the tap cracked.
	/// </summary>
	private bool TryIceCrackTap()
	{
		bool clear = !DebrisOverlaps(_ice.Position, IceBlock.BlockerRadius);
		if (_hammerArmed && clear)
		{
			_tapArmed = false; // crack taps never chain into a burst
			_ice.Crack();
			// The shockwave: like a double-tap burst on the ice, but
			// twice the radius and its ground stays clean — recordSpots
			// is false, so no backfill spot is recorded for it.
			_sweeper.Burst(_ice.Position, CrackDispersalRadius, recordSpots: false);
			if (_ice.Shattered)
				WinLevel();
			return true;
		}
		if (!_hammerArmed)
			_ice.PulseLocked();
		if (!clear)
			FlashIceBlockers();
		return false;
	}

	/// <summary>The buried hammer under a tap, or null (see SelectableCoinAt).</summary>
	private Hammer SelectableHammerAt(Vector2 world)
	{
		if (_hammer == null || !IsInstanceValid(_hammer) || _hammer.Collected)
			return null!;
		// A covered hammer can't be collected — the tap starts sweeping
		// there instead. Coverage is judged against the hammer's visible
		// face (OcclusionRadius), not its tap area.
		if (!_hammer.ContainsPoint(world)
			|| DebrisOverlaps(_hammer.Position, _hammer.OcclusionRadius))
			return null!;
		return _hammer;
	}

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
	/// A collected hammer flies its clockwise spiral up to the top-middle
	/// power-up slot and floats there for the rest of the round; the
	/// ice-crack gate arms the moment it parks.
	/// </summary>
	private void CollectHammer(Hammer hammer)
	{
		Vector2 screenPos = GetCanvasTransform() * hammer.Position;
		hammer.FloatStarted += OnHammerFloatStarted;
		hammer.Reparent(_hud);
		hammer.Position = screenPos;
		hammer.Collect(new Vector2(_viewSize.X / 2f, HammerFloatHeight));
	}

	private void OnHammerFloatStarted()
	{
		_hammerArmed = true;
	}

	/// <summary>
	/// The blizzard rescue key: one mallet per winter storm round, buried
	/// in the litter like the coins. Without it the ice can't be cracked
	/// — the rescue is a two-step dig: find the tool, then free the bug.
	/// </summary>
	private void SpawnHammer(Rect2 floor)
	{
		var hammer = new Hammer { Name = "Hammer" };
		hammer.Setup(_rng.RandfRange(78f, 90f), _rng);
		// CoinSpot keeps the mallet spread out from the bug and coins.
		hammer.Position = CoinSpot(floor);
		AddChild(hammer);
		_hammer = hammer;
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
		_summerGround = GD.Load<Texture2D>(GroundPath);
		_winterGround = GD.Load<Texture2D>(GroundWinterPath);
		var groundSprite = new Sprite2D { Texture = _summerGround };
		groundSprite.Name = "Sprite";
		_ground.AddChild(groundSprite);
		AddChild(_ground);

		// The summer tornado: a world-space prop above the litter (ZIndex
		// 4), shown only while a tornado crosses.
		_tornado = new Tornado { Name = "Tornado" };
		AddChild(_tornado);
		_waterStream = new WaterStream { Name = "WaterStream" };
		AddChild(_waterStream);
		_ice = new IceBlock { Name = "IceBlock" };
		AddChild(_ice);

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
		// world 0 → season grade 1 → storm 2 → menu 3 → hud 4 → warn 5 →
		// prismatic 6 → season banner 7 → bug book 90. The seasonal grade
		// sits directly above the floor; the storm veil and rain ride
		// above it, and every UI above that.
		_seasonGrade = new SeasonGrade { Name = "SeasonGrade" };
		AddChild(_seasonGrade);

		_storm = new StormOverlay { Name = "Storm" };
		AddChild(_storm);

		// The "Storm Round" warning sign lives above the HUD (layer 5) so
		// its sparks never dim under the storm veil nor sit under UI.
		_warn = new StormWarn { Name = "StormWarn" };
		AddChild(_warn);

		// The "Prismatic" banner rides out a prismatic find the same way,
		// one rung above the storm sign so both can show at once (a
		// prismatic round immediately before a storm round seats the
		// banner just below the storm cloud — see PrismaticSign).
		_prismaticSign = new PrismaticSign { Name = "PrismaticSign" };
		AddChild(_prismaticSign);

		// One shared celebration material for the whole litter (pieces pick
		// it up at spawn — see Debris.CelebrationMaterial).
		_celebrationMat = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://assets/shaders/prismatic_celebration.gdshader"),
		};

		// The season banner rides one rung above the prismatic sign: the
		// season note opens a round, the find banners close one — they
		// never share the screen in practice, and if they ever do the
		// season note yields the top.
		_seasonBanner = new SeasonBanner { Name = "SeasonBanner" };
		AddChild(_seasonBanner);

		_hud = new Hud { Name = "Hud" };
		_hud.Layer = 4;
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
		_menu.Layer = 3;
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
		// A floating hammer parks at a screen-relative slot, so it rides
		// the new size; one mid-flight jumps straight to the new slot
		// (its tween path was computed against the old screen); a buried
		// one scales with the floor like the bug.
		if (_hammer != null && IsInstanceValid(_hammer))
		{
			if (_hammer.Floating)
				_hammer.SnapFloat(new Vector2(_viewSize.X / 2f, HammerFloatHeight));
			else if (_hammer.Collected)
				_hammer.SkipToFloat(new Vector2(_viewSize.X / 2f, HammerFloatHeight));
			else
				_hammer.Position *= floorRatio;
		}
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
		if (_hammer != null && IsInstanceValid(_hammer))
			_hammer.QueueFree();
		_hammer = null;
		_hammerArmed = false;
		_bug.Visible = false;
		_clearedSpots.Clear();
		CancelStormDrift();
	}

	/// <summary>
	/// Takes the current drift raft (if any) off the screen at once — a
	/// win or a fresh round ends the weather's chaos mid-crossing.
	/// </summary>
	private void CancelStormDrift()
	{
		if (_stormDrift != null && IsInstanceValid(_stormDrift))
			_stormDrift.QueueFree();
		_stormDrift = null;
	}

	private void StartLevel(int level)
	{
		ClearLevel();
		CancelShuffle();
		FitGround();
		// A celebrating litter sheds its gold/white mix over the opening —
		// the storm label's dissolve pacing.
		ReleaseGold();

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
		// Year-loop bonus: the sweep clears more debris from level 400 on.
		_sweeper.SetSweepPower(RoundConfig.SweepPowerForLevel(level));
		_isStormRound = _forceStorm
			|| OS.GetEnvironment("LEAF_STORM") == "1"
			|| RoundConfig.IsStormLevel(level);
		// Winter storm rounds are blizzards: the overlay runs its snow
		// mode and every storm drop is a snow pile. Mechanics key off the
		// true level season (LEAF_SEASON restyles, never re-mechanics).
		_blizzardRound = _isStormRound
			&& RoundConfig.SeasonForLevel(level) == RoundConfig.Season.Winter;
		if (_isStormRound)
			_storm.FadeIn(snow: _blizzardRound);
		else
			_storm.FadeOut();
		SpawnDebris(level, floor);
		_roundStartDebris = _debris.Count;
		_clusterCountdown = _rng.RandfRange(StormSpotDelayMin, StormSpotDelayMax);
		_spiralCountdown = _rng.RandfRange(SpiralIntervalMin, SpiralIntervalMax);
		_driftCountdown = _rng.RandfRange(DriftIntervalMin, DriftIntervalMax);
		_floodDone = false;
		_awaitingSettle = true;

		RoundConfig.Season season = ResolveSeason(level);
		_hud.ShowLevel(level, season);
		AnnounceSeason(level, season);
		// Seasonal vibe: the grade and the litter mix follow the display
		// season, and winter swaps the ground for its snow-covered twin —
		// a tint can't fake snow.
		_seasonGrade.ShowSeason(season);
		_debrisSeason = season;
		_ground.GetChild<Sprite2D>(0).Texture = season == RoundConfig.Season.Winter
			? _winterGround : _summerGround;
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

	// Shared celebration shader for the whole litter: gold_mix is tweened
	// on this one material, so every circling piece flips to gold/white
	// together and releases back together. Null-checked at use sites —
	// only pieces spawned while it exists carry it.
	private ShaderMaterial? _celebrationMat;
	private Tween? _celebrationTween;

	// Autoplay probe: the celebration tint's current strength.
	public float CelebrationGoldMix =>
		_celebrationMat?.GetShaderParameter("gold_mix").As<float>() ?? 0f;

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
		// Blizzard rescue: winter storm rounds freeze the bug in ice;
		// every other round frees it.
		if (_blizzardRound)
			_ice.Place(_bug.Position);
		else
			_ice.Reset();

		SpawnGustCoins(floor);
		// The blizzard rescue key: one mallet buried with the rest.
		if (_blizzardRound)
			SpawnHammer(floor);

		_stats.Start(_activeLevel);
		_rustleCountdown = NextRustleDelay();
		_seasonEventCountdown = RoundConfig.TornadoInterval; // summer cadence; fall reads StreamInterval when its turn comes

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

	/// <summary>
	/// Airborne litter palette for the storm drift: light and medium pieces
	/// only — sticks and rocks flying through the air reads wrong.
	/// Declared after <see cref="DebrisPalette"/> so the static initializer
	/// sees it.
	/// </summary>
	private static readonly string[] AirborneTextures = BuildAirborneTextures();

	private static string[] BuildAirborneTextures()
	{
		var paths = new List<string>();
		foreach (var entry in DebrisPalette)
			if (entry.Weight != DebrisWeight.Heavy)
				paths.Add(entry.Path);
		return paths.ToArray();
	}

	// Winter ground: snow-covered forest floor swapped in for winter
	// levels — a tint can't fake snow (generated by tools/gen_art.mjs).
	private const string GroundPath = "res://assets/textures/ground.svg";
	private const string GroundWinterPath = "res://assets/textures/ground_winter.svg";
	// Blizzard storm drops: fresh snow piles the round's winds pile on.
	private const string SnowPilePath = "res://assets/textures/snow_pile.svg";

	/// <summary>Builds one debris piece of a random palette kind at <paramref name="pos"/>.</summary>
	private Debris CreateDebris(Vector2 pos)
	{
		int total = 0;
		foreach (var entry in DebrisPalette)
			total += EffectiveFrequency(entry);

		int roll = _rng.RandiRange(1, total);
		(string path, DebrisWeight weight, _) = DebrisPalette[0];
		foreach (var entry in DebrisPalette)
		{
			roll -= EffectiveFrequency(entry);
			if (roll <= 0)
			{
				(path, weight, _) = entry;
				break;
			}
		}

		var debris = new Debris { CelebrationMaterial = _celebrationMat };
		debris.Setup(path, pos,
			_rng.RandfRange(0f, 360f),
			_rng.RandfRange(1.25f, 1.9f),
			weight,
			_rng);
		return debris;
	}

	// The litter matches the trees: each season re-weights the shared
	// palette — summer leans green, fall goes red/gold, winter mixes snow
	// flecks in and thins the leaves (vibe only; weights never change what
	// a piece weighs or how it sweeps). Spring keeps the base mix.
	private int EffectiveFrequency((string Path, DebrisWeight Weight, int Freq) entry)
	{
		float f = entry.Freq;
		return (int)MathF.Round(_debrisSeason switch
		{
			RoundConfig.Season.Summer => entry.Path.Contains("leaf_green") ? f * 2.4f
				: entry.Path.Contains("moss") ? f * 1.5f : f,
			RoundConfig.Season.Fall => entry.Path.Contains("leaf_red")
				|| entry.Path.Contains("leaf_yellow") ? f * 2f
				: entry.Path.Contains("petal") ? f * 0.5f : f,
			RoundConfig.Season.Winter => entry.Path.Contains("petal_white") ? f * 2.5f
				: entry.Path.Contains("leaf") ? f * 0.7f : f,
			_ => f,
		});
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

		// A prismatic find erupts in a yellow-sun lens flare parented to the
		// winning bug itself — its transform carries the sun from the tap,
		// behind the bug, all the way to its seat in the win card.
		_flareSeen |= _bug.IsPrismatic;
		if (_bug.IsPrismatic)
		{
			_sunFlare = new SunFlare();
			_bug.AddChild(_sunFlare);
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
		CancelStormDrift();
		CancelShuffle();
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
		{
			_prismaticSign.ShowSign(belowStormSign: _warn.Visible);
			// The litter circling in the win wind turns gold and white for
			// the celebration, releasing as the next round opens.
			CelebrateGold();
		}
		// The win overlay waits for the bug's golden moment.
		_pendingWinComment = comment;
		_pendingWinRoundLine = roundLine;
		_pendingWinStats = stats;
	}

	// The celebration tint rides the same pacing as the prismatic banner:
	// a quick ramp in with the find (0.45s, the banner's FadeSeconds), then
	// a slow 4s release over the next round's opening — the storm label's
	// fade pacing — so the litter returns to its own colors as play begins.
	private const float CelebrationRampSeconds = 0.45f;
	private const float CelebrationReleaseSeconds = 4f;

	/// <summary>Flips the circling litter to its gold/white celebration mix.</summary>
	private void CelebrateGold()
	{
		if (_celebrationMat == null)
			return;
		_celebrationTween?.Kill();
		_celebrationTween = CreateTween();
		// Tween a method, not the material property: headless Godot's dummy
		// renderer never compiles the shader, so "shader_parameter/gold_mix"
		// isn't exposed as a property there and TweenProperty silently fails.
		_celebrationTween.TweenMethod(
			Callable.From<float>(v => _celebrationMat.SetShaderParameter("gold_mix", v)),
			CelebrationGoldMix, 1f, CelebrationRampSeconds);
	}

	/// <summary>
	/// Releases the celebration tint over the next round's opening, the
	/// same pacing the storm label dissolves with.
	/// </summary>
	private void ReleaseGold()
	{
		if (_celebrationMat == null || CelebrationGoldMix <= 0f)
			return;
		_celebrationTween?.Kill();
		_celebrationTween = CreateTween();
		_celebrationTween.TweenMethod(
			Callable.From<float>(v => _celebrationMat.SetShaderParameter("gold_mix", v)),
			CelebrationGoldMix, 0f, CelebrationReleaseSeconds);
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
		StartLevel(SaveStartLevel());
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
			_seasonGrade.HideGrade();
			_warn.HideWarning();
			_prismaticSign.HideSign();
			_seasonBanner.HideBanner();
			CancelShuffle();
			// Full vibe reset: a blizzard round quit mid-dig must not
			// strand its ice chunk, snow ground or winter litter mix on
			// the menu.
			_ice.Reset();
			_ground.GetChild<Sprite2D>(0).Texture = _summerGround;
			_debrisSeason = RoundConfig.Season.Spring;
			_blizzardRound = false;
			_menu.Refresh(_save);
			SpawnMenuDebris();
		}
	}

	/// <summary>
	/// The season shown for this level's HUD tag and intro banner.
	/// LEAF_SEASON=spring|summer|fall|winter forces the display/vibe season
	/// for testing — level-derived mechanics always key off the true level
	/// season (INITIAL_LEVEL=&lt;n&gt; forces the whole level instead).
	/// </summary>
	private RoundConfig.Season ResolveSeason(int level)
	{
		switch (OS.GetEnvironment("LEAF_SEASON").ToLowerInvariant())
		{
			case "spring": return RoundConfig.Season.Spring;
			case "summer": return RoundConfig.Season.Summer;
			case "fall": return RoundConfig.Season.Fall;
			case "winter": return RoundConfig.Season.Winter;
			default: return RoundConfig.SeasonForLevel(level);
		}
	}

	/// <summary>
	/// Season announcements: the year-loop bonus card when a new year
	/// begins, otherwise the season's intro note the first time that season
	/// shows in the session. A loop restart shows the bonus card INSTEAD of
	/// the plain season intro — the restart must read as a reward.
	/// </summary>
	private void AnnounceSeason(int level, RoundConfig.Season season)
	{
		int loop = RoundConfig.LoopIndex(level);
		if (loop > 0 && loop > _announcedLoop)
		{
			_announcedLoop = loop;
			_announcedSeason = season;
			_seasonBanner.ShowLoopBonus(loop);
		}
		else if (_announcedSeason != season)
		{
			_announcedSeason = season;
			_seasonBanner.ShowSeason(season);
		}
	}

	/// <summary>
	/// INITIAL_LEVEL=&lt;n&gt; testing hook: save-driven round starts (Play,
	/// Next, Restart) begin at level &lt;n&gt; instead of the save's current
	/// level, so manual playtests can jump straight to a difficulty tier
	/// (storm rounds from 10, the camouflage ramp from 60). While the var is
	/// set the run stays pinned to &lt;n&gt; — clearing the level still bumps
	/// the save, same caveat as INITIAL_GUSTS.
	/// </summary>
	private int SaveStartLevel()
	{
		if (int.TryParse(OS.GetEnvironment("INITIAL_LEVEL"), out int forcedLevel)
			&& forcedLevel > 0)
			return forcedLevel;
		return _save.CurrentLevel;
	}

	private void OnPlayPressed() => StartLevel(SaveStartLevel());

	private void OnNewGamePressed()
	{
		_save.Reset();
		// A fresh run re-earns its announcements: without this reset the
		// year-loop bonus card could never fire a second time in one
		// session (loop > _announcedLoop would stay false).
		_announcedSeason = null;
		_announcedLoop = -1;
		StartLevel(1);
	}

	private void OnNextPressed() => StartLevel(SaveStartLevel());

	private void OnMenuPressed()
	{
		_hud.HideWin();
		// The menu's decorative litter always shows its true colors.
		_celebrationTween?.Kill();
		if (_celebrationMat != null)
			_celebrationMat.SetShaderParameter("gold_mix", 0f);
		SetState(GameState.Menu);
	}
}
