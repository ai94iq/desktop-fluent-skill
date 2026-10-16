using Avalonia.Controls;
using Avalonia.Media;
using __Product__.Presentation.Shell;

namespace __Product__.Desktop.Shell;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
}
