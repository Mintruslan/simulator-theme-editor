namespace SImulator.ViewModel.Theming;

/// <summary>
/// Defines a portable, versioned visual theme for the SImulator presentation surface.
/// </summary>
public sealed class SimulatorThemeDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Id { get; init; } = "simulator-default";

    public string Name { get; init; } = "Default SImulator Theme";

    public string? BasedOn { get; init; }

    public SimulatorThemeAssets Assets { get; init; } = new();

    public SimulatorThemeTokens Tokens { get; init; } = new();
}

public sealed class SimulatorThemeAssets
{
    public Dictionary<string, string> Fonts { get; init; } = [];

    public Dictionary<string, string> Images { get; init; } = [];
}

public sealed class SimulatorThemeTokens
{
    public SimulatorGlobalThemeTokens Global { get; init; } = new();

    public SimulatorTypographyThemeTokens Typography { get; init; } = new();

    public SimulatorBoardThemeTokens Board { get; init; } = new();

    public SimulatorQuestionThemeTokens Question { get; init; } = new();

    public SimulatorPlayersThemeTokens Players { get; init; } = new();

    public SimulatorEffectsThemeTokens Effects { get; init; } = new();
}

public sealed class SimulatorGlobalThemeTokens
{
    public string BackgroundColor { get; init; } = "#0A0E30";

    public string? BackgroundImage { get; init; }

    public string BackgroundFit { get; init; } = "cover";

    public string BackgroundPosition { get; init; } = "center";

    public string TextColor { get; init; } = "#FFFFFF";

    public string AccentColor { get; init; } = "#FF8C00";

    public double BorderRadius { get; init; } = 4;

    public double Scale { get; init; } = 1;
}

public sealed class SimulatorTypographyThemeTokens
{
    public SimulatorTypographyToken ThemeName { get; init; } = SimulatorTypographyToken.Create(60, 500);

    public SimulatorTypographyToken FinalThemeName { get; init; } = SimulatorTypographyToken.Create(144, 500);

    public SimulatorTypographyToken QuestionPrice { get; init; } = SimulatorTypographyToken.Create(144, 500);

    public SimulatorTypographyToken QuestionText { get; init; } = SimulatorTypographyToken.Create(72, 500);

    public SimulatorTypographyToken AnswerText { get; init; } = SimulatorTypographyToken.Create(72, 500);

    public SimulatorTypographyToken PlayerName { get; init; } = SimulatorTypographyToken.Create(48, 700);

    public SimulatorTypographyToken PlayerScore { get; init; } = SimulatorTypographyToken.Create(48, 700);

    public SimulatorTypographyToken Timer { get; init; } = SimulatorTypographyToken.Create(16, 500);
}

public sealed class SimulatorTypographyToken
{
    public string FontFamily { get; init; } = "Standard";

    public double FontSize { get; init; }

    public bool AutoSize { get; init; } = true;

    public int FontWeight { get; init; } = 500;

    public string LineHeight { get; init; } = "normal";

    public double LetterSpacing { get; init; }

    public string Color { get; init; } = "#FFFFFF";

    public string TextAlign { get; init; } = "center";

    public string TextTransform { get; init; } = "none";

    public string TextShadow { get; init; } = "2px 2px 4px rgba(0, 0, 0, 0.7)";

    public bool TextShadowEnabled { get; init; } = true;

    public static SimulatorTypographyToken Create(double fontSize, int fontWeight, string color = "#FFFFFF") => new()
    {
        FontSize = fontSize,
        FontWeight = fontWeight,
        Color = color,
    };
}

public sealed class SimulatorBoardThemeTokens
{
    public double Gap { get; init; } = 4;

    public double Padding { get; init; } = 2;

    public string CellBackground { get; init; } = "transparent";

    public string ThemeHeaderBackground { get; init; } = "rgba(255, 255, 255, 0.05)";

    public double BorderWidth { get; init; } = 2;

    public bool BordersVisible { get; init; } = true;

    public string BorderColor { get; init; } = "transparent";

    public double BorderRadius { get; init; } = 4;

    public string HoverBackground { get; init; } = "rgba(255, 255, 255, 0.3)";

    public string SelectedBackground { get; init; } = "rgba(255, 255, 255, 0.15)";

    public double AnsweredOpacity { get; init; }
}

public sealed class SimulatorQuestionThemeTokens
{
    public string Background { get; init; } = "transparent";

    public string ContentWidth { get; init; } = "100%";

    public double Padding { get; init; } = 5;

    public string Alignment { get; init; } = "center";

    public string ImageFit { get; init; } = "contain";
}

public sealed class SimulatorPlayersThemeTokens
{
    public string Layout { get; init; } = "horizontal";

    public double Gap { get; init; }

    public string CardBackground { get; init; } = "linear-gradient(135deg, rgba(20, 24, 62, 0.4), rgba(5, 6, 31, 0.6))";

    public string CardBorderColor { get; init; } = "rgba(255, 255, 255, 0.15)";

    public double CardBorderWidth { get; init; } = 1;

    public double CardBorderRadius { get; init; } = 10;

    public string BuzzerBackground { get; init; } = "linear-gradient(135deg, rgba(236, 232, 164, 0.75), rgba(182, 170, 67, 0.95))";

    public string CorrectBackground { get; init; } = "linear-gradient(135deg, rgba(76, 175, 80, 0.75), rgba(46, 125, 50, 0.95))";

    public string IncorrectBackground { get; init; } = "linear-gradient(135deg, rgba(244, 67, 54, 0.75), rgba(198, 40, 40, 0.95))";
}

public sealed class SimulatorEffectsThemeTokens
{
    public string Shadow { get; init; } = "0 4px 12px rgba(0, 0, 0, 0.3)";

    public double TransitionDuration { get; init; } = 300;

    public double MotionScale { get; init; } = 1;
}
