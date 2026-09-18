namespace SITheme;

/// <summary>
/// Provides local-first persistence for user-created presentation themes.
/// </summary>
public interface IThemeRepository
{
    IReadOnlyList<SimulatorThemeDocument> GetAll();

    SimulatorThemeDocument? TryGet(string id);

    void Save(SimulatorThemeDocument theme);

    SimulatorThemeDocument Duplicate(SimulatorThemeDocument source, string name);

    bool Delete(string id);

    SimulatorThemeDocument Import(string sourceFilePath);

    SimulatorThemeDocument Import(SimulatorThemeDocument theme);

    void Export(SimulatorThemeDocument theme, string destinationFilePath);
}
