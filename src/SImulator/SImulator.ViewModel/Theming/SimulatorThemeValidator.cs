using System.Text.RegularExpressions;

namespace SImulator.ViewModel.Theming;

/// <summary>
/// Validates theme documents before they cross a persistence or presentation boundary.
/// </summary>
public static partial class SimulatorThemeValidator
{
    public static bool IsValid(SimulatorThemeDocument? theme) => theme != null
        && theme.SchemaVersion == SimulatorThemeDocument.CurrentSchemaVersion
        && IsValidId(theme.Id)
        && !string.IsNullOrWhiteSpace(theme.Name)
        && theme.Assets != null
        && theme.Assets.Fonts != null
        && theme.Assets.Images != null
        && theme.Tokens != null
        && IsValidGlobal(theme.Tokens.Global)
        && IsValidTypography(theme.Tokens.Typography)
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

    private static bool IsValidTypography(SimulatorTypographyThemeTokens? tokens) => tokens != null
        && IsValidTypographyToken(tokens.ThemeName)
        && IsValidTypographyToken(tokens.FinalThemeName)
        && IsValidTypographyToken(tokens.QuestionPrice)
        && IsValidTypographyToken(tokens.QuestionText)
        && IsValidTypographyToken(tokens.AnswerText)
        && IsValidTypographyToken(tokens.PlayerName)
        && IsValidTypographyToken(tokens.PlayerScore)
        && IsValidTypographyToken(tokens.Timer);

    private static bool IsValidTypographyToken(SimulatorTypographyToken? token) => token != null
        && HasValue(token.FontFamily)
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
