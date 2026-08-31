using Godot;

namespace LeafSweeper;

/// <summary>
/// Season-intro banner: a soft card near the top of the screen announcing
/// a season change ("Fall — The rains are coming") or the year-loop bonus
/// ("Year 2 begins — +2 Sweep Power · +1 Gust Coin"). Simple labels with a
/// modulate fade — the neon storm and prismatic signs already own the
/// flashy canvas, so this one stays a calm, warm note. Owns canvas layer
/// 7 — explicit ladder: world 0 → season grade 1 → storm 2 → menu 3 → hud 4 →
/// warn 5 → prismatic 6 → season banner 7 → bug book 90.
/// </summary>
public partial class SeasonBanner : CanvasLayer
{
    // A gentle entrance: the banner breathes in slower than the neon
    // signs, holds long enough to read, then dissolves politely.
    private const float FadeSeconds = 0.6f;
    private const float HoldSeconds = 2.2f;
    private const float OutSeconds = 1.2f;

    // Card geometry: a fixed-fraction width, hanging below the HUD's
    // level/sweep labels so it covers nothing the player is reading. The
    // height is NOT set — the card sizes to its text (a fixed height once
    // clamped against the wrapped label's minimum size and stretched the
    // card into a full-screen column in portrait).
    private const float WidthFraction = 0.62f;
    private const float TopAnchor = 0.16f;

    // Season accents — warm-cozy values that sit with the cream/gold UI
    // palette. The loop bonus gets its own gold: the year's reward must
    // read as a celebration, not a reset.
    private static readonly Color SpringAccent = new("6f9a44");
    private static readonly Color SummerAccent = new("d9a441");
    private static readonly Color FallAccent = new("b56a32");
    private static readonly Color WinterAccent = new("7fa8c9");
    private static readonly Color LoopAccent = new("c9a227");

    private Control _root = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _subtitle = null!;
    private Tween _fade = null!;

    // Autoplay reads the tweened alpha to prove the fade is actually
    // running (same pattern as the storm sign).
    public float FadeAlpha => _root.Modulate.A;

    public SeasonBanner()
    {
        Layer = 7;
        Visible = false;
    }

    public override void _Ready()
    {
        // Everything hangs off one root so fading is a single Modulate.
        _root = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        // Anchors AND offsets — SetAnchorsPreset alone preserves the old
        // zero rect (the storm overlay's invisible-storm bug).
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        // Soft cream card with the win panel's leather-gold border — the
        // season note is good news, so it borrows the win card's warmth.
        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("f7f0e1"),
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            ContentMarginLeft = 40,
            ContentMarginRight = 40,
            ContentMarginTop = 26,
            ContentMarginBottom = 26,
            BorderWidthBottom = 6,
            BorderWidthTop = 6,
            BorderWidthLeft = 6,
            BorderWidthRight = 6,
            BorderColor = new Color("c9a06a"),
        });
        _root.AddChild(_panel);

        // The box hangs straight off the panel (PanelContainer stretches
        // it to fill), centered vertically. A CenterContainer is the trap
        // here: it sizes children to their minimum size, and an autowrap
        // label's minimum width collapses to about one character — the
        // card became a one-letter-per-line column in portrait. Explicit
        // label widths (LayoutPanel) keep the wrap honest.
        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _title = Hud.MakeLabel(72, true);
        _subtitle = Hud.MakeLabel(44, false, new Color("4a3a26"));
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        // The card is a fixed-width box; long lines wrap inside it.
        _title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_title);
        box.AddChild(_subtitle);
        _panel.AddChild(box);
    }

    /// <summary>Announces a season with its flavor line and accent color.</summary>
    public void ShowSeason(RoundConfig.Season season)
    {
        var (title, flavor) = season switch
        {
            RoundConfig.Season.Spring => ("Spring", "The forest floor wakes"),
            RoundConfig.Season.Summer => ("Summer", "Storms gather over the meadow"),
            RoundConfig.Season.Fall => ("Fall", "The rains are coming"),
            _ => ("Winter", "Snow settles on the leaves"),
        };
        ShowBanner(title, flavor, AccentFor(season));
    }

    /// <summary>
    /// Loop-restart bonus card — the year's reward must feel like a
    /// reward, not a reset.
    /// </summary>
    public void ShowLoopBonus(int loopIndex) =>
        ShowBanner($"Year {loopIndex + 1} begins",
            $"+{RoundConfig.LoopSweepPowerBonus} Sweep Power · " +
            $"+{RoundConfig.LoopGustCoinBonus} Gust Coin", LoopAccent);

    /// <summary>Fades the banner in, holds it, fades it out and hides it.</summary>
    public void ShowBanner(string title, string subtitle, Color accent)
    {
        _title.Text = title;
        _title.LabelSettings.FontColor = accent;
        _subtitle.Text = subtitle;
        LayoutPanel();
        KillFade();
        Visible = true;
        _root.Modulate = new Color(1f, 1f, 1f, 0f);
        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", 1f, FadeSeconds);
        _fade.TweenInterval(HoldSeconds);
        _fade.TweenProperty(_root, "modulate:a", 0f, OutSeconds);
        _fade.TweenCallback(Callable.From(() => Visible = false));
    }

    /// <summary>Hides the banner at once (menu returns, round teardown).</summary>
    public void HideBanner()
    {
        KillFade();
        Visible = false;
    }

    private static Color AccentFor(RoundConfig.Season season) => season switch
    {
        RoundConfig.Season.Spring => SpringAccent,
        RoundConfig.Season.Summer => SummerAccent,
        RoundConfig.Season.Fall => FallAccent,
        _ => WinterAccent,
    };

    // Offsets are pixels, so the fractional card geometry resolves against
    // the live viewport on each show (the banner is transient and follows
    // window resizes lazily, like the storm sign). The card anchors to a
    // single point — centered horizontally, top at TopAnchor — and grows
    // from there: width via a custom minimum, height purely from its
    // text, so long flavor lines make the card taller without ever
    // stretching it off-screen.
    private void LayoutPanel()
    {
        Vector2 view = GetViewport().GetVisibleRect().Size;
        float w = view.X * WidthFraction;
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorTop = TopAnchor;
        _panel.AnchorBottom = TopAnchor;
        _panel.OffsetLeft = 0f;
        _panel.OffsetRight = 0f;
        _panel.OffsetTop = 0f;
        _panel.OffsetBottom = 0f;
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.End;
        _panel.CustomMinimumSize = new Vector2(w, 0f);
        // Explicit wrap width for the labels: without it an autowrap
        // label's minimum width collapses toward one character and its
        // minimum height explodes into one-line-per-character (the
        // portrait bug). Style margins are 2 × 40 + 2 × 6 border.
        float textWidth = Mathf.Max(w - 92f, 80f);
        _title.CustomMinimumSize = new Vector2(textWidth, 0f);
        _subtitle.CustomMinimumSize = new Vector2(textWidth, 0f);
    }

    private void KillFade()
    {
        if (_fade != null && _fade.IsValid())
            _fade.Kill();
    }
}
