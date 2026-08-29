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
    private GameState _state = GameState.Menu;
    private Vector2 _viewSize;

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
        for (int i = 0; i < 7; i++)
        {
            _stats.Tick(1.0);
            _stats.CountSwipe();
        }
        WinLevel();

        var reloaded = SaveData.Load();
        bool ok = reloaded.CurrentLevel == 4
            && reloaded.LevelsCleared == 1
            && reloaded.TotalSwipes == 7
            && reloaded.BugFindCounts.Count == 1
            && reloaded.History.Count == 1
            && reloaded.History[0].Level == 3;

        GD.Print($"AUTOPLAY save: level={_save.CurrentLevel} cleared={_save.LevelsCleared} " +
                 $"swipes={_save.TotalSwipes} bugs={_save.BugFindCounts.Count} hist={_save.History.Count}");
        GD.Print($"AUTOPLAY reload: level={reloaded.CurrentLevel} cleared={reloaded.LevelsCleared} " +
                 $"swipes={reloaded.TotalSwipes} ok={ok}");
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
                if (_bug.ContainsPoint(world))
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
        AddChild(_debrisBottom);

        _debrisTop = new Node2D { Name = "DebrisTop" };
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

        // Stretch the live round's layout from the old rect onto the new
        // one so the floor never shows bare background mid-level.
        if (oldSize.X <= 0f || oldSize.Y <= 0f)
            return;
        Vector2 ratio = new(newSize.X / oldSize.X, newSize.Y / oldSize.Y);
        // While the bug is seated on the win card it lives in the HUD and the
        // containers position it — only stretch it while it's in the world.
        bool bugInWorld = IsInstanceValid(_bug) && _bug.Visible && _bug.GetParent() == this;
        if (bugInWorld)
            _bug.Position *= ratio;
        foreach (var d in _debris)
            if (IsInstanceValid(d) && !d.Swept)
                d.Position *= ratio;
    }

    private void ClearLevel()
    {
        foreach (var d in _debris)
            if (IsInstanceValid(d))
                d.QueueFree();
        _debris.Clear();
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

        Rect2 view = GetViewportRect();

        var bugType = BugTypes.Random();
        float bugScale = RoundConfig.BugScale(level);
        _bug.Setup(bugType, bugScale, RoundConfig.Camouflage(level));
        _bug.Position = new Vector2(
            _rng.RandfRange(180f, view.Size.X - 180f),
            _rng.RandfRange(320f, view.Size.Y - 200f));
        _bug.Visible = true;

        SpawnDebris(level, view);

        _stats.Start(level);
        _hud.ShowLevel(level);
        _hud.ShowSwipes(0);
        _hud.HideWin();
        _hud.SetInLevelVisible(true);
        SetState(GameState.Playing);
    }

    private void SpawnDebris(int level, Rect2 view)
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
        int count = (int)(view.Size.X * view.Size.Y * RoundConfig.Coverage(level));
        float cell = Mathf.Sqrt(view.Size.X * view.Size.Y / Mathf.Max(count, 1));
        int topCount = count * 35 / 100; // 35% drawn above the rest for depth

        int placed = 0;
        for (float y = cell * 0.5f; y < view.Size.Y && placed < count; y += cell)
        {
            for (float x = cell * 0.5f; x < view.Size.X && placed < count; x += cell)
            {
                Vector2 pos = new(
                    Mathf.Clamp(x + _rng.RandfRange(-0.45f, 0.45f) * cell, 14f, view.Size.X - 14f),
                    Mathf.Clamp(y + _rng.RandfRange(-0.45f, 0.45f) * cell, 14f, view.Size.Y - 14f));

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
        _save.RecordClear(_stats.Level, _stats.Swipes, (int)_stats.Elapsed, _bug.Type.Id);

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
            var sparkle = new Sprite2D { Texture = petalTex, Position = _bug.Position };
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
        _hud.SetInLevelVisible(state != GameState.Menu);
        _hud.SetControlsVisible(state == GameState.Playing);
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
