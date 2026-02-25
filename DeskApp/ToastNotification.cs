using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DeskApp
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastNotification
    {
        private static Grid? _notificationContainer;
        private static StackPanel? _stackPanel;
        private static readonly object _lock = new object();

        public static void Initialize(Grid container)
        {
            _notificationContainer = container;

            try
            {
                foreach (UIElement child in _notificationContainer.Children)
                {
                    if (child is StackPanel sp && sp.Tag as string == "_toast_stack")
                    {
                        _stackPanel = sp;
                        break;
                    }
                }

                if (_stackPanel == null)
                {
                    _stackPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 8, 8, 0),
                        Tag = "_toast_stack"
                    };
                    _stackPanel.IsHitTestVisible = false;

                    _notificationContainer.Children.Add(_stackPanel);
                }
            }
            catch
            {
            }
        }

        public static void Show(string message, ToastType type = ToastType.Info, int durationSeconds = 3)
        {
            if (_notificationContainer == null || _stackPanel == null)
            {
                throw new InvalidOperationException("ToastNotification no ha sido inicializado. Llama a Initialize() primero.");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = CreateToast(message, type);

                lock (_lock)
                {
                    _stackPanel.Children.Add(toast);
                }

                AnimateIn(toast);

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(durationSeconds)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    AnimateOut(toast, () =>
                    {
                        try
                        {
                            lock (_lock)
                            {
                                if (_stackPanel != null && _stackPanel.Children.Contains(toast))
                                {
                                    _stackPanel.Children.Remove(toast);
                                }
                            }
                        }
                        catch { }
                    });
                };
                timer.Start();
            });
        }

        private static Border CreateToast(string message, ToastType type)
        {
            var (backgroundColor, iconText, foregroundColor) = GetToastStyle(type);

            var iconTextBlock = new TextBlock
            {
                Text = iconText,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(foregroundColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var messageTextBlock = new TextBlock
            {
                Text = message,
                FontSize = 16,
                Foreground = new SolidColorBrush(foregroundColor),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 10, 15, 10)
            };
            stackPanel.Children.Add(iconTextBlock);
            stackPanel.Children.Add(messageTextBlock);

            var border = new Border
            {
                Background = new SolidColorBrush(backgroundColor),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 400,
                Opacity = 0,
                RenderTransform = new TranslateTransform(50, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 5,
                    BlurRadius = 10,
                    Opacity = 0.3,
                    Color = Colors.Black
                },
                Child = stackPanel
            };

            border.IsHitTestVisible = false;

            return border;
        }

        private static (Color background, string icon, Color foreground) GetToastStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => (Color.FromRgb(76, 175, 80), "", Colors.White),
                ToastType.Error => (Color.FromRgb(244, 67, 54), "", Colors.White),
                ToastType.Warning => (Color.FromRgb(255, 152, 0), "", Colors.White),
                ToastType.Info => (Color.FromRgb(33, 150, 243), "", Colors.White),
                _ => (Color.FromRgb(158, 158, 158), "", Colors.White)
            };
        }

        private static void AnimateIn(Border toast)
        {
            try
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var slideIn = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                if (toast.RenderTransform is TranslateTransform tt)
                {
                    tt.BeginAnimation(TranslateTransform.XProperty, slideIn);
                }
            }
            catch { }
        }

        private static void AnimateOut(Border toast, Action onComplete)
        {
            try
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = 50,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s, e) => onComplete?.Invoke();

                toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                if (toast.RenderTransform is TranslateTransform tt)
                {
                    tt.BeginAnimation(TranslateTransform.XProperty, slideOut);
                }
            }
            catch
            {
                try { onComplete?.Invoke(); } catch { }
            }
        }
    }
}
