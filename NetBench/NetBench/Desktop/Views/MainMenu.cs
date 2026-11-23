using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Shell.Presentation.Desktop;
using NetBench.Localization;

namespace NetBench.Desktop.Views;

/// <summary>
/// Меню приложения. На macOS — системная строка меню (<see cref="NativeMenu"/>): меню «NetBench»
/// с «О программе» и стандартными «Скрыть»/«Завершить», затем Файл, Правка, Вид, Окно, Справка.
/// На Windows/Linux строки меню нет, а те же сочетания (Ctrl вместо ⌘) работают через KeyBindings окна.
/// </summary>
internal static class MainMenu
{
    private static KeyModifiers Primary => OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

    /// <summary>Меню «NetBench» (первое на macOS): Avalonia допишет к нему «Службы», «Скрыть» и «Завершить».</summary>
    public static NativeMenu CreateApplicationMenu(Action showAbout)
    {
        var about = new NativeMenuItem(Strings.Instance.Root.Menu.About);
        about.Click += (_, _) => showAbout();
        return [about];
    }

    public static void Attach(MainWindow window, ShellViewModel shell)
    {
        var m = Strings.Instance.Root.Menu;

        var menu = new NativeMenu
        {
            Submenu(m.File,
                Item(m.NewScenario, shell.Sidebar.AddScenarioCommand, Key.N),
                Item(m.EditScenario, shell.EditScenarioCommand, Key.I),
                new NativeMenuItemSeparator(),
                Item(m.Run, shell.RunCommand, Key.R),
                Item(m.Stop, shell.StopCommand, Key.OemPeriod),
                new NativeMenuItemSeparator(),
                Submenu(m.ExportReport,
                    Item(m.ExportJson, shell.ExportJsonCommand, Key.J, KeyModifiers.Shift),
                    Item(m.ExportHtml, shell.ExportHtmlCommand, Key.E),
                    Item(m.ExportCsv, shell.ExportCsvCommand)),
                new NativeMenuItemSeparator(),
                Item(m.CloseWindow, new RelayCommand(window.Close), Key.W)),

            // На macOS пункты меню перехватывают ⌘C/⌘V раньше поля ввода, поэтому правка —
            // явно над полем в фокусе. Без этого меню сочетания работали бы, но меню «Правка»
            // в приложении для Mac ждут.
            Submenu(m.Edit,
                Item(m.Undo, Edit(window, t => t.Undo()), Key.Z),
                Item(m.Redo, Edit(window, t => t.Redo()), Key.Z, KeyModifiers.Shift),
                new NativeMenuItemSeparator(),
                Item(m.Cut, Edit(window, t => t.Cut()), Key.X),
                Item(m.Copy, new RelayCommand(() => Copy(window)), Key.C),
                Item(m.Paste, Edit(window, t => t.Paste()), Key.V),
                Item(m.SelectAll, new RelayCommand(() => SelectAll(window)), Key.A)),

            Submenu(m.View,
                Item(m.ToggleTheme, shell.Sidebar.ToggleThemeCommand, Key.D, KeyModifiers.Shift)),

            Submenu(m.Window,
                Item(m.Minimize, new RelayCommand(() => window.WindowState = WindowState.Minimized), Key.M),
                Item(m.Zoom, new RelayCommand(() => window.WindowState =
                    window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized))),

            Submenu(m.Help,
                Item(m.Website, Link(window, AppInfo.Website)),
                Item(m.ReportIssue, Link(window, AppInfo.Issues)),
                new NativeMenuItemSeparator(),
                Item(m.Privacy, Link(window, AppInfo.PrivacyPolicy)),
                Item(m.Terms, Link(window, AppInfo.Terms)),
                Item(m.SourceCode, Link(window, AppInfo.SourceCode))),
        };

        NativeMenu.SetMenu(window, menu);

        // Строка меню есть только на macOS; на остальных ОС те же сочетания — через окно.
        // Правку (Ctrl+C/V/X/Z/A) поля ввода там обрабатывают сами.
        if (!OperatingSystem.IsMacOS())
        {
            window.KeyBindings.Add(Binding(shell.Sidebar.AddScenarioCommand, Key.N));
            window.KeyBindings.Add(Binding(shell.EditScenarioCommand, Key.I));
            window.KeyBindings.Add(Binding(shell.RunCommand, Key.R));
            window.KeyBindings.Add(Binding(shell.StopCommand, Key.OemPeriod));
            window.KeyBindings.Add(Binding(shell.ExportHtmlCommand, Key.E));
            window.KeyBindings.Add(Binding(shell.ExportJsonCommand, Key.J, KeyModifiers.Shift));
            window.KeyBindings.Add(Binding(shell.Sidebar.ToggleThemeCommand, Key.D, KeyModifiers.Shift));
        }
    }

    private static NativeMenuItem Submenu(string header, params NativeMenuItemBase[] items)
    {
        var menu = new NativeMenu();
        foreach (var item in items)
            menu.Add(item);
        return new NativeMenuItem(header) { Menu = menu };
    }

    private static NativeMenuItem Item(string header, ICommand command, Key? key = null,
        KeyModifiers extra = KeyModifiers.None) => new(header)
    {
        Command = command,
        Gesture = key is { } k ? new KeyGesture(k, Primary | extra) : null,
    };

    private static KeyBinding Binding(ICommand command, Key key, KeyModifiers extra = KeyModifiers.None) => new()
    {
        Command = command,
        Gesture = new KeyGesture(key, Primary | extra),
    };

    private static RelayCommand Link(TopLevel window, Uri uri) =>
        new(() => _ = window.Launcher.LaunchUriAsync(uri));

    private static RelayCommand Edit(TopLevel window, Action<TextBox> action) => new(() =>
    {
        if (window.FocusManager?.GetFocusedElement() is TextBox textBox)
            action(textBox);
    });

    // Копировать можно и из выделяемого текста отчёта (сообщения об ошибках), не только из полей.
    private static void Copy(TopLevel window)
    {
        switch (window.FocusManager?.GetFocusedElement())
        {
            case TextBox textBox:
                textBox.Copy();
                break;
            case SelectableTextBlock block:
                block.Copy();
                break;
        }
    }

    private static void SelectAll(TopLevel window)
    {
        switch (window.FocusManager?.GetFocusedElement())
        {
            case TextBox textBox:
                textBox.SelectAll();
                break;
            case SelectableTextBlock block:
                block.SelectAll();
                break;
        }
    }

    /// <summary>Окно «О программе» поверх главного (одно на раз).</summary>
    public static void ShowAbout(Window? owner)
    {
        var about = new AboutWindow();
        if (owner is { IsVisible: true })
            _ = about.ShowDialog(owner);
        else
            about.Show();
    }
}
