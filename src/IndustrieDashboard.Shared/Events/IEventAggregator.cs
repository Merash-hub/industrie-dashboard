namespace IndustrieDashboard.Shared.Events;

/// <summary>
/// Entkoppelter Publish/Subscribe-Mechanismus zwischen Modulen (z. B. damit das
/// Kontrolleingriffe-Modul eine Statusänderung veröffentlicht, auf die das
/// Dashboard reagiert, ohne dass die Module sich direkt kennen).
/// </summary>
public interface IEventAggregator
{
    void Subscribe<TEvent>(Action<TEvent> handler);

    void Unsubscribe<TEvent>(Action<TEvent> handler);

    void Publish<TEvent>(TEvent evt);
}
