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

    private SaveData _save = null!;
    private LevelStats _stats = new();
    private Sweeper _sweeper = null!;
    private List<Debris> _debris = new();
    private GameState _state = GameState.Menu;

    public override void _Ready()
    {
        _rng.Randomize();

        BuildTree();

        _save = SaveData.Load();
        _sweeper = new Sweeper(() => _debris, _rng, OnSwipeCompleted);

        _menu.Refresh(_save);
        SetState(GameState.Menu);
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

        _debrisBottom = new Node2D { Name = "DebrisBottom" };
        AddChild(_debrisBottom);

        _bug = new Bug { Name = "Bug" };
        AddChild(_bug);

        _debrisTop = new Node2D { Name = "DebrisTop" };
        AddChild(_debrisTop);

        _hud = new Hud { Name = "Hud" };
        _hud.NextPressed += OnNextPressed;
        _hud.MenuPressed += OnMenuPressed;
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

        Rect2 view = GetViewportRect();

        var bugType = BugTypes.Random();
        float bugScale = RoundConfig.BugScale(level);
        _bug.Setup(bugType, bugScale, RoundConfig.Camouflage(level));
        _bug.Position = new Vector2(
            _rng.RandfRange(180f, view.Size.X - 180f),
            _rng.RandfRange(320f, view.Size.Y - 200f));
        _bug.Visible = true;

        SpawnDebris(level, view, bugType.TapRadius * bugScale);

        _stats.Start(level);
        _hud.ShowLevel(level);
        _hud.ShowSwipes(0);
        _hud.HideWin();
        _hud.SetInLevelVisible(true);
        SetState(GameState.Playing);
    }

    private void SpawnDebris(int level, Rect2 view, float bugRadius)
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

        int count = RoundConfig.DebrisCount(level);
        int topCount = count * 30 / 100; // 30% drawn above the bug
        var placed = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = FindSpot(view, placed, bugRadius, i >= topCount);
            placed.Add(pos);

            int roll = _rng.RandiRange(1, total);
            (string path, DebrisWeight weight, _) = Pick(palette, roll);

            var debris = new Debris();
            debris.Setup(
                path,
                pos,
                _rng.RandfRange(0f, 360f),
                _rng.RandfRange(0.9f, 1.5f),
                weight,
                _rng);

            _debris.Add(debris);
            (i < topCount ? _debrisTop : _debrisBottom).AddChild(debris);
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

    /// <summary>
    /// Finds a spawn spot: inside the play area, not clumping with existing
    /// debris, and (for debris drawn above the bug) never covering the bug's
    /// core so it always peeks out.
    /// </summary>
    private Vector2 FindSpot(Rect2 view, List<Vector2> placed,
        float bugRadius, bool avoidBugCore)
    {
        Vector2 margin = new(70f, 150f);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2 p = new(
                _rng.RandfRange(margin.X, view.Size.X - margin.X),
                _rng.RandfRange(margin.Y, view.Size.Y - margin.Y));

            if (avoidBugCore && p.DistanceTo(_bug.Position) < bugRadius * 0.65f)
                continue;

            bool tooClose = false;
            foreach (Vector2 other in placed)
            {
                if (p.DistanceTo(other) < 26f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
                return p;
        }
        return new Vector2(
            _rng.RandfRange(margin.X, view.Size.X - margin.X),
            _rng.RandfRange(margin.Y, view.Size.Y - margin.Y));
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

        _bug.Celebrate();
        PetalSparkle();
        _hud.ShowWin(comment, statsLine);
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
