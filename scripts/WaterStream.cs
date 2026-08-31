using Godot;

namespace LeafSweeper;

/// <summary>
/// The fall water streams: a full-floor sheet of flowing streaks
/// (water.gdshader) that shimmers ~1.5s as its telegraph — the floor
/// glistens, nothing moves — then washes across while the shuffle slides
/// everything downstream, and fades. The motion lives in the shader
/// streaks plus the debris slide; the sheet itself just covers the floor.
/// Purely visual, like the tornado; ZIndex rides above the debris.
/// </summary>
public partial class WaterStream : FloorChurn
{
    // Telegraph: the floor shimmers with slow pulses this long before the
    // wash starts — a memory game never cheats.
    public const float TelegraphSeconds = 1.5f;

    // Wash pace: the streams race across the floor for ~2s, matching the
    // tornado's crossing read.
    public const float WashSeconds = 2f;

    // Feel tunables: telegraph shimmer sits low and pulses gently; the
    // wash brightens well above it; the fade dissolves the puddle away.
    private const float ShimmerBase = 0.14f;
    private const float ShimmerAmp = 0.08f;
    private const float ShimmerRate = 9f;  // shimmer pulse frequency
    private const float WashIntensity = 0.42f;
    private const float FadeSeconds = 0.4f;

    private readonly ColorRect _sheet = new();
    private readonly ShaderMaterial _mat = new()
    {
        Shader = GD.Load<Shader>("res://assets/shaders/water.gdshader"),
    };

    private float _age;

    public WaterStream()
    {
        ZIndex = 4;
        Visible = false;
        _sheet.Material = _mat;
        _sheet.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_sheet);
    }

    /// <summary>+1 when the wash flows right, -1 when it flows left.</summary>
    public float Direction { get; private set; } = 1f;

    /// <summary>True while the sheet is on screen (telegraph or wash).</summary>
    public override bool Active { get; protected set; }

    /// <summary>True during the shimmer telegraph, before the wash starts.</summary>
    public override bool Telegraphing { get; protected set; }

    // Begins the show: `from` is the floor's top-left, `to` the floor's
    // size — the sheet covers the whole playable floor; the crossing
    // motion lives in the shader streaks and the debris slide.
    public override void Begin(Vector2 from, Vector2 to)
    {
        // Churn visuals use Godot's global RNG on purpose: direction
        // carries no gameplay state, and the seeded generator stays
        // reserved for reproducible round layouts.
        Direction = GD.Randf() < 0.5f ? -1f : 1f;
        Position = from;
        _sheet.Size = to;
        _age = 0f;
        Active = true;
        Telegraphing = true;
        Visible = true;
        _mat.SetShaderParameter("intensity", 0f);
        _mat.SetShaderParameter("direction", Direction);
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

        if (_age < TelegraphSeconds)
        {
            // Shimmer: gentle brightness pulses across the floor — the
            // wash is coming, nothing moves yet.
            _mat.SetShaderParameter("intensity",
                ShimmerBase + ShimmerAmp * Mathf.Sin(_age * ShimmerRate));
            return;
        }

        float wash = (_age - TelegraphSeconds) / WashSeconds;
        if (wash < 1f)
        {
            Telegraphing = false;
            _mat.SetShaderParameter("intensity", WashIntensity);
            return;
        }

        float fade = (_age - TelegraphSeconds - WashSeconds) / FadeSeconds;
        if (fade < 1f)
        {
            _mat.SetShaderParameter("intensity", WashIntensity * (1f - fade));
            return;
        }
        EndShow();
    }
}