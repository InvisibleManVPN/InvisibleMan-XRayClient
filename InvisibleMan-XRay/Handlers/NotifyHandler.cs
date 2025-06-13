using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Services;
    using Services.Analytics.Notify;
    using Values;

    public class NotifyHandler : Handler
    {
        private NotifyIcon? notifyIcon;

        private Func<Mode> getMode;
        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;
        private Action onProxyModeClick;
        private Action onTunnelModeClick;

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
            CoreEnabledOrDisabledModeObserver coreEnabledOrDisabledModeObserver
        )
        {
            this.getMode = getMode;
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;
            this.onProxyModeClick = onProxyModeClick;
            this.onTunnelModeClick = onTunnelModeClick;
            coreEnabledOrDisabledModeObserver.Subscribe(HandleProxyEnabledOrDisabled);
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
            notifyIcon.Icon = GetNotifyIconFromResources(forEnabled: false);
            notifyIcon.Visible = true;

            HandleNotifyIconClick();
            AddMenuStrip();

            bool IsNotifyIconAlreadyExists() => notifyIcon != null;
        }

        public void HandleProxyEnabledOrDisabled(ProxyEnabledOrDisabledState state)
        {
            if (notifyIcon is null)
                return;
            
            var icon = GetNotifyIconFromResources(forEnabled: state == ProxyEnabledOrDisabledState.ENABLED);
            notifyIcon.Icon = icon;
        }
        
        private Icon GetNotifyIconFromResources(bool forEnabled)
        {
            const string rootNamespace = "InvisibleManXRay";
            const string basePath = $"Assets.NotifyIcons";
            var fileName = forEnabled ? "IconEnabled.ico" : "IconDisabled.ico";
            var resourceName = $"{rootNamespace}.{basePath}.{fileName}";
            
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("Something went wrong with resources or with assembly or with root namespace");
            var icon = new Icon(stream);
            return icon;
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
                { Mode.TUN, CreateItem("TUN", OnTunnelModeClick, true, getMode.Invoke() == Mode.TUN) }
            };
            
            AddMenuItem(LocalizationService.GetTerm(Localization.NOTIFY_OPEN), OnOpenClick);
            AddMenuItem(LocalizationService.GetTerm(Localization.NOTIFY_MODE), delegate { }, modeItems.Values.ToArray());
            AddMenuItem(LocalizationService.GetTerm(Localization.NOTIFY_UPDATE), OnUpdateClick);
            AddMenuItem(LocalizationService.GetTerm(Localization.NOTIFY_ABOUT), OnAboutClick);
            AddMenuItem(LocalizationService.GetTerm(Localization.NOTIFY_CLOSE), OnCloseClick);

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