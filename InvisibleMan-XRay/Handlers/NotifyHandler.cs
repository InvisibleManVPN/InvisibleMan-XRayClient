using System;
using System.Linq;
using System.Drawing;
using System.Windows;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class NotifyHandler : Handler
    {
        private NotifyIcon notifyIcon;

        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;

        public NotifyHandler()
        {
            InitializeNotifyIcon();
        }

        public void Setup(
            Action onOpenClick,
            Action onUpdateClick,
            Action onAboutClick,
            Action onCloseClick
        )
        {
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;

            HandleNotifyIconClick();
            AddMenuStrip();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = NotifyIcon.Create();
            notifyIcon.Icon = GetNotifyIcon();

            Icon GetNotifyIcon()
            {
                return Icon.ExtractAssociatedIcon(
                    System.Environment.GetCommandLineArgs().First()
                );
            }
        }

        private void HandleNotifyIconClick()
        {
            notifyIcon.Click += (sender, e) => {
                onOpenClick.Invoke();
            };
        }

        private void AddMenuStrip()
        {
            AddMenuItem("Open Invisible Man XRay", onOpenClick);
            AddMenuItem("Check for updates", onUpdateClick);
            AddMenuItem("About", onAboutClick);
            AddMenuItem("Close", onCloseClick);

            void AddMenuItem(string text, Action onClick)
            {
                ContextMenuStrip.MenuItem item = new ContextMenuStrip.MenuItem() { Text = text };
                item.Click += (sender, e) => { onClick.Invoke(); };
                notifyIcon.ContextMenuStrip.Items.Add(item);
            }
        }
    }
}