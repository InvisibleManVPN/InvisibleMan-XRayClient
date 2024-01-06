namespace InvisibleManXRay.Handlers
{
    using Models;
    using Templates;

    public class TemplateHandler : Handler
    {
        private ConfigTemplate configTemplate;
        private SubscriptionTemplate subscriptionTemplate;

        public TemplateHandler()
        {
            InitializeTemplates();
            RegisterTemplates();
            SetupTemplates();

            void InitializeTemplates()
            {
                this.configTemplate = new ConfigTemplate();
                this.subscriptionTemplate = new SubscriptionTemplate();
            }

            void RegisterTemplates()
            {
                configTemplate.RegisterTemplates();
                subscriptionTemplate.RegisterTemplates();
            }

            void SetupTemplates()
            {
                subscriptionTemplate.Setup(configTemplate.ConverLinkToConfig);
            }
        }

        public Status ConverLinkToConfig(string link) => configTemplate.ConverLinkToConfig(link);

        public Status ConvertLinkToSubscription(string remark, string link) => subscriptionTemplate.ConvertLinkToSubscription(remark, link);
    }
}