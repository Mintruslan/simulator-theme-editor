using NUnit.Framework;
using SImulator.ViewModel.Theming;

namespace SImulator.ViewModel.Tests;

public sealed class SimulatorDefaultThemeTests
{
    [Test]
    public void CreatePreservesLegacyPresentationSettings()
    {
        var theme = SimulatorDefaultTheme.Create("#112233", "#445566", "Inter");

        Assert.Multiple(() =>
        {
            Assert.That(theme.SchemaVersion, Is.EqualTo(SimulatorThemeDocument.CurrentSchemaVersion));
            Assert.That(theme.Id, Is.EqualTo("simulator-default"));
            Assert.That(theme.Tokens.Global.TextColor, Is.EqualTo("#112233"));
            Assert.That(theme.Tokens.Global.BackgroundColor, Is.EqualTo("#445566"));
            Assert.That(theme.Tokens.Typography.QuestionText.FontFamily, Is.EqualTo("Inter"));
        });
    }

    [Test]
    public void CreateMapsLegacyDefaultFontToBundledStandardFont()
    {
        var theme = SimulatorDefaultTheme.Create("White", "#0A0E30", "_Default");

        Assert.That(theme.Tokens.Typography.QuestionText.FontFamily, Is.EqualTo("Standard"));
    }
}
