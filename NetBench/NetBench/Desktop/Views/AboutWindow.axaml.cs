using Avalonia.Controls;
using Avalonia.Input;
using NetBench.Localization;

namespace NetBench.Desktop.Views;

/// <summary>«О программе NetBench» — из меню приложения (на macOS — первый пункт меню NetBench).</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = Strings.Instance.Root.About.Version(AppInfo.Version, AppInfo.BuildNumber);
        PrivacyButton.Click += (_, _) => Open(AppInfo.PrivacyPolicy);
        TermsButton.Click += (_, _) => Open(AppInfo.Terms);
        FeedbackButton.Click += (_, _) => Open(AppInfo.Issues);
        SourceButton.Click += (_, _) => Open(AppInfo.SourceCode);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
            Close();
    }

    private void Open(Uri uri) => _ = Launcher.LaunchUriAsync(uri);
}
