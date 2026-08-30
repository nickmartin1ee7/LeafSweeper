using Godot;

namespace LeafSweeper;

/// <summary>
/// Grandiose win-card dressing: slowly rotating sun rays plus a loop of
/// twinkling sparkles, drawn additively behind the card's content.
/// </summary>
public partial class PrismaticGlow : Control
{
    private const int RayCount = 14;

    private static readonly Color RayColor = new(1f, 0.88f, 0.45f);
    private static readonly Color SparkColor = new(1f, 0.97f, 0.8f);

    private float _time;

    // Fixed sparkle layout (unit space): position, phase offset, speed.
    private readonly (Vector2 Pos, float Phase, float Speed)[] _sparks =
    {
        (new Vector2(0.12f, 0.18f), 0.0f, 1.3f),
        (new Vector2(0.85f, 0.12f), 1.1f, 1.7f),
        (new Vector2(0.68f, 0.55f), 2.2f, 1.1f),
        (new Vector2(0.2f, 0.72f), 3.0f, 1.5f),
        (new Vector2(0.5f, 0.3f), 4.1f, 1.9f),
        (new Vector2(0.9f, 0.7f), 5.0f, 1.2f),
        (new Vector2(0.33f, 0.45f), 5.9f, 1.6f),
        (new Vector2(0.75f, 0.85f), 6.8f, 1.4f),
    };

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size / 2f;
        float reach = Mathf.Max(Size.X, Size.Y) * 0.85f;

        // Rotating rays behind the card content.
        for (int i = 0; i < RayCount; i++)
        {
            float angle = Mathf.Tau * i / RayCount + _time * 0.25f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var side = dir.Orthogonal() * reach * 0.035f;
            var tip = center + dir * reach;
            var col = new Color(RayColor, 0.10f);
            DrawColoredPolygon(
                new[] { center + side, center - side, tip }, col);
        }

        // Looping sparkles: tiny diamonds that swell and fade out of phase.
        foreach (var (pos, phase, speed) in _sparks)
        {
            float twinkle = 0.5f - 0.5f * Mathf.Cos(_time * speed + phase);
            twinkle = Mathf.Pow(twinkle, 2.2f);
            if (twinkle < 0.03f)
                continue;
            float r = 5f + twinkle * 7f;
            var p = pos * Size;
            var col = new Color(SparkColor, 0.25f + 0.7f * twinkle);
            DrawColoredPolygon(
                new[]
                {
                    p + new Vector2(0, -r), p + new Vector2(r, 0),
                    p + new Vector2(0, r), p + new Vector2(-r, 0),
                }, col);
        }
    }
}
