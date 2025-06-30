using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using AvaloniaManager.Services;
using AvaloniaManager.ViewModels;
using Material.Styles.Controls;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace AvaloniaManager
{
    public partial class MainView : ReactiveUserControl<MainWindowViewModel>
    {
        private SnackbarHost? _snackbarHost;
        public MainView()
        {
            InitializeComponent();

            this.AttachedToVisualTree += (s, e) => {
                _snackbarHost = this.FindControl<SnackbarHost>("Root");
                NotificationManagerService.SnackbarHost = _snackbarHost;
            };

            if (_snackbarHost == null)
            {
                Console.WriteLine("SnackbarHost не найден!");
            }

            NavDrawerSwitch.Click += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.IsDrawerOpen = !ViewModel.IsDrawerOpen;
                }
            };

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel!.IsAuthenticated)
                    .Subscribe(authenticated =>
                    {
                        if (authenticated)
                        {
                            PageCarousel.SelectedIndex = 1;
                            // Управление состоянием drawer через привязку
                            NavDrawerSwitch.IsChecked = true;
                        }
                        else
                        {
                            PageCarousel.SelectedIndex = 0;
                            NavDrawerSwitch.IsChecked = false;
                        }
                    })
                    .DisposeWith(disposables);

                // Обработка выбора в меню
                DrawerList.SelectionChanged += OnDrawerSelectionChanged;
                Disposable.Create(() => DrawerList.SelectionChanged -= OnDrawerSelectionChanged)
                    .DisposeWith(disposables);
            });
        }

        private void OnDrawerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrawerList.SelectedIndex < 0 || ViewModel == null) return;

            var viewModel = (MainWindowViewModel)DataContext!;

            // Если пытаемся выбрать авторизацию, когда она скрыта
            if (DrawerList.SelectedIndex == 0 && !viewModel.ShowAuthItem)
            {
                DrawerList.SelectedIndex = 1; // Переключаем на первую рабочую страницу
            }

            // Обновляем SelectedPageIndex в ViewModel
            viewModel.SelectedPageIndex = DrawerList.SelectedIndex;

            // Синхронизируем Carousel
            PageCarousel.SelectedIndex = viewModel.SelectedPageIndex;
            mainScroller.Offset = Vector.Zero;
        }

        private void MaterialIcon_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var materialTheme = Application.Current!.LocateMaterialTheme<MaterialTheme>();
            materialTheme.BaseTheme =
                materialTheme.BaseTheme == BaseThemeMode.Light ? BaseThemeMode.Dark : BaseThemeMode.Light;
        }

        private void UserControl_Loaded(object? sender, RoutedEventArgs e)
        {
            NotificationManagerService.SnackbarHost = this.FindControl<SnackbarHost>("Root");
            NotificationManagerService.ShowSuccess("Добро пожаловать!");
        }

    }
}