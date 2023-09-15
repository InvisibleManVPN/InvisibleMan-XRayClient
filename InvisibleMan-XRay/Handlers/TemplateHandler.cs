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
        }

        public Status ConverLinkToV2Ray(string link) => configTemplate.ConverLinkToV2Ray(link);

        public Status ConvertLinkToSubscription(string remark, string link) => subscriptionTemplate.ConvertLinkToSubscription(remark, link);
    }
}