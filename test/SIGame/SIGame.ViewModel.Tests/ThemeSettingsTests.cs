using SITheme;
using SIGame.ViewModel;
using System.Xml.Serialization;

namespace SIGame.ViewModel.Tests;

public sealed class ThemeSettingsTests
{
    [Test]
    public void DefaultPresentationThemeUsesSIGameBrandAndLegacyColors()
    {
        var settings = new ThemeSettings();
        settings.UISettings.TableColorString = "#FF112233";
        settings.UISettings.TableBackColorString = "#FF445566";

        var theme = settings.CreateDefaultPresentationTheme();

        Assert.Multiple(() =>
        {
            Assert.That(theme.Id, Is.EqualTo(ThemeSettings.DefaultPresentationThemeId));
            Assert.That(theme.Name, Is.EqualTo("Default SIGame Theme"));
            Assert.That(theme.Tokens.Global.TextColor, Is.EqualTo("#112233FF"));
            Assert.That(theme.Tokens.Global.BackgroundColor, Is.EqualTo("#445566FF"));
        });
    }

    [Test]
    public void PresentationThemeUpdatesPersistedThemeIdentifier()
    {
        var settings = new ThemeSettings
        {
            PresentationTheme = new SimulatorThemeDocument { Id = "studio-theme", Name = "Studio" },
        };

        Assert.That(settings.PresentationThemeId, Is.EqualTo("studio-theme"));

        settings.PresentationTheme = null;

        Assert.That(settings.PresentationThemeId, Is.EqualTo(ThemeSettings.DefaultPresentationThemeId));
    }

    [Test]
    public void PresentationThemeIdentifierRoundTripsThroughXml()
    {
        var serializer = new XmlSerializer(typeof(ThemeSettings));
        var source = new ThemeSettings { PresentationThemeId = "broadcast-theme" };

        using var writer = new StringWriter();
        serializer.Serialize(writer, source);
        using var reader = new StringReader(writer.ToString());
        var restored = (ThemeSettings)serializer.Deserialize(reader)!;

        Assert.That(restored.PresentationThemeId, Is.EqualTo("broadcast-theme"));
        Assert.That(restored.PresentationTheme, Is.Null);
    }
}
