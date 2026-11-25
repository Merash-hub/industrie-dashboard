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
        // Kein Timing: Die Aktion läuft, bis der Test sie über "freigabe" beendet.
        // Früher wartete der Test fest 100 ms auf eine 50-ms-Aktion; unter Last kam die
        // Fortsetzung zu spät und CanExecute war noch false (wackeliger Test).
        var freigabe = new TaskCompletionSource();
        var aufrufe = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            aufrufe++;
            await freigabe.Task;
        });
        var beendet = new TaskCompletionSource();
        command.CanExecuteChanged += (_, _) =>
        {
            if (command.CanExecute(null))
                beendet.TrySetResult();
        };

        command.Execute(null);

        // Während die erste Ausführung läuft: CanExecute false, zweiter Aufruf wirkungslos.
        Assert.False(command.CanExecute(null));
        command.Execute(null);
        Assert.Equal(1, aufrufe);

        freigabe.SetResult();
        await beendet.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(command.CanExecute(null));
        Assert.Equal(1, aufrufe);
    }

    [Fact]
    public void AsyncRelayCommand_FuehrtNichtAus_WennBedingungFalsch()
    {
        var aufrufe = 0;
        var command = new AsyncRelayCommand(_ => { aufrufe++; return Task.CompletedTask; }, _ => false);

        command.Execute(null);

        Assert.Equal(0, aufrufe);
    }
}
