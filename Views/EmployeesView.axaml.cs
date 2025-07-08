using Avalonia;
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
            
        }

        public static readonly StyledProperty<EmployeesViewModel> ViewModelProperty =
        AvaloniaProperty.Register<EmployeesView, EmployeesViewModel>(nameof(ViewModel));

        public EmployeesViewModel ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

    }
}