using IndustrieDashboard.Shared.Events;
using Xunit;

namespace IndustrieDashboard.Tests.Shared;

public class EventAggregatorTests
{
    private record TestEvent(string Nachricht);

    [Fact]
    public void Publish_RuftAlleAbonnentenAuf()
    {
        var aggregator = new EventAggregator();
        var empfangeneNachrichten = new List<string>();

        aggregator.Subscribe<TestEvent>(e => empfangeneNachrichten.Add(e.Nachricht));
        aggregator.Subscribe<TestEvent>(e => empfangeneNachrichten.Add(e.Nachricht.ToUpperInvariant()));

        aggregator.Publish(new TestEvent("hallo"));

        Assert.Equal(new[] { "hallo", "HALLO" }, empfangeneNachrichten);
    }

    [Fact]
    public void Publish_OhneAbonnenten_WirftKeineException()
    {
        var aggregator = new EventAggregator();

        var exception = Record.Exception(() => aggregator.Publish(new TestEvent("niemand hört zu")));

        Assert.Null(exception);
    }

    [Fact]
    public void Unsubscribe_EntferntHandlerZuverlaessig()
    {
        var aggregator = new EventAggregator();
        var aufrufe = 0;
        void Handler(TestEvent e) => aufrufe++;

        aggregator.Subscribe<TestEvent>(Handler);
        aggregator.Publish(new TestEvent("eins"));
        aggregator.Unsubscribe<TestEvent>(Handler);
        aggregator.Publish(new TestEvent("zwei"));

        Assert.Equal(1, aufrufe);
    }

    [Fact]
    public void Publish_UnterschiedlicheEventTypen_BleibenGetrennt()
    {
        var aggregator = new EventAggregator();
        var testEventAufrufe = 0;
        var anderesEventAufrufe = 0;

        aggregator.Subscribe<TestEvent>(_ => testEventAufrufe++);
        aggregator.Subscribe<KontrolleingriffStatusGeaendertEvent>(_ => anderesEventAufrufe++);

        aggregator.Publish(new TestEvent("x"));

        Assert.Equal(1, testEventAufrufe);
        Assert.Equal(0, anderesEventAufrufe);
    }
}
