using Avalonia.Controls;
using AvaloniaManager.ViewModels;

namespace AvaloniaManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        // ������������� �� ������� �������� ����
        Closing += MainWindow_Closing;
    }
    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // �������� �������� ��� ��������
            e.Cancel = true;

            // �������� ���������
            bool canClose = await vm.CanCloseApplication();

            if (canClose)
            {
                Closing -= MainWindow_Closing;
                Close();
            }
        }
    }

}