namespace IndustrieDashboard.Tests;

/// <summary>
/// Test, der nur in Debug-Builds läuft. Grund: <c>PrototypBenutzerKontext</c>
/// verweigert sich in Release-Builds absichtlich (Spezifikation A8); Tests, die
/// ihn erzeugen, können dort nicht laufen. In Release wird der Test mit dieser
/// Begründung übersprungen, nicht verschwiegen. Dass die Verweigerung in
/// Release wirklich greift, prüft <see cref="NurImReleaseBuildFactAttribute"/>.
/// </summary>
public sealed class NurImDebugBuildFactAttribute : FactAttribute
{
    public NurImDebugBuildFactAttribute()
    {
#if !DEBUG
        Skip = Begruendungen.NurDebug;
#endif
    }
}

/// <inheritdoc cref="NurImDebugBuildFactAttribute"/>
public sealed class NurImDebugBuildTheoryAttribute : TheoryAttribute
{
    public NurImDebugBuildTheoryAttribute()
    {
#if !DEBUG
        Skip = Begruendungen.NurDebug;
#endif
    }
}

/// <summary>
/// Test, der nur in Release-Builds läuft, etwa der Nachweis, dass der
/// Prototyp-Anbieter dort nicht erzeugt werden kann.
/// </summary>
public sealed class NurImReleaseBuildFactAttribute : FactAttribute
{
    public NurImReleaseBuildFactAttribute()
    {
#if DEBUG
        Skip = "Prüft Verhalten, das nur in Release-Builds gilt.";
#endif
    }
}

internal static class Begruendungen
{
    public const string NurDebug =
        "Braucht PrototypBenutzerKontext, der sich in Release-Builds absichtlich verweigert (Spezifikation A8).";
}
