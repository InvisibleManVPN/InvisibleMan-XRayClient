using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Models;
using InvisibleGorillaXRay.Services;

namespace InvisibleGorillaXRay.Mac.Handlers
{
    public class MacNotifyHandler : Handler
    {
        private Func<Mode> getMode;
        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;
        private Action onProxyModeClick;
        private Action onTunnelModeClick;

        private NativeMenuItem? proxyItem;
        private NativeMenuItem? tunItem;

        public void Setup(
            Func<Mode> getMode, Action onOpenClick, Action onUpdateClick,
            Action onAboutClick, Action onCloseClick,
            Action onProxyModeClick, Action onTunnelModeClick)
        {
            this.getMode = getMode;
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;
            this.onProxyModeClick = onProxyModeClick;
            this.onTunnelModeClick = onTunnelModeClick;

            InitializeNotifyIcon();
        }

        public void InitializeNotifyIcon()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var trayIcons = TrayIcon.GetIcons(Application.Current);
                    if (trayIcons != null)
                        trayIcons.Clear();
                    else
                        trayIcons = new TrayIcons();

                    var menu = new NativeMenu();

                    var openItem = new NativeMenuItem("Open");
                    openItem.Click += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onOpenClick?.Invoke());
                    menu.Add(openItem);
                    menu.Add(new NativeMenuItemSeparator());

                    proxyItem = new NativeMenuItem("Proxy");
                    proxyItem.Click += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onProxyModeClick?.Invoke());
                    menu.Add(proxyItem);

                    tunItem = new NativeMenuItem("TUN");
                    tunItem.Click += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onTunnelModeClick?.Invoke());
                    menu.Add(tunItem);

                    menu.Add(new NativeMenuItemSeparator());

                    var aboutItem = new NativeMenuItem("About");
                    aboutItem.Click += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onAboutClick?.Invoke());
                    menu.Add(aboutItem);

                    var quitItem = new NativeMenuItem("Quit");
                    quitItem.Click += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onCloseClick?.Invoke());
                    menu.Add(quitItem);

                    var trayIcon = new TrayIcon
                    {
                        ToolTipText = "Invisible Gorilla XRay",
                        Icon = CreateMenuBarIcon(),
                        Menu = menu
                    };

                    trayIcon.Clicked += (s, e) => Dispatcher.UIThread.InvokeAsync(() => onOpenClick?.Invoke());

                    trayIcons.Add(trayIcon);
                    TrayIcon.SetIcons(Application.Current, trayIcons);

                    CheckModeItem();
                }
                catch { }
            });
        }

        private WindowIcon CreateMenuBarIcon()
        {
            try
            {
                int size = 44;
                var visual = new Avalonia.Controls.Shapes.Rectangle { Width = size, Height = size };
                if (Application.Current?.TryFindResource("Icon.InvisibleGorilla", out var res) == true && res is IBrush brush)
                    visual.Fill = brush;
                else
                    visual.Fill = new SolidColorBrush(Color.Parse("#4CAF50"));

                visual.Measure(new Size(size, size));
                visual.Arrange(new Rect(0, 0, size, size));

                var rtb = new RenderTargetBitmap(new PixelSize(size, size));
                rtb.Render(visual);

                using var stream = new MemoryStream();
                rtb.Save(stream);
                stream.Position = 0;
                return new WindowIcon(stream);
            }
            catch { return null; }
        }

        public void CheckModeItem()
        {
            try
            {
                Mode mode = getMode?.Invoke() ?? Mode.PROXY;
                if (proxyItem != null)
                    proxyItem.IsEnabled = mode != Mode.PROXY;
                if (tunItem != null)
                    tunItem.IsEnabled = mode != Mode.TUN;
            }
            catch { }
        }
    }
}
