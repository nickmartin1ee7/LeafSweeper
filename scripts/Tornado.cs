using Godot;

namespace LeafSweeper;

/// <summary>
/// The summer tornado: a swirling funnel prop that telegraphs its arrival
/// (~1.5s spin-up in place, touching nothing — a memory game never
/// cheats), then crosses the playable floor (~2s) while the shuffle it
/// announced lifts the litter, the bug and the coins into fresh spots.
/// Purely visual: the relocation itself lives in Main.ShuffleRound; the
/// funnel sells the cause. The node's origin is the funnel's ground tip
/// (where the dust ring spins), and ZIndex rides above the debris so it
/// draws over everything it disturbs.
/// </summary>
public partial class Tornado : FloorChurn
{
    // Telegraph: the funnel spins up semi-transparent for this long before
    // the shuffle starts, so players can watch it form and brace.
    public const float TelegraphSeconds = 1.5f;

    // Crossing pace: the funnel clears the floor in ~2s — a brisk walk,
    // matching the storm drift's read.
    public const float TravelSeconds = 2f;

    // Feel tunables: the funnel churns via tilt + sway (rotating the cone
    // sprite itself would tip it over), the dust skirt spins freely, and
    // the whole prop fades quickly at each end of the crossing.
    private const float FunnelSpinRate = 9f;   // rad/s churn of the tilt wobble
    private const float FunnelTiltDeg = 8f;    // max tilt off vertical
    private const float FunnelSwayFrac = 0.04f;// sideways wander, fraction of travel
    private const float DustSpinRate = 5f;     // rad/s skirt rotation
    private const float FadeSeconds = 0.4f;    // dissolve at the far end
    private const float FunnelScale = 1.25f;   // prop size on screen
    private const float MaxAlpha = 0.9f;

    // Sprite offsets: the funnel SVG is 120×320 with its ground tip ~20px
    // above the bottom edge, so shift it up to hang the tip on the origin;
    // the dust ring is centered by nature.
    private const float FunnelTipInset = 20f;

    private readonly Sprite2D _funnel = new()
    {
        Texture = GD.Load<Texture2D>("res://assets/textures/tornado_funnel.svg"),
        Offset = new Vector2(0f, -(160f - FunnelTipInset)),
    };
    private readonly Sprite2D _dust = new()
    {
        Texture = GD.Load<Texture2D>("res://assets/textures/dust_ring.svg"),
    };

    private Vector2 _from;
    private Vector2 _to;
    private float _age;

    public Tornado()
    {
        ZIndex = 4;
        Visible = false;
        _funnel.Scale = Vector2.One * FunnelScale;
        _dust.Scale = Vector2.One * FunnelScale;
        AddChild(_dust);
        AddChild(_funnel);
    }

    /// <summary>True while the funnel is on screen (telegraph or crossing).</summary>
    public override bool Active { get; protected set; }

    /// <summary>True during the spin-up telegraph, before the shuffle starts.</summary>
    public override bool Telegraphing { get; protected set; }

    /// <summary>
    /// Starts the show: telegraph in place at <paramref name="from"/>,
    /// then cross to <paramref name="to"/>.
    /// </summary>
    public override void Begin(Vector2 from, Vector2 to)
    {
        _from = from;
        _to = to;
        _age = 0f;
        Active = true;
        Telegraphing = true;
        Visible = true;
        Position = from;
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>Ends the show at once (win / menu / restart).</summary>
    public override void EndShow()
    {
        Active = false;
        Telegraphing = false;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Active)
            return;
        _age += (float)delta;
        float dt = (float)delta;

        if (_age < TelegraphSeconds)
        {
            // Telegraph: funnel scales up and solidifies in place while the
            // tilt wobble and dust skirt spin up. The litter is untouched.
            float t = _age / TelegraphSeconds;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Position = _from;
            Modulate = new Color(1f, 1f, 1f, MaxAlpha * eased);
            float churn = FunnelTiltDeg * eased;
            _funnel.RotationDegrees = Mathf.Sin(_age * FunnelSpinRate) * churn;
            _funnel.Scale = Vector2.One * FunnelScale * (0.35f + 0.65f * eased);
            _dust.Rotation += DustSpinRate * dt * eased;
            return;
        }

        float p = (_age - TelegraphSeconds) / TravelSeconds;
        if (p < 1f)
        {
            Telegraphing = false;
            // Crossing: a smooth walk across the floor with a sideways
            // wander (zero at both ends) that reads as the funnel weaving
            // through its own wind.
            float eased = Mathf.SmoothStep(0f, 1f, p);
            Vector2 dir = (_to - _from).Normalized();
            Vector2 side = new(-dir.Y, dir.X);
            float wander = Mathf.Sin(p * Mathf.Pi) * _from.DistanceTo(_to)
                * FunnelSwayFrac;
            Position = _from.Lerp(_to, eased) + side * wander;
            Modulate = Colors.White;
            _funnel.RotationDegrees = Mathf.Sin(_age * FunnelSpinRate) * FunnelTiltDeg;
            _dust.Rotation += DustSpinRate * dt;
            return;
        }

        float fade = (_age - TelegraphSeconds - TravelSeconds) / FadeSeconds;
        if (fade < 1f)
        {
            Modulate = new Color(1f, 1f, 1f, MaxAlpha * (1f - fade));
            _dust.Rotation += DustSpinRate * dt;
            return;
        }
        EndShow();
    }
}
