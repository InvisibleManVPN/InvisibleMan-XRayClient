using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace InvisibleManXRay.Handlers
{
    using Models;

    public class NotifyHandler : Handler
    {
        private NotifyIcon notifyIcon;

        private Func<Mode> getMode;
        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;
        private Action onProxyModeClick;
        private Action onTunnelModeClick;

        public NotifyHandler()
        {
            InitializeNotifyIcon();
        }

        public void Setup(
            Func<Mode> getMode,
            Action onOpenClick,
            Action onUpdateClick,
            Action onAboutClick,
            Action onCloseClick,
            Action onProxyModeClick,
            Action onTunnelModeClick
        )
        {
            this.getMode = getMode;
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;
            this.onProxyModeClick = onProxyModeClick;
            this.onTunnelModeClick = onTunnelModeClick;

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
            AddMenuItem("Mode", delegate { }, new ToolStripMenuItem[] {
                CreateItem("Proxy", onProxyModeClick, true, getMode.Invoke() == Mode.PROXY),
                CreateItem("TUN (Experimental)", onTunnelModeClick, true, getMode.Invoke() == Mode.TUN)
            });
            AddMenuItem("Check for updates", onUpdateClick);
            AddMenuItem("About", onAboutClick);
            AddMenuItem("Close", onCloseClick);

            notifyIcon.ContextMenuStrip = contextMenuStrip;

            void AddMenuItem(string text, Action onClick, ToolStripMenuItem[] children = default)
            {
                ToolStripMenuItem item = CreateItem(text, onClick);

                if (children != null)
                    foreach(ToolStripMenuItem child in children)
                        item.DropDownItems.Add(child);
                
                contextMenuStrip.Items.Add(item);
            }

            ToolStripMenuItem CreateItem(
                string text, 
                Action onClick, 
                bool isToggle = default,
                bool isChecked = default
            )
            {
                ToolStripMenuItem item = new ToolStripMenuItem() { Text = text, Checked = isChecked };
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

                void UncheckAllItems()
                {
                    foreach(ToolStripMenuItem item in item.GetCurrentParent().Items)
                        item.Checked = false;
                }

                void CheckItem(ToolStripMenuItem item)
                {
                    item.Checked = true;
                }
            }
        }
    }
}