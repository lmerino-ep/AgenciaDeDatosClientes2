namespace Modelos.InyeccionDeDependencias
{
    public class LemonSqueezy
    {
        public bool UseSecretsManager { get; set; } = false;

        public string NombreDelSecreto { get; set; }

        public string SecretoClaveApiKey { get; set; }

        public string SecretoClaveSigningSecret { get; set; }

        public string ApiKey { get; set; }

        public string SigningSecret { get; set; }

        public string StoreId { get; set; }

        public string VariantId { get; set; }

        public string CheckoutSuccessPath { get; set; }

        public string CheckoutCancelPath { get; set; }
    }
}
