using Godot;

namespace LeafSweeper;

/// <summary>
/// A yellow-sun lens flare for a prismatic bug's winning tap: an additive
/// core with cross streaks, slowly rotating rays and staggered expanding
/// rings. Main adds it as a child of the celebrated bug, so the bug's own
/// transform carries it — erupting at the winning tap, riding behind the
/// bug to its seat in the win card, and glowing there until it fades.
/// Plays once and frees itself.
/// </summary>
public partial class SunFlare : Node2D
{
    private const float Life = 1.8f;
    private const float CoreRadius = 54f;
    private const int RayCount = 12;

    private static readonly Color Sun = new(1f, 0.92f, 0.45f);
    private static readonly Color SunHot = new(1f, 1f, 0.85f);

    private float _age;

    public override void _Ready()
    {
        // Exactly one rung below the celebrated bug (relative z): the sun
        // glows behind the bug itself yet outshines everything else around
        // it, wherever the bug is — no cross-space position tracking.
        ZIndex = -1;
        ShowBehindParent = true;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        };
        Scale = new Vector2(0.2f, 0.2f);
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One, 0.35f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Life)
        {
            QueueFree();
            return;
        }
        Rotation += (float)delta * 0.6f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_age / Life, 0f, 1f);
        float fadeIn = Mathf.Clamp(_age / 0.25f, 0f, 1f);
        float fadeOut = 1f - Mathf.SmoothStep(0.6f, 1f, t);
        float a = fadeIn * fadeOut;
        if (a <= 0.01f)
            return;

        // Core: hot center with a soft halo.
        DrawCircle(Vector2.Zero, CoreRadius * 1.9f, new Color(Sun, 0.10f * a));
        DrawCircle(Vector2.Zero, CoreRadius * 1.2f, new Color(Sun, 0.22f * a));
        DrawCircle(Vector2.Zero, CoreRadius * 0.55f, new Color(SunHot, 0.5f * a));
        DrawCircle(Vector2.Zero, CoreRadius * 0.28f, new Color(SunHot, 0.9f * a));

        // Cross streaks, turning slowly with the rays.
        var streak = new Vector2(640f, 7f);
        DrawRect(new Rect2(-streak / 2f, streak), new Color(Sun, 0.35f * a));
        DrawRect(new Rect2(-new Vector2(7f, 640f) / 2f, new Vector2(7f, 640f)),
            new Color(Sun, 0.2f * a));

        // Rotating rays: thin triangles fading out along their length.
        for (int i = 0; i < RayCount; i++)
        {
            float angle = Mathf.Tau * i / RayCount;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var side = dir.Orthogonal() * 16f;
            var tip = dir * 300f;
            var col = new Color(Sun, 0.16f * a);
            DrawColoredPolygon(
                new[] { side, -side, tip }, col);
        }

        // Expanding rings, staggered so they pulse outward one after another.
        for (int r = 0; r < 3; r++)
        {
            float phase = Mathf.Clamp((_age - r * 0.28f) / 1.1f, 0f, 1f);
            if (phase <= 0f || phase >= 1f)
                continue;
            DrawArc(Vector2.Zero, 60f + phase * 300f, 0f, Mathf.Tau, 64,
                new Color(Sun, 0.35f * (1f - phase) * fadeIn), 3.5f);
        }
    }
}
