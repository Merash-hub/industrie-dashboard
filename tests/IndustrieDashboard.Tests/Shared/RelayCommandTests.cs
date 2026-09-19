using IndustrieDashboard.Shared.Mvvm;
using Xunit;

namespace IndustrieDashboard.Tests.Shared;

public class RelayCommandTests
{
    [Fact]
    public void Execute_RuftUebergebeneAktionAuf()
    {
        var ausgefuehrt = false;
        var command = new RelayCommand(() => ausgefuehrt = true);

        command.Execute(null);

        Assert.True(ausgefuehrt);
    }

    [Fact]
    public void CanExecute_OhneBedingung_IstImmerWahr()
    {
        var command = new RelayCommand(() => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_MitBedingung_SpiegeltBedingungWider()
    {
        var erlaubt = false;
        var command = new RelayCommand(() => { }, () => erlaubt);

        Assert.False(command.CanExecute(null));

        erlaubt = true;

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_LoestEventAus()
    {
        var command = new RelayCommand(() => { });
        var eventAusgeloest = false;
        command.CanExecuteChanged += (_, _) => eventAusgeloest = true;

        command.RaiseCanExecuteChanged();

        Assert.True(eventAusgeloest);
    }

    [Fact]
    public async Task AsyncRelayCommand_VerhindertParalleleAusfuehrung()
    {
        var laufendeAusfuehrungen = 0;
        var maxGleichzeitig = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            laufendeAusfuehrungen++;
            maxGleichzeitig = Math.Max(maxGleichzeitig, laufendeAusfuehrungen);
            await Task.Delay(50);
            laufendeAusfuehrungen--;
        });

        command.Execute(null);
        // Zweiter Aufruf während der erste noch läuft: CanExecute muss false liefern.
        var kannWaehrendLaufAusgefuehrtWerden = command.CanExecute(null);

        await Task.Delay(100);

        Assert.False(kannWaehrendLaufAusgefuehrtWerden);
        Assert.Equal(1, maxGleichzeitig);
        Assert.True(command.CanExecute(null));
    }
}
