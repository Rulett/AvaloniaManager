using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons.Avalonia;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using DialogHostAvalonia;

namespace AvaloniaManager.Services
{
    public class DialogService
    {
        public static async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            var content = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(16),
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children =
                        {
                            new MaterialIcon
                            {
                                Kind = Material.Icons.MaterialIconKind.InfoCircleOutline,
                                Width = 32,
                                Height = 32,
                                Foreground = Brushes.DodgerBlue
                            },
                            new TextBlock
                            {
                                Text = title,
                                FontWeight = FontWeight.Bold,
                                FontSize = 18,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Margin = new Thickness(44, 0, 0, 0)
                    }
                }
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(0, 16, 0, 0),
                Children =
                {
                    new Button
                    {
                        Content = "Да",
                        Classes = { "Primary" },
                        Width = 100,
                        Command = new RelayCommand(() => DialogHost.Close("MainDialogHost", true)),
                    },
                    new Button
                    {
                        Content = "Нет",
                        Classes = { "Outlined" },
                        Width = 100,
                        Command = new RelayCommand(() => DialogHost.Close("MainDialogHost", false)),
                    }
                }
            };

            var mainContent = new StackPanel
            {
                Spacing = 0,
                Children = { content, buttons }
            };

            var result = await DialogHost.Show(mainContent, "MainDialogHost");
            return result is bool b && b;
        }

        public static async Task ShowErrorNotification(string message)
        {
            NotificationManagerService.ShowError(message);
            await Task.CompletedTask;
        }

        public static async Task ShowSuccessNotification(string message)
        {
            NotificationManagerService.ShowSuccess(message);
            await Task.CompletedTask;
        }
    }
}