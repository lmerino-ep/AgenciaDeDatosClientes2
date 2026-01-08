namespace Modelos.InyeccionDeDependencias
{
    public class Google
    {
        public bool UseSecretsManager { get; set; } = false;
        public string NombreDelSecreto { get; set; }

        public string SecretoClaveInstance { get; set; }
        public string SecretoClaveClientId { get; set; }
        public string SecretoClaveClientSecret { get; set; }
        public string SecretoClaveCallbackPath { get; set; }
        public string SecretoClaveReCaptchaClaveSitio { get; set; }
        public string SecretoClaveReCaptchaClaveSecreta { get; set; }

        public string Instance { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CallbackPath { get; set; }
        public string ReCaptchaClaveSitio { get; set; }
        public string ReCaptchaClaveSecreta { get; set; }
    }
}

