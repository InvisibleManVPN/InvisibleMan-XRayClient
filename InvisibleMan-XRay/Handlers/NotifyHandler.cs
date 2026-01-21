using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Services;
    using Services.Analytics.Notify;
    using Values;

    public class NotifyHandler : Handler, IDisposable
    {
        private NotifyIcon notifyIcon;
        private Icon baseIcon;

        private Func<Mode> getMode;
        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;
        private Action onProxyModeClick;
        private Action onTunnelModeClick;
        private Action onSwitchConnectionClick;

        private Dictionary<Mode, ToolStripMenuItem> modeItems;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public void Setup(
            Func<Mode> getMode,
            Action onOpenClick,
            Action onUpdateClick,
            Action onAboutClick,
            Action onCloseClick,
            Action onProxyModeClick,
            Action onTunnelModeClick,
            Action onSwitchConnectionClick
        )
        {
            this.getMode = getMode;
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;
            this.onProxyModeClick = onProxyModeClick;
            this.onTunnelModeClick = onTunnelModeClick;
            this.onSwitchConnectionClick = onSwitchConnectionClick;
        }

        public void UpdateConnectionStatus(bool isRunning)
        {
            string switchConnectionLocalizationKey = isRunning ? Localization.NOTIFY_STOP : Localization.NOTIFY_RUN;
            ToolStripItem switchConnectionMenuItem = notifyIcon.ContextMenuStrip.Items.Find(Localization.NOTIFY_RUN, false).First();
            switchConnectionMenuItem.Text = LocalizationService.GetTerm(switchConnectionLocalizationKey);
        }

        public void CheckModeItem(Mode mode)
        {
            ToolStripMenuItem modeItem = modeItems[mode];
            UncheckAllItems();
            CheckItem(modeItem);
        }

        public void InitializeNotifyIcon()
        {
            if (IsNotifyIconAlreadyExists())
                notifyIcon.Dispose();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = GetNotifyIcon();
            baseIcon = notifyIcon.Icon;
            notifyIcon.Visible = true;

            HandleNotifyIconClick();
            AddMenuStrip();

            bool IsNotifyIconAlreadyExists() => notifyIcon != null;

            Icon GetNotifyIcon()
            {
                return Icon.ExtractAssociatedIcon(
                    System.Environment.GetCommandLineArgs().First()
                );
            }
        }

        private void HandleNotifyIconClick()
        {
            notifyIcon.MouseClick += (sender, e) => {
                if (e.Button == MouseButtons.Left)
                    onOpenClick.Invoke();
            };
        }

        private void AddMenuStrip()
        {
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            modeItems = new Dictionary<Mode, ToolStripMenuItem>() {
                { Mode.PROXY, CreateItem(nameof(Mode.PROXY), "Proxy", OnProxyModeClick, true, getMode.Invoke() == Mode.PROXY) },
                { Mode.TUN, CreateItem(nameof(Mode.TUN), "TUN", OnTunnelModeClick, true, getMode.Invoke() == Mode.TUN) }
            };

            AddMenuItem(Localization.NOTIFY_OPEN, LocalizationService.GetTerm(Localization.NOTIFY_OPEN), OnOpenClick);
            AddMenuItem(Localization.NOTIFY_RUN, LocalizationService.GetTerm(Localization.NOTIFY_RUN), OnSwitchConnectionClick);
            AddMenuItem(Localization.NOTIFY_MODE, LocalizationService.GetTerm(Localization.NOTIFY_MODE), delegate { }, modeItems.Values.ToArray());
            AddMenuItem(Localization.NOTIFY_UPDATE, LocalizationService.GetTerm(Localization.NOTIFY_UPDATE), OnUpdateClick);
            AddMenuItem(Localization.NOTIFY_ABOUT, LocalizationService.GetTerm(Localization.NOTIFY_ABOUT), OnAboutClick);
            AddMenuItem(Localization.NOTIFY_CLOSE, LocalizationService.GetTerm(Localization.NOTIFY_CLOSE), OnCloseClick);

            notifyIcon.ContextMenuStrip = contextMenuStrip;

            void AddMenuItem(string key, string text, Action onClick, ToolStripMenuItem[] children = default)
            {
                ToolStripMenuItem item = CreateItem(key, text, onClick);

                if (children != null)
                    foreach(ToolStripMenuItem child in children)
                        item.DropDownItems.Add(child);

                contextMenuStrip.Items.Add(item);
            }

            ToolStripMenuItem CreateItem(
                string key,
                string text,
                Action onClick,
                bool isToggle = default,
                bool isChecked = default
            )
            {
                ToolStripMenuItem item = new ToolStripMenuItem() { Name = key, Text = text, Checked = isChecked };
                item.Click += (sender, e) => {
                    HandleToggleClick();
                    onClick.Invoke();
                };

                return item;

                void HandleToggleClick()
                {
                    if (!isToggle)
                        return;

                    UncheckAllItems();
                    CheckItem(item);
                }
            }

            void OnProxyModeClick()
            {
                AnalyticsService.SendEvent(new ProxyModeClickedEvent());
                onProxyModeClick.Invoke();
            }

            void OnTunnelModeClick()
            {
                AnalyticsService.SendEvent(new TunModeClickedEvent());
                onTunnelModeClick.Invoke();
            }

            void OnOpenClick()
            {
                AnalyticsService.SendEvent(new OpenClickedEvent());
                onOpenClick.Invoke();
            }

            void OnSwitchConnectionClick()
            {
                AnalyticsService.SendEvent(new SwitchConnectionClickedEvent());
                onSwitchConnectionClick.Invoke();
            }

            void OnUpdateClick()
            {
                AnalyticsService.SendEvent(new CheckForUpdateClickedEvent());
                onUpdateClick.Invoke();
            }

            void OnAboutClick()
            {
                AnalyticsService.SendEvent(new AboutClickedEvent());
                onAboutClick.Invoke();
            }

            void OnCloseClick()
            {
                onCloseClick.Invoke();
            }
        }

        private void UncheckAllItems()
        {
            foreach(ToolStripMenuItem itemElement in modeItems.Values)
                itemElement.Checked = false;
        }

        private void CheckItem(ToolStripMenuItem item)
        {
            item.Checked = true;
        }

        public void SetIndicator(Mode? mode)
        {
            if (notifyIcon == null || baseIcon == null)
                return;

            if (mode == null)
            {
                notifyIcon.Icon = baseIcon;
                return;
            }

            Color color = mode == Mode.TUN ? Color.Red : Color.Blue;
            notifyIcon.Icon = CreateIndicatorIcon(color);
        }
        private Icon CreateIndicatorIcon(Color color)
        {
            using (Bitmap bmp = baseIcon.ToBitmap())
            using (Graphics g = Graphics.FromImage(bmp))
            using (Brush brush = new SolidBrush(color))
            using (Pen outline = new Pen(Color.White, 1))
            {
                int size = bmp.Width / 3 + 2;
                int x = bmp.Width - size - 1;
                int y = bmp.Height - size - 1;

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(brush, x, y, size, size);
                g.DrawEllipse(outline, x, y, size, size);

                IntPtr hIcon = bmp.GetHicon();
                Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon);
                return icon;

                [System.Runtime.InteropServices.DllImport("user32.dll")]
                static extern bool DestroyIcon(IntPtr handle);
            }
        }

        public void Dispose()
        {
            notifyIcon?.Dispose();
        }
    }
}