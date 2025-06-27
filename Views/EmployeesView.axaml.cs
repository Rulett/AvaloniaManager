using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaManager.ViewModels;

namespace AvaloniaManager.Views
{
    public partial class EmployeesView : UserControl
    {
        public EmployeesView()
        {
            InitializeComponent();
            DataContext = new EmployeesViewModel();
        }

    }
}