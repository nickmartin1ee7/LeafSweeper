using Godot;

namespace LeafSweeper;

/// <summary>
/// Full-screen storm atmosphere for storm levels: the world darkens under a
/// heavy veil, dense wind-swept rain falls, cloud shadows and mist drift,
/// and lightning flashes (see assets/shaders/storm.gdshader). The overlay
/// lives on its own canvas layer above the world but below the HUD, is
/// touch-transparent, and animates itself off TIME — the game only fades
/// the shader's `intensity` uniform in and out.
/// </summary>
public partial class StormOverlay : CanvasLayer
{
    private const string ShaderPath = "res://assets/shaders/storm.gdshader";

    // A slow fade so the weather arrives like weather, not a light switch.
    private const float FadeSeconds = 1.4f;

    private ColorRect _rect = null!;
    private ShaderMaterial _material = null!;
    private Tween _fade = null!;

    /// <summary>True while the storm should be on (fade in either direction).</summary>
    public bool Active { get; private set; }

    /// <summary>Current shader intensity — 0 hidden, 1 full storm.</summary>
    public float Intensity { get; private set; }

    public StormOverlay()
    {
        // Explicit canvas ladder (declared in Main.BuildTree): world 0 →
        // storm 1 → menu 2 → hud 3 → book 90. Godot draws same-layer
        // CanvasLayers in non-deterministic order, so the storm owns
        // its own index.
        Layer = 1;
        Visible = false;
    }

    public override void _Ready()
    {
        _rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        _material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };
        _material.SetShaderParameter("intensity", 0f);
        _rect.Material = _material;
        AddChild(_rect);
        // Anchors AND offsets: plain SetAnchorsPreset leaves the offsets
        // preserving the rect's old zero size — the full-screen rect stays
        // 0×0 and renders nothing (same trap the dock once hit).
        _rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Fades the storm in and leaves it running.</summary>
    public void FadeIn()
    {
        Active = true;
        Visible = true;
        FadeTo(1f);
    }

    /// <summary>Fades the storm out and hides the layer once it's gone.</summary>
    public void FadeOut()
    {
        Active = false;
        FadeTo(0f);
    }

    private void FadeTo(float target)
    {
        // A first FadeOut can arrive before any tween exists (the menu
        // engages the weather off state during _Ready).
        if (_fade != null && _fade.IsValid())
            _fade.Kill();
        _fade = CreateTween();
        _fade.TweenMethod(Callable.From<float>(v =>
        {
            Intensity = v;
            _material.SetShaderParameter("intensity", v);
        }), Intensity, target, FadeSeconds);
        if (target == 0f)
            _fade.TweenCallback(Callable.From(() => Visible = false));
    }
}
