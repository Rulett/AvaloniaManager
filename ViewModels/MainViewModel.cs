using ReactiveUI;
using System;
using System.Reactive;

namespace AvaloniaManager.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private bool _loginFailed;
        public bool LoginFailed
        {
            get => _loginFailed;
            set => this.RaiseAndSetIfChanged(ref _loginFailed, value);
        }

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }

        public MainViewModel()
        {
            LoginCommand = ReactiveCommand.Create(() =>
            {
                IsAuthenticated = Password == "admin";
                LoginFailed = !IsAuthenticated;

                if (IsAuthenticated)
                {
                    Password = "";
                    LoginFailed = false;
                }
            });
        }
    }
}