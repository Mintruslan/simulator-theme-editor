using SITheme;
using SIGame.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace SIGame.Implementation;

/// <summary>
/// Projects portable presentation tokens onto the native SIGame WPF presentation resources.
/// </summary>
internal sealed partial class PresentationThemeApplier : IDisposable
{
    private const string StandardFontUri = "pack://application:,,,/SIGame;component/Fonts/#Jost";

    private readonly ThemeSettings _settings;
    private readonly IThemeRepository _repository;
    private readonly ResourceDictionary _resources;
    private readonly string _fontCacheDirectory;

    public PresentationThemeApplier(
        ThemeSettings settings,
        IThemeRepository repository,
        ResourceDictionary resources,
        string fontCacheDirectory)
    {
        _settings = settings;
        _repository = repository;
        _resources = resources;
        _fontCacheDirectory = fontCacheDirectory;

        _settings.PropertyChanged += Settings_PropertyChanged;
        _settings.UISettings.PropertyChanged += UISettings_PropertyChanged;
    }

    public void Initialize()
    {
        var theme = _settings.PresentationThemeId == ThemeSettings.DefaultPresentationThemeId
            ? null
            : _repository.TryGet(_settings.PresentationThemeId);

        if (theme == null && _settings.PresentationThemeId != ThemeSettings.DefaultPresentationThemeId)
        {
            _settings.PresentationThemeId = ThemeSettings.DefaultPresentationThemeId;
        }

        _settings.PresentationTheme = theme;
        Apply(theme ?? _settings.CreateDefaultPresentationTheme());
    }

