using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

namespace InvisibleManXRay
{
    using Models;
    using Values;
    using Utilities;
    using Services.Analytics.ServerWindow;

    public partial class ServerWindow : Window
    {
        private enum SubscriptionOperation { CREATE, EDIT }

        private string groupPath = null;
        private SubscriptionOperation subscriptionOperation;
        private List<Components.Config> subscriptionConfigComponents;

        private Func<string, List<Config>> getAllSubscriptionConfigs;
        private Func<List<Subscription>> getAllGroups;
        private Func<string, string, Status> convertLinkToSubscription;
        private Action<string, string, string> onCreateSubscription;
        private Action<Subscription> onDeleteSubscription;

        private void InitializeImportingGroups()
        {
            SetActiveFileImportingGroup(true);
            SetActiveLinkImportingGroup(false);
            SetImportingType(ImportingType.FILE);
        }

        private void InitializeSubscriptionConfigComponents()
        {
            subscriptionConfigComponents = new List<Components.Config>();
        }

        private void InitializeGroupPath()
        {
            groupPath = getCurrentConfigPath.Invoke();
        }

        private void OnSubscriptionTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableSubscriptionTabButton(false);
            SetActiveSubscriptionPanel(true);
        }

        private void OnSubscriptionComboBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            if(comboBoxSubscription.SelectedValue == null)
                return;

