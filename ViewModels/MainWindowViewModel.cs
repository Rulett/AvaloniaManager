using ReactiveUI;
using System.Reactive;

namespace AvaloniaManager.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
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

        private bool _showAuthItem = true;
        public bool ShowAuthItem
        {
            get => _showAuthItem;
            set => this.RaiseAndSetIfChanged(ref _showAuthItem, value);
        }

        private string _currentPageTitle = "Avalonia Manager";
        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => this.RaiseAndSetIfChanged(ref _currentPageTitle, value);
        }

        private int _selectedPageIndex;
        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPageIndex, value);
                UpdatePageTitle();
            }
        }

        private bool _isDrawerOpen = true;
        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set => this.RaiseAndSetIfChanged(ref _isDrawerOpen, value);
        }

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }

        public MainWindowViewModel()
        {
            LoginCommand = ReactiveCommand.Create(() =>
            {
                IsAuthenticated = Password == "admin";
                LoginFailed = !IsAuthenticated;

                if (IsAuthenticated)
                {
                    Password = "";
                    ShowAuthItem = false;
                    SelectedPageIndex = 1; // Автоматически переключаем на "Сотрудники"
                }
            });

            // Исправленная подписка на изменения SelectedPageIndex
            this.WhenAnyValue(x => x.SelectedPageIndex)
                .Subscribe(new AnonymousObserver<int>(_ => UpdatePageTitle()));
        }

        private void UpdatePageTitle()
        {
            CurrentPageTitle = SelectedPageIndex switch
            {
                0 => "Авторизация",
                1 => "Сотрудники",
                2 => "Статьи",
                3 => "Отчеты",
                _ => "Avalonia Manager"
            };
        }
    }
}
