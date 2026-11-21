using NetBench.Desktop.Services;
using NetBench.Features.Settings.Domain;
using NetBench.Features.TestRun.Presentation.Desktop;
using Xunit;

namespace NetBench.Tests;

public sealed class RunConsentTests
{
    [Fact]
    public async Task Asks_before_the_first_run_and_remembers_the_answer()
    {
        var settings = new MemorySettings();
        var dialog = new FakeDialog(answer: true);
        var consent = new RunConsent(settings, dialog);

        Assert.True(await consent.EnsureAsync());
        Assert.True(settings.Current.OwnTargetsConfirmed);

        Assert.True(await consent.EnsureAsync());
        Assert.Equal(1, dialog.Shown);
    }

    [Fact]
    public async Task Cancel_blocks_the_run_and_asks_again_next_time()
    {
        var settings = new MemorySettings();
        var dialog = new FakeDialog(answer: false);
        var consent = new RunConsent(settings, dialog);

        Assert.False(await consent.EnsureAsync());
        Assert.False(settings.Current.OwnTargetsConfirmed);

        Assert.False(await consent.EnsureAsync());
        Assert.Equal(2, dialog.Shown);
    }

    [Fact]
    public async Task Confirmed_settings_skip_the_dialog()
    {
        var settings = new MemorySettings { Current = new AppSettings { OwnTargetsConfirmed = true } };
        var dialog = new FakeDialog(answer: false);

        Assert.True(await new RunConsent(settings, dialog).EnsureAsync());
        Assert.Equal(0, dialog.Shown);
    }

    private sealed class MemorySettings : IAppSettingsStore
    {
        public AppSettings Current { get; set; } = new();

        public void Save(AppSettings settings) => Current = settings;
    }

    private sealed class FakeDialog(bool answer) : IConfirmationDialog
    {
        public int Shown { get; private set; }

        public Task<bool> ConfirmAsync(string title, string message, string confirm, string cancel)
        {
            Shown++;
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(message));
            return Task.FromResult(answer);
        }
    }
}
