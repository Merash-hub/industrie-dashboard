using IndustrieDashboard.Core.Enums;

namespace IndustrieDashboard.Shared.Events;

/// <summary>
/// Cross-Modul-Nachrichten für den <see cref="IEventAggregator"/>. Neue
/// Nachrichtentypen werden hier ergänzt, wenn Module lose gekoppelt
/// miteinander kommunizieren sollen (z. B. Kontrolleingriffe -> Dashboard).
/// </summary>
public record KontrolleingriffStatusGeaendertEvent(int AnforderungId, int MaschineId, KontrolleingriffStatus NeuerStatus);

public record NavigationsAnfrageEvent(string ZielModul);