            groupPath = ((Subscription)comboBoxSubscription.SelectedValue).Directory.FullName;
            LoadConfigsList(GroupType.SUBSCRIPTION);
            SelectConfig(getCurrentConfigPath.Invoke());
        }

        private void OnAddSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddSubscriptionsServerPanel();
            AnalyticsService.SendEvent(new AddSubButtonClickedEvent());
        }

        private void OnDeleteSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubDeleteButtonClickedEvent());

            if(comboBoxSubscription.SelectedValue == null)
                return;
            
            string remarks = comboBoxSubscription.Text;
            Subscription subscription = (Subscription)comboBoxSubscription.SelectedValue;

            MessageBoxResult result = MessageBox.Show(
                this,
                string.Format(Message.DELETE_CONFIRMATION, remarks),
                Caption.INFO,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
                DeleteSubscription(subscription);
        }

        private void OnEditSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubEditButtonClickedEvent());

            if (comboBoxSubscription.SelectedValue == null)
                return;

            ShowEditSubscriptionServerPanel();
        }

        private void OnUpdateSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubUpdateButtonClickedEvent());
            
            InitializeTextBoxFields();
            UpdateSubscription();

            void InitializeTextBoxFields()
            {
                textBoxSubscriptionRemarks.Text = comboBoxSubscription.Text;
                textBoxSubscriptionLink.Text = ((Subscription)comboBoxSubscription.SelectedValue).Url;
            }

            void UpdateSubscription()
            {
                EditSubscription(
                    subscription: (Subscription)comboBoxSubscription.SelectedValue,
                    remarks: textBoxSubscriptionRemarks.Text,
                    link: textBoxSubscriptionLink.Text
                );
            }
        }

        private void OnImportSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            if (subscriptionOperation == SubscriptionOperation.CREATE)
            {
                AnalyticsService.SendEvent(new SubFromLinkImportedEvent());
                HandleImportingSubscription();
            }
            else if (subscriptionOperation == SubscriptionOperation.EDIT)
            {
                AnalyticsService.SendEvent(new SubFromLinkEditedEvent());
                EditSubscription(
                    subscription: (Subscription)comboBoxSubscription.SelectedValue,
                    remarks: textBoxSubscriptionRemarks.Text,
                    link: textBoxSubscriptionLink.Text
                );
            }
        }

        private void HandleImportingSubscription()
        {
            if (!IsRemarksEntered())
            {
                MessageBox.Show(
                    this,
                    Values.Message.NO_SUBSCRIPTION_REMARKS_ENTERED, 
                    Values.Caption.WARNING, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
                return;
            }
            else if (!IsLinkEntered())
            {
                MessageBox.Show(
                    this,
                    Values.Message.NO_SUBSCRIPTION_LINK_ENTERED, 
                    Values.Caption.WARNING, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
                return;
            }

            SetActiveLoadingPanel(true);
            TryAddSubscription();

            bool IsRemarksEntered() => !string.IsNullOrEmpty(textBoxSubscriptionRemarks.Text);

            bool IsLinkEntered() => !string.IsNullOrEmpty(textBoxSubscriptionLink.Text);

            void TryAddSubscription()
            {
                Status subscriptionStatus;

                subscriptionStatus = convertLinkToSubscription.Invoke(
                    textBoxSubscriptionRemarks.Text, 
                    textBoxSubscriptionLink.Text
                );

                if (subscriptionStatus.Code == Code.ERROR)
                {
                    HandleError();
                    SetActiveLoadingPanel(false);
                    return;
                }

                string[] subscription = GetSubscription();
                groupPath = "";
                onCreateSubscription.Invoke(
                    GetSubscriptionRemark(), 
                    GetSubscriptionUrl(), 
                    GetSubscriptionData()
                );
                onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
                SetActiveLoadingPanel(false);
                LoadGroupsList();
                LoadConfigsList(GroupType.SUBSCRIPTION);
                ClearSubscriptionRemarks();
                ClearSubscriptionPath();
                ShowServersPanel();

                string[] GetSubscription() => (string[])subscriptionStatus.Content;

                string GetSubscriptionUrl() => textBoxSubscriptionLink.Text;

                string GetSubscriptionRemark() => subscription[0];

                string GetSubscriptionData() => subscription[1];

                void HandleError()
                {
                    switch (subscriptionStatus.SubCode)
                    {
                        case SubCode.NO_CONFIG:
                        case SubCode.UNSUPPORTED_LINK:
                            HandleWarningMessage(subscriptionStatus.Content.ToString());
                            break;
                        case SubCode.INVALID_CONFIG:
                            HandleErrorMessage(subscriptionStatus.Content.ToString());
                            break;
                        default:
                            return;
                    }
                }
            }
        }

        private void EditSubscription(Subscription subscription, string remarks, string link)
        {
            string oldRemarks = comboBoxSubscription.Text;

            HandleImportingSubscription();
            if (!HasSameRemarks())
                DeleteSubscription(subscription);

            bool HasSameRemarks() => oldRemarks == comboBoxSubscription.Text;
        }

        private void ShowAddSubscriptionsServerPanel()
        {
            subscriptionOperation = SubscriptionOperation.CREATE;
            ShowSubscriptionPanel();
            InitializeTextBoxFields();

            void ShowSubscriptionPanel()
            {
                panelServers.Visibility = Visibility.Hidden;
                panelAddSubscriptions.Visibility = Visibility.Visible;
            }

            void InitializeTextBoxFields()
            {
                textBoxSubscriptionRemarks.Text = "";
                textBoxSubscriptionLink.Text = "";
            }
        }

        private void ShowEditSubscriptionServerPanel()
        {
            subscriptionOperation = SubscriptionOperation.EDIT;
            ShowSubscriptionPanel();
            FetchTextBoxFields();

            void ShowSubscriptionPanel()
            {
                panelServers.Visibility = Visibility.Hidden;
                panelAddSubscriptions.Visibility = Visibility.Visible;
            }

            void FetchTextBoxFields()
            {
                textBoxSubscriptionRemarks.Text = comboBoxSubscription.Text;
                textBoxSubscriptionLink.Text = ((Subscription)comboBoxSubscription.SelectedValue).Url;
            }
        }

        private void ClearSubscriptionRemarks()
        {
            textBoxSubscriptionRemarks.Text = null;
        }

        private void ClearSubscriptionPath()
        {
            textBoxSubscriptionLink.Text = null;
        }

        private void LoadGroupsList()
        {
            Dictionary<Subscription, string> groups;
            InitializeGroups();
            SelectCurrentGroup();

            void InitializeGroups()
            {
                groups = new Dictionary<Subscription, string>();
                foreach(Subscription group in getAllGroups.Invoke())
                    groups.Add(group, group.Directory.Name);
                
                comboBoxSubscription.ItemsSource = groups;
            }

            void SelectCurrentGroup()
            {
                if (!IsAnyGroupExists())
                    return;

                KeyValuePair<Subscription, string> currentGroup = groups.FirstOrDefault(
                    group => group.Key.Directory.FullName == FileUtility.GetDirectory(groupPath)
                );

                if (!IsCurrentGroupExists())
                    currentGroup = groups.Last();
                
                comboBoxSubscription.SelectedValue = currentGroup.Key;

                bool IsCurrentGroupExists() => currentGroup.Key != null && currentGroup.Value != null;
            }

            bool IsAnyGroupExists() => groups.Count > 0;
        }

        private void LoadSubscriptionConfigsList()
        {
            List<Config> configs = getAllSubscriptionConfigs.Invoke(groupPath);
            List<Subscription> groups = getAllGroups.Invoke();
            subscriptionConfigComponents = new List<Components.Config>();

            ClearConfigsList(listSubscriptions);
            HandleShowingSubscriptionControlPanel();

            HandleShowingNoServerExistsHint(
                configs: configs,
                groups: groups,
                textNoServer: textNoSubscription
            );

            AddAllConfigsToList(
                configs: configs, 
                configComponents: subscriptionConfigComponents, 
                parent: listSubscriptions
            );

            if (IsAnyConfigExists(configs))
                AddConfigHintAtTheEndOfList(listSubscriptions);
                
            void HandleShowingSubscriptionControlPanel()
            {
                if (groups.Count > 0)
                    panelSubscriptionControl.Visibility = Visibility.Visible;
                else
                    panelSubscriptionControl.Visibility = Visibility.Collapsed;
            }
        }

        private string GetLastSubscriptionConfigPath()
        {
            Config lastConfig = getAllSubscriptionConfigs.Invoke(groupPath).LastOrDefault();

            if (!IsConfigExists())
                lastConfig = getAllGeneralConfigs.Invoke().LastOrDefault();
            
            if(!IsConfigExists())
                return null;

            return lastConfig.Path;

            bool IsConfigExists() => lastConfig != null;
        }

        private void SetActiveSubscriptionPanel(bool isActive) => SetActivePanel(panelSubscription, isActive);

        private void SetEnableSubscriptionTabButton(bool isEnabled) => SetEnableButton(buttonSubscriptionTab, isEnabled);
    }
}