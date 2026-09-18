using System.Text.RegularExpressions;

namespace SITheme;

/// <summary>
/// Validates theme documents before they cross a persistence or presentation boundary.
/// </summary>
public static partial class SimulatorThemeValidator
{
    private const int MaxFontCount = 8;
    private const int MaxFontFileSize = 5 * 1024 * 1024;
    private const int MaxFontDataUrlLength = (((MaxFontFileSize + 2) / 3) * 4) + 64;
    private const string BundledFontFamily = "Standard";

    private static readonly string[] SupportedFontDataUrlPrefixes =
    [
        "data:font/ttf;base64,",
        "data:font/otf;base64,",
        "data:font/woff;base64,",
        "data:font/woff2;base64,",
        "data:application/x-font-ttf;base64,",
        "data:application/x-font-opentype;base64,",
        "data:application/font-woff;base64,",
        "data:application/font-woff2;base64,",
        "data:application/octet-stream;base64,",
    ];

    public static bool IsValid(SimulatorThemeDocument? theme) => theme != null
        && theme.SchemaVersion == SimulatorThemeDocument.CurrentSchemaVersion
        && IsValidId(theme.Id)
        && !string.IsNullOrWhiteSpace(theme.Name)
        && theme.Assets != null
        && theme.Assets.Fonts != null
        && IsValidFonts(theme.Assets.Fonts)
        && theme.Assets.Images != null
        && theme.Tokens != null
        && IsValidGlobal(theme.Tokens.Global)
        && IsValidTypography(theme.Tokens.Typography, theme.Assets.Fonts)
        && IsValidBoard(theme.Tokens.Board)
        && IsValidQuestion(theme.Tokens.Question)
        && IsValidPlayers(theme.Tokens.Players)
        && IsValidEffects(theme.Tokens.Effects);

    public static bool IsValidId(string? id) => id != null && ThemeIdExpression().IsMatch(id);

    private static bool IsValidGlobal(SimulatorGlobalThemeTokens? tokens) => tokens != null
        && HasValue(tokens.BackgroundColor)
        && (tokens.BackgroundImage == null || HasValue(tokens.BackgroundImage))
        && new[] { "cover", "contain", "stretch", "center" }.Contains(tokens.BackgroundFit)
        && HasValue(tokens.BackgroundPosition)
        && HasValue(tokens.TextColor)
        && HasValue(tokens.AccentColor)
        && IsNonNegative(tokens.BorderRadius)
        && tokens.Scale > 0;

    private static bool IsValidFonts(IReadOnlyDictionary<string, string> fonts) => fonts.Count <= MaxFontCount
        && fonts.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == fonts.Count
        && fonts.All(pair => IsValidFontFamilyName(pair.Key) && IsValidFontDataUrl(pair.Value));

    private static bool IsValidFontFamilyName(string fontFamily) => HasValue(fontFamily)
        && fontFamily.Length <= 64
        && !fontFamily.Equals(BundledFontFamily, StringComparison.OrdinalIgnoreCase)
        && !fontFamily.Any(character => char.IsControl(character) || "{};\"\\".Contains(character));

    private static bool IsValidFontDataUrl(string? source) => source != null
        && source.Length <= MaxFontDataUrlLength
        && SupportedFontDataUrlPrefixes.Any(prefix =>
            source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && source.Length > prefix.Length);

    private static bool IsValidTypography(
        SimulatorTypographyThemeTokens? tokens,
        IReadOnlyDictionary<string, string> fonts) => tokens != null
        && IsValidTypographyToken(tokens.ThemeName, fonts)
        && IsValidTypographyToken(tokens.FinalThemeName, fonts)
        && IsValidTypographyToken(tokens.QuestionPrice, fonts)
        && IsValidTypographyToken(tokens.QuestionText, fonts)
        && IsValidTypographyToken(tokens.AnswerText, fonts)
        && IsValidTypographyToken(tokens.PlayerName, fonts)
        && IsValidTypographyToken(tokens.PlayerScore, fonts)
        && IsValidTypographyToken(tokens.Timer, fonts);

    private static bool IsValidTypographyToken(
        SimulatorTypographyToken? token,
        IReadOnlyDictionary<string, string> fonts) => token != null
        && HasValue(token.FontFamily)
        && (token.FontFamily == BundledFontFamily || fonts.ContainsKey(token.FontFamily))
        && token.FontSize > 0
        && token.FontWeight is >= 1 and <= 1000
        && HasValue(token.LineHeight)
        && HasValue(token.Color)
        && new[] { "left", "center", "right" }.Contains(token.TextAlign)
        && new[] { "none", "uppercase", "lowercase" }.Contains(token.TextTransform)
        && token.TextShadow != null;

    private static bool IsValidBoard(SimulatorBoardThemeTokens? tokens) => tokens != null
        && IsNonNegative(tokens.Gap)
        && IsNonNegative(tokens.Padding)
        && HasValue(tokens.CellBackground)
        && HasValue(tokens.ThemeHeaderBackground)
        && IsNonNegative(tokens.BorderWidth)
        && HasValue(tokens.BorderColor)
        && IsNonNegative(tokens.BorderRadius)
        && HasValue(tokens.HoverBackground)
        && HasValue(tokens.SelectedBackground)
        && tokens.AnsweredOpacity is >= 0 and <= 1;

    private static bool IsValidQuestion(SimulatorQuestionThemeTokens? tokens) => tokens != null
        && HasValue(tokens.Background)
        && HasValue(tokens.ContentWidth)
        && IsNonNegative(tokens.Padding)
        && new[] { "left", "center", "right" }.Contains(tokens.Alignment)
        && new[] { "cover", "contain", "fill" }.Contains(tokens.ImageFit);

    private static bool IsValidPlayers(SimulatorPlayersThemeTokens? tokens) => tokens != null
        && new[] { "horizontal", "vertical" }.Contains(tokens.Layout)
        && IsNonNegative(tokens.Gap)
        && HasValue(tokens.CardBackground)
        && HasValue(tokens.CardBorderColor)
        && IsNonNegative(tokens.CardBorderWidth)
        && IsNonNegative(tokens.CardBorderRadius)
        && HasValue(tokens.BuzzerBackground)
        && HasValue(tokens.CorrectBackground)
        && HasValue(tokens.IncorrectBackground);

    private static bool IsValidEffects(SimulatorEffectsThemeTokens? tokens) => tokens != null
        && tokens.Shadow != null
        && IsNonNegative(tokens.TransitionDuration)
        && IsNonNegative(tokens.MotionScale);

    private static bool IsNonNegative(double value) => double.IsFinite(value) && value >= 0;

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ThemeIdExpression();
}
