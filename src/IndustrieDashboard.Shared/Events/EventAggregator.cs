using System.Collections.Concurrent;

namespace IndustrieDashboard.Shared.Events;

/// <summary>
/// Thread-sichere Standardimplementierung von <see cref="IEventAggregator"/>.
/// Als Singleton in der DI registriert.
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlerListen = new();
    private readonly object _sperre = new();

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var liste = _handlerListen.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (_sperre)
        {
            liste.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (_handlerListen.TryGetValue(typeof(TEvent), out var liste))
        {
            lock (_sperre)
            {
                liste.Remove(handler);
            }
        }
    }

    public void Publish<TEvent>(TEvent evt)
    {
        if (!_handlerListen.TryGetValue(typeof(TEvent), out var liste))
        {
            return;
        }

        Delegate[] snapshot;
        lock (_sperre)
        {
            snapshot = liste.ToArray();
        }

        foreach (var handler in snapshot)
        {
            ((Action<TEvent>)handler).Invoke(evt);
        }
    }

    /// <summary>
    /// Nur für Diagnose und Tests: Anzahl aktuell registrierter Abonnenten für
    /// TEvent. Damit lässt sich nachweisen, dass ViewModels sich beim
    /// Verwerfen (Dispose) korrekt wieder abmelden, statt sich bei jedem
    /// Moduswechsel anzusammeln.
    /// </summary>
    public int AnzahlAbonnenten<TEvent>()
    {
        if (!_handlerListen.TryGetValue(typeof(TEvent), out var liste))
        {
            return 0;
        }

        lock (_sperre)
        {
            return liste.Count;
        }
    }
}
