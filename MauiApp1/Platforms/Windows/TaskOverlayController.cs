#if WINDOWS
using Anti_Bunda_Mole.Models;
using Anti_Bunda_Mole.Methods;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using ScrollView = Microsoft.Maui.Controls.ScrollView;
using Thickness = Microsoft.UI.Xaml.Thickness;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace Anti_Bunda_Mole.Platforms.Windows
{
    public static class TaskOverlayController
    {
        private static bool _subscribed = false;
        private static Func<ScrollView>? _lastBuilder;

        public static void ShowTasks(Func<ScrollView> buildCards)
        {
            _lastBuilder = buildCards;

            ConfigManager.Instance.LoadIfNeeded();
            var config = ConfigManager.Instance.Config;

            if (!_subscribed)
            {
                ConfigManager.Instance.ConfigChanged += OnConfigChanged;
                _subscribed = true;
            }

            BuildAndShowOverlay(config, buildCards);
        }

        private static void BuildAndShowOverlay(Configuracoes config, Func<ScrollView> buildCards)
        {
            OverlaySide side = config.PosicaoTarefas?.Contains("_l") == true
                ? OverlaySide.Left
                : OverlaySide.Right;

            bool bottomToTop = config.PosicaoTarefas?.Contains("lower") == true;

            var mauiPanel = buildCards();

            // StackPanel WinUI para receber os filhos MAUI convertidos
            var xamlPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var children = mauiPanel.Children.ToList();
            if (bottomToTop)
                children.Reverse();

            var mauiContext = MauiWinUIApplication.Current.Application.Windows
                .FirstOrDefault()?.Handler?.MauiContext;

            if (mauiContext == null)
                return;

            // Converte filhos MAUI -> WinUI
            foreach (var mauiChild in children)
            {
                var native = mauiChild.ToPlatform(mauiContext);
                if (native is FrameworkElement element)
                    xamlPanel.Children.Add(element);
            }

            FrameworkElement containerChild;

            // Se bottomToTop, usa grid para posicionamento
            if (bottomToTop)
            {
                var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
                xamlPanel.VerticalAlignment = VerticalAlignment.Bottom;
                grid.Children.Add(xamlPanel);
                containerChild = grid;
            }
            else
            {
                containerChild = xamlPanel;
            }

            // Aqui a diferença: adiciona ScrollViewer do WinUI
            var scrollViewer = new Microsoft.UI.Xaml.Controls.ScrollViewer
            {
                Content = containerChild,
                VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled
            };

            // Container final com padding
            var container = new Border
            {
                Padding = new Thickness(16),
                Child = scrollViewer
            };

            OverlayManager.ShowOverlay(container, side, width: 400);
        }

        private static void OnConfigChanged()
        {
            Close();

            if (_lastBuilder != null)
            {
                var config = ConfigManager.Instance.Config;
                BuildAndShowOverlay(config, _lastBuilder);
            }
        }

        public static void Close()
        {
            OverlayManager.CloseOverlay();
        }
    }
}
#endif
