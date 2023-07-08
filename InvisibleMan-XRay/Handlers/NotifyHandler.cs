using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Services;
    using Services.Analytics.Notify;

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

        private Dictionary<Mode, ToolStripMenuItem> modeItems;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

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

        public void CheckModeItem(Mode mode)
        {
            ToolStripMenuItem modeItem = modeItems[mode];
            UncheckAllItems();
            CheckItem(modeItem);
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
            modeItems = new Dictionary<Mode, ToolStripMenuItem>() {
                { Mode.PROXY, CreateItem("Proxy", OnProxyModeClick, true, getMode.Invoke() == Mode.PROXY) },
                { Mode.TUN, CreateItem("TUN (Experimental)", OnTunnelModeClick, true, getMode.Invoke() == Mode.TUN) }
            };
            
            AddMenuItem("Open Invisible Man XRay", OnOpenClick);
            AddMenuItem("Mode", delegate { }, modeItems.Values.ToArray());
            AddMenuItem("Check for updates", OnUpdateClick);
            AddMenuItem("About", OnAboutClick);
            AddMenuItem("Close", OnCloseClick);

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
    }
}