using AutoLogin.Token;
using AutoLogin.Verification.Interfaces;
using Dashboard.Utils.Filters;
using Dashboard.Utils.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Modelos.InyeccionDeDependencias;
using Nuget.Helper;
using NugetPortable.Cache;
using NugetPortable.Herramientas.CDN;
using NugetPortable.SQS;

namespace Dashboard.Pages
{
    public class IndexModel : PageModel
    {
        public ETag ETag { get; set; } //Ojo: esta propiedad debe llamarse así. No cambiar, porque lo casteamos cmomo dynamic con este nombre (ETag) desde el ETagFilterControllers para evitar usar reflection
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ILogger<IndexModel> logger,
            LoggerManual loggerManual,
            IRedis redis,
            Aurora auroraConfig,
            Anthropic anthropic,
            OpenAI openAI,
            Zilliz zilliz,
            Configuracion configuracion,
            Transparent transparentCDN,
            Modelos.InyeccionDeDependencias.Google google,
            ITokenHandler autoLoginTokenHandler,
            ITokenDbService autoLoginTokenDbService,
            S3 s3,
            ISqs sqs,
            Modelos.InyeccionDeDependencias.Sqs sqsConfig
        )
        {
            _logger = logger;
            
            HelperS3 amazonS3 = new HelperS3(
                s3.Bucket,
                s3.AccessKeyId,
                s3.CloudfrontDistributionId,
                s3.SecretAccessKey,
                transparentCDN
            );

            //TODO: Incluímos en el constructor todas las DI solo para probar que el proyecto está correctamente configurado
        }

        public void OnGet()
        {

        }
    }
}
