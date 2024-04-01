using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InvisibleManXRay.Windows
{
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
        }

        private void OnConfigurationTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableConfigurationTab(false);
            SetActiveConfigurationPanel(true);
        }

        private void OnSubscriptionTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableSubscriptionTab(false);
            SetActiveSubscriptionPanel(true);
        }

        private void OnAddConfigurationButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveConfigListsPanel(false);
            SetActiveAddConfigurationPanel(true);
        }

        private void OnAddSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveConfigListsPanel(false);
            SetActiveAddSubscriptionPanel(true);
        }

        private void OnAddConfigurationImportButtonClick(object sender, RoutedEventArgs e)
        {
            GoToConfigurationPanel();
        }

        private void OnAddConfigurationCancelButtonClick(object sender, RoutedEventArgs e)
        {
            GoToConfigurationPanel();
        }

        private void OnAddSubscriptionImportButtonClick(object sender, RoutedEventArgs e)
        {
            GoToSubscriptionPanel();
        }

        private void OnAddSubscriptionCancelButtonClick(object sender, RoutedEventArgs e)
        {
            GoToSubscriptionPanel();
        }

        private void GoToConfigurationPanel()
        {
            SetActiveAddConfigurationPanel(false);
            SetActiveConfigListsPanel(true);
        }

        private void GoToSubscriptionPanel()
        {
            SetActiveAddSubscriptionPanel(false);
            SetActiveConfigListsPanel(true);
        }

        private void EnableAllTabs()
        {
            SetEnableConfigurationTab(true);
            SetEnableSubscriptionTab(true);
        }

        private void HideAllPanels()
        {
            SetActiveConfigurationPanel(false);
            SetActiveSubscriptionPanel(false);
        }

        private void SetEnableConfigurationTab(bool isEnabled) => SetEnableButton(buttonConfigurationTab, isEnabled);

        private void SetEnableSubscriptionTab(bool isEnabled) => SetEnableButton(buttonSubscriptionTab, isEnabled);

        private void SetEnableButton(Button button, bool isEnabled) => button.IsEnabled = isEnabled;

        private void SetActiveConfigListsPanel(bool isActive) => SetActivePanel(panelConfigLists, isActive);

        private void SetActiveConfigurationPanel(bool isActive) => SetActivePanel(panelConfiguration, isActive);

        private void SetActiveSubscriptionPanel(bool isActive) => SetActivePanel(panelSubscription, isActive);

        private void SetActiveAddConfigurationPanel(bool isActive) => SetActivePanel(panelAddConfiguration, isActive);

        private void SetActiveAddSubscriptionPanel(bool isActive) => SetActivePanel(panelAddSubscription, isActive);

        private void SetActivePanel(Panel panel, bool isActive) => panel.IsVisible = isActive;
    }
}