using Avalonia.Controls;

namespace NetBench.Desktop.Views;

public partial class ConfirmDialog : Window
{
    // Для XAML-загрузчика и дизайнера
    public ConfirmDialog() => InitializeComponent();

    public ConfirmDialog(string title, string message, string confirm, string cancel) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirm;
        CancelButton.Content = cancel;
        ConfirmButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
    }
}
