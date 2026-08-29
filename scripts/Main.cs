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
    private string _pendingWinStats = "";

    private SaveData _save = null!;
    private LevelStats _stats = new();
    private Sweeper _sweeper = null!;
    private List<Debris> _debris = new();
    private List<GustCoin> _coins = new();
    private GameState _state = GameState.Menu;
    private Vector2 _viewSize;

    /// <summary>Gold gust coins hidden below the debris each round.</summary>
    private const int GustCoinsPerLevel = 3;

    public override void _Ready()
    {
        _rng.Randomize();

        BuildTree();
        _viewSize = GetViewportRect().Size;
        FitGround();
        GetViewport().SizeChanged += OnViewportResized;

        _save = SaveData.Load();
        _sweeper = new Sweeper(() => _debris, _rng, OnSwipeCompleted);

        _menu.Refresh(_save);
        SetState(GameState.Menu);

        if (OS.GetEnvironment("LEAF_AUTOPLAY") == "1")
            RunHeadlessAutoplay();
    }

    /// <summary>Headless self-test: plays a level end-to-end and verifies the save round-trip.</summary>
    private void RunHeadlessAutoplay()
    {
        _save.Reset(); // deterministic: the test assumes a fresh save file
        StartLevel(3);

        // Covered-bug rule: debris parked on the bug blocks selection, and
        // sweeping every overlapping piece away makes it selectable again.
        var blocker = _debris.Find(d => IsInstanceValid(d) && !d.Swept);
        bool blocked = false;
        bool uncovered = false;
        if (blocker != null)
        {
            blocker.Position = _bug.Position;
            blocked = BugIsCovered();
            foreach (var d in _debris)
                if (IsInstanceValid(d) && !d.Swept
                    && d.Position.DistanceTo(_bug.Position) <= _bug.TapRadius + d.CoverRadius)
                    d.Fling(Vector2.Right * 2000f, _rng);
            uncovered = !BugIsCovered();
        }
        GD.Print($"AUTOPLAY uncover: blocked={blocked} cleared={uncovered}");

        // Gust coins: three hide below the debris each round; collecting one
        // banks +1 power, spending a gust takes −1.
        var coin = _coins.Find(c => IsInstanceValid(c) && !c.Collected);
        bool coinSpawned = _coins.Count == GustCoinsPerLevel;
        bool coinBanked = false;
        if (coin != null)
        {
            CollectCoin(coin);
            coinBanked = _save.GustPower == SaveData.StartingGustPower + 1;
        }
        GD.Print($"AUTOPLAY coins: spawned={_coins.Count} banked={coinBanked} " +
                 $"power={_save.GustPower}");

        for (int i = 0; i < 7; i++)
        {
            _stats.Tick(1.0);
            _stats.CountSwipe();
        }
        OnWindPressed(); // spends one gust power and counts the use
        bool gustSpent = _save.GustPower == SaveData.StartingGustPower;
        WinLevel();

        var reloaded = SaveData.Load();
        bool ok = blocked && uncovered && coinSpawned && coinBanked && gustSpent
            && reloaded.CurrentLevel == 4
            && reloaded.LevelsCleared == 1
            && reloaded.TotalSwipes == 7
            && reloaded.TotalGusts == 1
            && reloaded.GustPower == SaveData.StartingGustPower
            && reloaded.BugFindCounts.Count == 1
            && reloaded.History.Count == 1
            && reloaded.History[0].Level == 3
            && reloaded.History[0].Gusts == 1;

        GD.Print($"AUTOPLAY save: level={_save.CurrentLevel} cleared={_save.LevelsCleared} " +
                 $"swipes={_save.TotalSwipes} gusts={_save.TotalGusts} " +
                 $"bugs={_save.BugFindCounts.Count} hist={_save.History.Count}");
        GD.Print($"AUTOPLAY reload: level={reloaded.CurrentLevel} cleared={reloaded.LevelsCleared} " +
                 $"swipes={reloaded.TotalSwipes} gusts={reloaded.TotalGusts} ok={ok}");
        GetTree().Quit(ok ? 0 : 1);
    }

    public override void _Process(double delta) => _stats.Tick(delta);

    // ------------------------------------------------------------- input --

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_state != GameState.Playing)
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
                    CollectCoin(coin);
                    return;
                }
                // The bug hides below the debris: while any unswept piece
                // overlaps its tap area a tap just starts sweeping there.
                if (_bug.ContainsPoint(world) && !BugIsCovered())
                {
                    WinLevel();
                    return;
                }
                _sweeper.Begin(world, Time.GetTicksUsec() * 1000UL);
                break;
            }
            case InputEventScreenTouch { Pressed: false } t:
                if (t.Canceled)
                    _sweeper.Cancel();
                else
                    _sweeper.End();
                break;
            case InputEventScreenDrag drag:
                _sweeper.Drag(ToWorld(drag.Position), Time.GetTicksUsec() * 1000UL);
                break;
        }
    }

    private Vector2 ToWorld(Vector2 screenPos) =>
        GetCanvasTransform().AffineInverse() * screenPos;

    private void OnSwipeCompleted()
    {
        if (_state != GameState.Playing)
            return;
        _stats.CountSwipe();
        _hud.ShowSwipes(_stats.Swipes);
    }

    /// <summary>
    /// True while any unswept debris overlaps a circular tap area — shared by
    /// the bug and gust coins, which both hide below the debris.
    /// </summary>
    private bool DebrisOverlaps(Vector2 pos, float radius)
    {
        foreach (var d in _debris)
            if (IsInstanceValid(d) && !d.Swept
                && d.Position.DistanceTo(pos) <= radius + d.CoverRadius)
                return true;
        return false;
    }

    /// <summary>
    /// True while any unswept debris still overlaps the bug's tap area —
    /// the bug can only be selected (and the round won) once it's uncovered.
    /// </summary>
    private bool BugIsCovered() => DebrisOverlaps(_bug.Position, _bug.TapRadius);

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
            // tap starts sweeping there instead.
            if (DebrisOverlaps(c.Position, c.TapRadius))
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
    /// An uncovered gust coin was tapped: bank the power right away (and
    /// persist it), then let the coin fly its golden spiral into the dock's
    /// gust button.
    /// </summary>
    private void CollectCoin(GustCoin coin)
    {
        _save.GustPower++;
        _save.Save();
        _hud.ShowGustPower(_save.GustPower);
        coin.Collect(ToWorld(_hud.WindButtonCenter));
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
        foreach (var d in _debris)
            if (IsInstanceValid(d) && !d.Swept)
                d.Position *= floorRatio;
        // Coins still hiding in the debris stretch along; collected ones are
        // mid-flight toward the dock and are left alone.
        foreach (var c in _coins)
            if (IsInstanceValid(c) && !c.Collected)
                c.Position *= floorRatio;
    }

    private void ClearLevel()
    {
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
        // take it back into the world before setting it up again.
        if (_bug.GetParent() != this)
            _bug.Reparent(this);

        // The floor is everything above the HUD dock — the dock is never
        // covered by debris or the bug.
        Rect2 floor = PlayableArea();

        var bugType = BugTypes.Random();
        float bugScale = RoundConfig.BugScale(level);
        _bug.Setup(bugType, bugScale, RoundConfig.Camouflage(level));
        _bug.Position = new Vector2(
            _rng.RandfRange(180f, floor.Size.X - 180f),
            _rng.RandfRange(320f, floor.Size.Y - 200f));
        _bug.Visible = true;

        SpawnDebris(level, floor);
        SpawnGustCoins(floor);

        _stats.Start(level);
        _hud.ShowLevel(level);
        _hud.ShowSwipes(0);
        _hud.ShowGustPower(_save.GustPower);
        _hud.HideWin();
        SetState(GameState.Playing);
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
            // The coin frees itself once its spiral flight finishes.
            coin.CollectionFlightFinished += coin.QueueFree;
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

        // Comment uses pre-save history so "best" refers to earlier rounds.
        string comment = _stats.Comment(_save, _bug.Type);
        string statsLine = $"{LevelStats.FormatTime(_stats.Elapsed)} · {_stats.Swipes} swipes";
        if (_stats.Gusts > 0)
            statsLine += $" · {_stats.Gusts} gust{(_stats.Gusts == 1 ? "" : "s")}";
        _save.RecordClear(_stats.Level, _stats.Swipes, (int)_stats.Elapsed, _bug.Type.Id, _stats.Gusts);

        // The bug pops above the debris, grows, then flies to the screen
        // center; the win card seats it below the title when it arrives.
        _bug.Celebrate(_viewSize / 2f);
        PetalSparkle();
        // The win overlay waits for the bug's golden moment.
        _pendingWinComment = comment;
        _pendingWinStats = statsLine;
    }

    private void OnBugCelebrationFinished()
    {
        if (_state == GameState.Won)
            _hud.ShowWin(_pendingWinComment, _pendingWinStats, _bug);
    }

    /// <summary>Blows away 10% of the remaining debris with a gusty fling.</summary>
    private void OnWindPressed()
    {
        if (_state != GameState.Playing)
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

        // Shuffle a copy so the ~10% sample is scattered across the floor,
        // not clustered in one grid region.
        for (int i = alive.Count - 1; i > 0; i--)
        {
            int j = _rng.RandiRange(0, i);
            (alive[i], alive[j]) = (alive[j], alive[i]);
        }

        int count = Mathf.Max(1, alive.Count / 10);
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
