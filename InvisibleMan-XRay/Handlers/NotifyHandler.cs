using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace InvisibleManXRay.Handlers
{
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
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = GetNotifyIcon();
            notifyIcon.Visible = true;

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
            
            AddMenuItem("Open Invisible Man XRay", onOpenClick);
            AddMenuItem("Check for updates", onUpdateClick);
            AddMenuItem("About", onAboutClick);
            AddMenuItem("Close", onCloseClick);

            notifyIcon.ContextMenuStrip = contextMenuStrip;

            void AddMenuItem(string text, Action onClick)
            {
                ToolStripMenuItem item = new ToolStripMenuItem() { Text = text };
                item.Click += (sender, e) => { onClick.Invoke(); };
                contextMenuStrip.Items.Add(item);
            }
        }
    }
}