    public void Dispose()
    {
        _settings.PropertyChanged -= Settings_PropertyChanged;
        _settings.UISettings.PropertyChanged -= UISettings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThemeSettings.PresentationTheme) || _settings.PresentationTheme == null)
        {
            Apply(_settings.PresentationTheme ?? _settings.CreateDefaultPresentationTheme());
        }
    }

    private void UISettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_settings.PresentationTheme == null)
        {
            Apply(_settings.CreateDefaultPresentationTheme());
        }
    }

    internal void Apply(SimulatorThemeDocument theme)
    {
        var global = theme.Tokens.Global;
        var board = theme.Tokens.Board;
        var question = theme.Tokens.Question;
        var players = theme.Tokens.Players;

        Set("SIThemeGlobalBackgroundBrush", CreateBrush(global.BackgroundColor));
        Set("SIThemeGlobalBackgroundImageBrush", CreateImageBrush(global.BackgroundImage, global.BackgroundFit, global.BackgroundPosition));
        Set("SIThemeGlobalTextBrush", CreateBrush(global.TextColor));
        Set("SIThemeAccentBrush", CreateBrush(global.AccentColor));

        var plainTextOnly = board.PlainTextOnly;
        var showBorders = board.BordersVisible && !plainTextOnly;
        Set("SIThemeBoardCellBackground", plainTextOnly ? Brushes.Transparent : CreateBrush(board.CellBackground));
        Set("SIThemeBoardHeaderBackground", plainTextOnly ? Brushes.Transparent : CreateBrush(board.ThemeHeaderBackground));
        Set("SIThemeBoardHoverBackground", plainTextOnly ? Brushes.Transparent : CreateBrush(board.HoverBackground));
        Set("SIThemeBoardSelectedBackground", plainTextOnly ? Brushes.Transparent : CreateBrush(board.SelectedBackground));
        Set("SIThemeBoardBorderBrush", showBorders ? CreateBrush(board.BorderColor) : Brushes.Transparent);
        Set("SIThemeBoardBorderThickness", new Thickness(showBorders ? board.BorderWidth : 0));
        Set("SIThemeBoardCornerRadius", new CornerRadius(plainTextOnly ? 0 : board.BorderRadius));
        Set("SIThemeBoardCellMargin", new Thickness(plainTextOnly ? 0 : board.Gap / 2));
        Set("SIThemeBoardPadding", new Thickness(plainTextOnly ? 0 : board.Padding));
        Set("SIThemeBoardAnsweredOpacity", board.AnsweredOpacity);

        Set("SIThemeQuestionBackground", CreateBrush(question.Background));
        Set("SIThemeQuestionPadding", new Thickness(question.Padding));
        Set("SIThemeQuestionTextAlignment", ToTextAlignment(question.Alignment));
        Set("SIThemeQuestionHorizontalAlignment", ToHorizontalAlignment(question.Alignment));
        Set("SIThemeQuestionImageStretch", ToImageStretch(question.ImageFit));
        SetQuestionContentWidths(question.ContentWidth, question.Alignment);

        Set("SIThemePlayersPanel", CreatePlayersPanel(players.Layout));
        Set("SIThemePlayersGap", new Thickness(players.Gap / 2));
        Set("SIThemePlayerCardBackground", CreateBrush(players.CardBackground));
        Set("SIThemePlayerCardBorderBrush", CreateBrush(players.CardBorderColor));
        Set("SIThemePlayerCardBorderThickness", new Thickness(players.CardBorderWidth));
        Set("SIThemePlayerCardCornerRadius", new CornerRadius(players.CardBorderRadius));
        Set("SIThemePlayerBuzzerBackground", CreateBrush(players.BuzzerBackground));
        Set("SIThemePlayerCorrectBackground", CreateBrush(players.CorrectBackground));
        Set("SIThemePlayerIncorrectBackground", CreateBrush(players.IncorrectBackground));

        ApplyTypography("ThemeName", theme.Tokens.Typography.ThemeName, theme.Assets.Fonts);
        ApplyTypography("FinalThemeName", theme.Tokens.Typography.FinalThemeName, theme.Assets.Fonts);
        ApplyTypography("QuestionPrice", theme.Tokens.Typography.QuestionPrice, theme.Assets.Fonts);
        ApplyTypography("QuestionText", theme.Tokens.Typography.QuestionText, theme.Assets.Fonts);
        ApplyTypography("AnswerText", theme.Tokens.Typography.AnswerText, theme.Assets.Fonts);
        ApplyTypography("PlayerName", theme.Tokens.Typography.PlayerName, theme.Assets.Fonts);
        ApplyTypography("PlayerScore", theme.Tokens.Typography.PlayerScore, theme.Assets.Fonts);
        ApplyTypography("Timer", theme.Tokens.Typography.Timer, theme.Assets.Fonts);
    }

    private void ApplyTypography(
        string role,
        SimulatorTypographyToken token,
        IReadOnlyDictionary<string, string> fonts)
    {
        Set($"SITheme{role}FontFamily", ResolveFontFamily(token.FontFamily, fonts));
        Set($"SITheme{role}FontSize", token.FontSize);
        Set($"SITheme{role}AutoSize", token.AutoSize);
        Set($"SITheme{role}FontWeight", FontWeight.FromOpenTypeWeight(token.FontWeight));
        Set($"SITheme{role}Foreground", CreateBrush(token.Color));
        Set($"SITheme{role}TextAlignment", ToTextAlignment(token.TextAlign));
        Set($"SITheme{role}HorizontalAlignment", ToHorizontalAlignment(token.TextAlign));
        Set($"SITheme{role}TextEffect", CreateTextEffect(token));
    }

    private void Set(string key, object? value) => _resources[key] = value;

    private void SetQuestionContentWidths(string contentWidth, string alignment)
    {
        var percent = 0.0;
        var pixels = 0.0;
        var isPercent = contentWidth.EndsWith('%')
            && double.TryParse(contentWidth[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out percent);
        var isPixels = contentWidth.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(contentWidth[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out pixels);

        GridLength content;
        double remaining;

        if (isPercent)
        {
            percent = Math.Clamp(percent, 1, 100);
            content = new GridLength(percent, GridUnitType.Star);
            remaining = 100 - percent;
        }
        else if (isPixels)
        {
            content = new GridLength(Math.Max(1, pixels), GridUnitType.Pixel);
            remaining = 1;
        }
        else
        {
            content = new GridLength(100, GridUnitType.Star);
            remaining = 0;
        }

        var leading = alignment == "left" ? 0 : alignment == "right" ? remaining : remaining / 2;
        var trailing = alignment == "right" ? 0 : alignment == "left" ? remaining : remaining / 2;
        Set("SIThemeQuestionLeadingWidth", new GridLength(leading, GridUnitType.Star));
        Set("SIThemeQuestionContentWidth", content);
        Set("SIThemeQuestionTrailingWidth", new GridLength(trailing, GridUnitType.Star));
    }

    private FontFamily ResolveFontFamily(string familyName, IReadOnlyDictionary<string, string> fonts)
    {
        if (familyName == "Standard" || !fonts.TryGetValue(familyName, out var source))
        {
            return new FontFamily(StandardFontUri);
        }

        try
        {
            var separatorIndex = source.IndexOf(',');
            var header = separatorIndex > 0 ? source[..separatorIndex] : "";

            if (!header.Contains("font/ttf", StringComparison.OrdinalIgnoreCase)
                && !header.Contains("font/otf", StringComparison.OrdinalIgnoreCase)
                && !header.Contains("x-font-ttf", StringComparison.OrdinalIgnoreCase)
                && !header.Contains("x-font-opentype", StringComparison.OrdinalIgnoreCase)
                && !header.Contains("octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                return new FontFamily(StandardFontUri);
            }

            var bytes = Convert.FromBase64String(source[(separatorIndex + 1)..]);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var extension = header.Contains("otf", StringComparison.OrdinalIgnoreCase) ? ".otf" : ".ttf";
            var fontDirectory = Path.Combine(_fontCacheDirectory, hash);
            var fontPath = Path.Combine(fontDirectory, $"font{extension}");

            Directory.CreateDirectory(fontDirectory);

            if (!File.Exists(fontPath))
            {
                File.WriteAllBytes(fontPath, bytes);
            }

            var fontFamily = Fonts.GetFontFamilies(new Uri(fontDirectory + Path.DirectorySeparatorChar)).FirstOrDefault();
            return fontFamily ?? new FontFamily(StandardFontUri);
        }
        catch (Exception exception) when (exception is FormatException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new FontFamily(StandardFontUri);
        }
    }

    internal static Brush CreateBrush(string cssValue)
    {
        if (string.IsNullOrWhiteSpace(cssValue) || cssValue.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return Brushes.Transparent;
        }

        if (cssValue.StartsWith("linear-gradient", StringComparison.OrdinalIgnoreCase))
        {
            var colors = CssColorExpression().Matches(cssValue)
                .Select(match => TryParseColor(match.Value, out var color) ? color : (Color?)null)
                .Where(color => color.HasValue)
                .Select(color => color!.Value)
                .Take(2)
                .ToArray();

            if (colors.Length == 2)
            {
                return new LinearGradientBrush(colors[0], colors[1], new Point(0, 0), new Point(1, 1));
            }
        }

        return TryParseColor(cssValue, out var parsedColor)
            ? new SolidColorBrush(parsedColor)
            : Brushes.Transparent;
    }

    private static Brush CreateImageBrush(string? source, string fit, string position)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Brushes.Transparent;
        }

        try
        {
            BitmapImage image;

            if (source.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var separatorIndex = source.IndexOf(',');
                var bytes = Convert.FromBase64String(source[(separatorIndex + 1)..]);
                using var stream = new MemoryStream(bytes);
                image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
            }
            else
            {
                var uri = Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri)
                    ? absoluteUri
                    : new Uri(Path.GetFullPath(source));

                image = new BitmapImage(uri);
            }

            var brush = new ImageBrush(image)
            {
                Stretch = fit switch
                {
                    "cover" => Stretch.UniformToFill,
                    "contain" => Stretch.Uniform,
                    "stretch" => Stretch.Fill,
                    _ => Stretch.None,
                },
                AlignmentX = position.Contains("left", StringComparison.OrdinalIgnoreCase)
                    ? AlignmentX.Left
                    : position.Contains("right", StringComparison.OrdinalIgnoreCase) ? AlignmentX.Right : AlignmentX.Center,
                AlignmentY = position.Contains("top", StringComparison.OrdinalIgnoreCase)
                    ? AlignmentY.Top
                    : position.Contains("bottom", StringComparison.OrdinalIgnoreCase) ? AlignmentY.Bottom : AlignmentY.Center,
            };

            return brush;
        }
        catch (Exception exception) when (exception is FormatException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Brushes.Transparent;
        }
    }

    private static Effect? CreateTextEffect(SimulatorTypographyToken token)
    {
        if (!token.TextShadowEnabled || token.TextShadow == "none")
        {
            return null;
        }

        var match = CssTextShadowExpression().Match(token.TextShadow);

        if (!match.Success || !TryParseColor(match.Groups[4].Value, out var color))
        {
            return new DropShadowEffect { BlurRadius = 4, ShadowDepth = 2, Opacity = 0.7 };
        }

        var x = ParseInvariantDouble(match.Groups[1].Value);
        var y = ParseInvariantDouble(match.Groups[2].Value);
        var blur = ParseInvariantDouble(match.Groups[3].Value);
        var alpha = color.A / 255.0;
        color.A = byte.MaxValue;

        return new DropShadowEffect
        {
            BlurRadius = Math.Max(0, blur),
            ShadowDepth = Math.Sqrt((x * x) + (y * y)),
            Direction = Math.Atan2(-y, x) * 180 / Math.PI,
            Color = color,
            Opacity = alpha,
            RenderingBias = RenderingBias.Performance,
        };
    }

    private static ItemsPanelTemplate CreatePlayersPanel(string layout)
    {
        var panel = new FrameworkElementFactory(typeof(UniformGrid));
        panel.SetValue(layout == "vertical" ? UniformGrid.ColumnsProperty : UniformGrid.RowsProperty, 1);
        return new ItemsPanelTemplate(panel);
    }

    private static bool TryParseColor(string value, out Color color)
    {
        var rgbaMatch = CssRgbaExpression().Match(value.Trim());

        if (rgbaMatch.Success)
        {
            var alpha = Math.Clamp(ParseInvariantDouble(rgbaMatch.Groups[4].Value), 0, 1);
            color = Color.FromArgb(
                (byte)Math.Round(alpha * byte.MaxValue),
                byte.Parse(rgbaMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                byte.Parse(rgbaMatch.Groups[2].Value, CultureInfo.InvariantCulture),
                byte.Parse(rgbaMatch.Groups[3].Value, CultureInfo.InvariantCulture));
            return true;
        }

        try
        {
            color = (Color)ColorConverter.ConvertFromString(value)!;
            return true;
        }
        catch (FormatException)
        {
            color = Colors.Transparent;
            return false;
        }
    }

    private static double ParseInvariantDouble(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static TextAlignment ToTextAlignment(string alignment) => alignment switch
    {
        "left" => TextAlignment.Left,
        "right" => TextAlignment.Right,
        _ => TextAlignment.Center,
    };

    private static HorizontalAlignment ToHorizontalAlignment(string alignment) => alignment switch
    {
        "left" => HorizontalAlignment.Left,
        "right" => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Center,
    };

    private static Stretch ToImageStretch(string fit) => fit switch
    {
        "cover" => Stretch.UniformToFill,
        "fill" => Stretch.Fill,
        _ => Stretch.Uniform,
    };

    [GeneratedRegex("rgba?\\([^)]+\\)|#[0-9a-fA-F]{3,8}", RegexOptions.CultureInvariant)]
    private static partial Regex CssColorExpression();

    [GeneratedRegex("^rgba\\(\\s*(\\d{1,3})\\s*,\\s*(\\d{1,3})\\s*,\\s*(\\d{1,3})\\s*,\\s*([0-9.]+)\\s*\\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CssRgbaExpression();

    [GeneratedRegex("^\\s*(-?[0-9.]+)px\\s+(-?[0-9.]+)px\\s+([0-9.]+)px\\s+(.+)\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CssTextShadowExpression();
}
