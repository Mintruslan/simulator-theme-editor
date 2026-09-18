namespace SITheme;

/// <summary>
/// Creates the compatibility theme that preserves the current SImulator appearance and legacy color settings.
/// </summary>
public static class SimulatorDefaultTheme
{
    public static SimulatorThemeDocument Create(
        string textColor,
        string backgroundColor,
        string? fontFamily,
        string name = "Default SImulator Theme",
        string? backgroundImage = null)
    {
        const string resolvedFontFamily = "Standard";

        return new SimulatorThemeDocument
        {
            Name = name,
            Tokens = new SimulatorThemeTokens
            {
                Global = new SimulatorGlobalThemeTokens
                {
                    BackgroundColor = backgroundColor,
                    BackgroundImage = backgroundImage,
                    TextColor = textColor,
                },
                Typography = new SimulatorTypographyThemeTokens
                {
                    ThemeName = CreateTypography(60, 500, textColor, resolvedFontFamily),
                    FinalThemeName = CreateTypography(144, 500, textColor, resolvedFontFamily),
                    QuestionPrice = CreateTypography(144, 500, textColor, resolvedFontFamily),
                    QuestionText = CreateTypography(72, 500, textColor, resolvedFontFamily),
                    AnswerText = CreateTypography(72, 500, textColor, resolvedFontFamily),
                    PlayerName = CreateTypography(48, 700, textColor, resolvedFontFamily),
                    PlayerScore = CreateTypography(48, 700, textColor, resolvedFontFamily),
                    Timer = CreateTypography(16, 500, textColor, resolvedFontFamily),
                },
            },
        };
    }

    private static SimulatorTypographyToken CreateTypography(
        double fontSize,
        int fontWeight,
        string color,
        string fontFamily) => new()
    {
        FontSize = fontSize,
        FontWeight = fontWeight,
        Color = color,
        FontFamily = fontFamily,
    };

    public static string ConvertWpfToHtmlColor(string wpfColor)
    {
        if (wpfColor.Length == 9 && wpfColor.StartsWith('#'))
        {
            return $"#{wpfColor.Substring(3)}{wpfColor.Substring(1, 2)}";
        }

        return wpfColor;
    }
}
