using Avalonia.Threading;
using AvaloniaManager.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AvaloniaManager.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public EmployeesViewModel EmployeesViewModel { get; } = new EmployeesViewModel();
        public ArticlesViewModel ArticlesViewModel { get; } = new ArticlesViewModel();
        //public ReportsViewModel ReportsViewModel { get; } = new ReportsViewModel();

        private bool _isAuthenticated=true;
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
            set => this.RaiseAndSetIfChanged(ref _selectedPageIndex, value);
            
        }

        private bool _isDrawerOpen = true;
        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set => this.RaiseAndSetIfChanged(ref _isDrawerOpen, value);
        }

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<int, Unit> NavigateCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }

        public MainWindowViewModel()
        {
            NavigateCommand = ReactiveCommand.CreateFromTask<int>(async targetIndex =>
            {
                Debug.WriteLine($"Запрошен переход на вкладку {targetIndex}, текущая: {SelectedPageIndex}");

                if (targetIndex == SelectedPageIndex)
                {
                    Debug.WriteLine("Переход на текущую вкладку - пропускаем");
                    return;
                }

                Debug.WriteLine("Проверяем несохраненные изменения...");
                bool canNavigate = await CheckForUnsavedChanges();
                Debug.WriteLine($"Результат проверки: {canNavigate}");

                if (!canNavigate)
                {
                    Debug.WriteLine("Переход отменен из-за несохраненных изменений");
                    return;
                }

                Debug.WriteLine($"Переходим на вкладку {targetIndex}");
                SelectedPageIndex = targetIndex;
                UpdatePageTitle();
            });

            LoginCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsAuthenticated = Password == "admin";
                LoginFailed = !IsAuthenticated;

                if (IsAuthenticated)
                {
                    NotificationManagerService.ShowSuccess("Авторизация прошла успешно!");
                    Password = "";
                    ShowAuthItem = false;
                    await NavigateCommand.Execute(1); 
                }
                else
                {
                    NotificationManagerService.ShowError("Неверный пароль!");
                }
            });

            ShowAboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Delay(100); 
                NotificationManagerService.ShowInfo("Пока тут пусто");
            });

            this.WhenAnyValue(x => x.SelectedPageIndex)
                .Subscribe(new AnonymousObserver<int>(_ => UpdatePageTitle()));
        }

        public async Task<bool> CheckForUnsavedChanges()
        {
            Debug.WriteLine($"CheckForUnsavedChanges() - текущая вкладка: {SelectedPageIndex}");

            switch (SelectedPageIndex)
            {
                case 1: // Employees
                    Debug.WriteLine($"Проверяем изменения в EmployeesViewModel... EmployeesViewModel.HasChanges= {EmployeesViewModel.HasChanges}");
                    if (EmployeesViewModel.HasUnsavedChanges)
                    {
                        Debug.WriteLine("Обнаружены несохраненные изменения, запрашиваем подтверждение...");
                        return await EmployeesViewModel.ConfirmNavigation();
                    }
                    Debug.WriteLine("Нет несохраненных изменений");
                    return true;

                case 2: // Articles
                    Debug.WriteLine($"Проверяем изменения в ArticlesViewModel... ArticlesViewModel.HasChanges= {ArticlesViewModel.HasChanges}");
                    if (ArticlesViewModel.HasUnsavedChanges)
                    {
                        Debug.WriteLine("Обнаружены несохраненные изменения, запрашиваем подтверждение...");
                        return await ArticlesViewModel.ConfirmNavigation();
                    }
                    Debug.WriteLine("Нет несохраненных изменений");
                    return true;

                default:
                    Debug.WriteLine("Для текущей вкладки проверка не требуется");
                    return true;
            }
        }

        public async Task<bool> CanCloseApplication()
        {
            Debug.WriteLine("Проверка изменений перед закрытием приложения");

            if (EmployeesViewModel.HasUnsavedChanges || ArticlesViewModel.HasUnsavedChanges)
            {
                var result = await DialogService.ShowConfirmationDialog(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть приложение?");

                if (result)
                {
                    EmployeesViewModel.Cleanup();
                    ArticlesViewModel.Cleanup();
                }
                return result;
            }

            EmployeesViewModel.Cleanup();
            ArticlesViewModel.Cleanup();
            return true;
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
