using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Core.Models;
using NetBench.Services;

namespace NetBench.Features.Report;

public partial class ReportViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public TestRunReport Report { get; }

    public ReportViewModel(TestRunReport report, INavigationService navigation)
    {
        Report = report;
        _navigation = navigation;
    }

    [RelayCommand]
    private void Close() => _navigation.NavigateTo(null);
}
