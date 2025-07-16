using ReactiveUI;
using System.Diagnostics;

namespace AvaloniaManager.ViewModels
{
    public class ReportsViewModel : ReactiveObject
    {
        private string _message = "ну пусто тут да";

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public ReportsViewModel()
        {

        }
    }
}