using Material.Styles.Controls;
using Material.Styles.Models;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;

namespace AvaloniaManager.Services
{
    public static class NotificationManagerService
    {
        public static SnackbarHost? SnackbarHost { get; set; }

        public static void ShowSuccess(string message)
        {
            PostSnackbar($"✓ {message}", TimeSpan.FromSeconds(4), "#2E7D32"); // зелёный текст
        }

        public static void ShowError(string message)
        {
            PostSnackbar($"✗ {message}", TimeSpan.FromSeconds(4), "#C62828"); // красный текст
        }

        public static void ShowInfo(string message)
        {
            PostSnackbar($"🛈 {message}", TimeSpan.FromSeconds(4), "#1565C0"); // синий текст
        }

        public static void ShowWarning(string message)
        {
            PostSnackbar($"⚠ {message}", TimeSpan.FromSeconds(4), "#F9A825"); // жёлтый текст
        }

        private static void PostSnackbar(string message, TimeSpan duration, string textColor)
        {
            Debug.WriteLine($"Показываем уведомление: {message}");

            var content = new Border
            {
                Background = Brushes.White, 
                Padding = new Thickness(16, 10), 
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.Parse(textColor)),
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontSize = 14 
                }
            };

            var model = new SnackbarModel(content, duration);

            Dispatcher.UIThread.Post(() =>
            {
                SnackbarHost.Post(model, null, DispatcherPriority.Normal);
            });
        }
    }
}