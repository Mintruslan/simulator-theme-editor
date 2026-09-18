namespace SITheme;

/// <summary>
/// Exposes the active presentation theme without coupling the editor to a host application.
/// </summary>
public interface IPresentationThemeSettings
{
    SimulatorThemeDocument? PresentationTheme { get; set; }

    SimulatorThemeDocument CreateDefaultPresentationTheme();
}
