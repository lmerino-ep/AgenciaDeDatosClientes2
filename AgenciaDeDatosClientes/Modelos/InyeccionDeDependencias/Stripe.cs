namespace Modelos.InyeccionDeDependencias
{
    public class Stripe
    {
        public bool UseSecretsManager { get; set; } = false;

        public string NombreDelSecreto { get; set; }

        public string SecretoClaveSecretKey { get; set; }

        public string SecretoClaveWebhookSecret { get; set; }

        public string SecretKey { get; set; }

        public string WebhookSecret { get; set; }

        public string CheckoutSuccessPath { get; set; }

        public string CheckoutCancelPath { get; set; }

        public string CheckoutSuccessPathResultados { get; set; }

        public string CheckoutCancelPathResultados { get; set; }
    }
}
