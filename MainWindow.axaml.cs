using Avalonia.Controls;
using AvaloniaManager.ViewModels;

namespace AvaloniaManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}