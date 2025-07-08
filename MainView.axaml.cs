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
using System.Reactive.Linq;

namespace AvaloniaManager
{
    public partial class MainView : ReactiveUserControl<MainWindowViewModel>
    {
        private SnackbarHost? _snackbarHost;
        public MainView()
        {
            InitializeComponent();

            
                _snackbarHost = this.FindControl<SnackbarHost>("Root");
                NotificationManagerService.SnackbarHost = _snackbarHost;
            

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
                            NavDrawerSwitch.IsChecked = true;
                        }
                        else
                        {
                            PageCarousel.SelectedIndex = 0;
                            NavDrawerSwitch.IsChecked = false;
                        }
                    })
                    .DisposeWith(disposables);

                // Синхронизация Carousel с SelectedPageIndex
                this.WhenAnyValue(x => x.ViewModel!.SelectedPageIndex)
                    .Subscribe(newIndex =>
                    {
                        PageCarousel.SelectedIndex = newIndex;
                        mainScroller.Offset = Vector.Zero;
                    })
                    .DisposeWith(disposables);

                // Обработка выбора в меню
                DrawerList.SelectionChanged += OnDrawerSelectionChanged;
                Disposable.Create(() => DrawerList.SelectionChanged -= OnDrawerSelectionChanged)
                    .DisposeWith(disposables);
            });
        }
        private bool _isHandlingSelection = false;

        private async void OnDrawerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isHandlingSelection || DrawerList.SelectedIndex < 0 || ViewModel == null)
                return;

            try
            {
                _isHandlingSelection = true;

                Console.WriteLine($"Выбрана вкладка: {DrawerList.SelectedIndex}");
                await ViewModel.NavigateCommand.Execute(DrawerList.SelectedIndex);
            }
            finally
            {
                _isHandlingSelection = false;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.WhenAnyValue(x => x.ViewModel!.SelectedPageIndex)
                .Subscribe(newIndex =>
                {
                    if (_isHandlingSelection) return;

                    Console.WriteLine($"Синхронизация выделения: {newIndex}");

                    _isHandlingSelection = true;
                    try
                    {
                        PageCarousel.SelectedIndex = newIndex;
                        mainScroller.Offset = Vector.Zero;
                        DrawerList.SelectedIndex = newIndex;
                    }
                    finally
                    {
                        _isHandlingSelection = false;
                    }
                });
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

        

        public void ResetDrawerSelection()
        {
            DrawerList.SelectedIndex = -1;
            Dispatcher.UIThread.Post(() =>
            {
                DrawerList.SelectedIndex = ViewModel?.SelectedPageIndex ?? 0;
            }, DispatcherPriority.Background);
        }

    }
}