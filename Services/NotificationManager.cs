using Material.Styles.Controls;
using Material.Styles.Models;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia;

namespace AvaloniaManager.Services
{
    public static class NotificationManagerService
    {
        public static SnackbarHost? SnackbarHost { get; set; }

        public static void ShowSuccess(string message)
        {
            PostSnackbar($"✅ {message}", TimeSpan.FromSeconds(4), "#C8E6C9", "#2E7D32"); // зелёный
        }

        public static void ShowError(string message)
        {
            PostSnackbar($"❌ {message}", TimeSpan.FromSeconds(5), "#FFCDD2", "#C62828"); // красный
        }

        public static void ShowInfo(string message)
        {
            PostSnackbar($"ℹ️ {message}", TimeSpan.FromSeconds(4), "#BBDEFB", "#1565C0"); // синий
        }

        public static void ShowWarning(string message)
        {
            PostSnackbar($"⚠️ {message}", TimeSpan.FromSeconds(4), "#FFF9C4", "#F9A825"); // жёлтый
        }


        private static void PostSnackbar(string message, TimeSpan duration, string backgroundColor, string textColor)
        {
            Debug.WriteLine($"Показываем цветное уведомление: {message}");

            var content = new Border
            {
                Background = Avalonia.Media.Brush.Parse(backgroundColor),
                Padding = new Thickness(16, 8),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Avalonia.Media.Brush.Parse(textColor),
                    FontWeight = Avalonia.Media.FontWeight.Bold
                }
            };

            var model = new SnackbarModel(content, duration);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Material.Styles.Controls.SnackbarHost.Post(model, null, DispatcherPriority.Normal);
            });
        }

    }
}
