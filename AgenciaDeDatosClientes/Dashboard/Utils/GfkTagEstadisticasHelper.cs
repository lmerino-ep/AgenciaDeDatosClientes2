using System.Text;

namespace Dashboard.Utils
{
    public static class GfkTagEstadisticasHelper
    {

        public const string TagBeginGfk = "<!-- BEGIN GFK TAG -->"; //Por si alguna vez tenemos que localizarlo para suprimir o modificar ... YA TÚ SABES!
        public const string TagEndGfk = "<!-- END GFK TAG -->";

        /*
         En el caso de EpData vamos a medir el impacto a nivel global. Por tanto, los customParameters y en específico
         el c2 será homepage
            Para más información https://europapress.visualstudio.com/webep/_wiki/wikis/webep.wiki/158/Gfk-Tag-Estad%C3%ADsticas
         */

        /// <summary>
        /// Obtiene la configuración para envíar a Gfk        /// 
        /// </summary>
        /// <returns>Devuelve empty si no hemos podido generar el tag; el tag con la configuración y ejecución en caso de todo correcto</returns>
        public static String GetScriptSeguimientoGfk(string nonce)
        {
            StringBuilder localWriter = new StringBuilder();
            string mediaIdGfk = GetGfkMediaIdConfigSiteTrackerByPortal();

            StringBuilder functionJsSendGfkImpression = GetFunctionJsSendGfkImpression();

            if (functionJsSendGfkImpression == null) return localWriter.ToString();

            localWriter.AppendLine(TagBeginGfk);
            if (String.IsNullOrEmpty(nonce))
            {
                localWriter.AppendLine($"<script async type=\"text/javascript\"> ");
            }
            else
            {
                localWriter.AppendLine($"<script nonce=\"{nonce}\" async type=\"text/javascript\"> ");
            }

            localWriter.Append(GetJsGfkS2sConf(mediaIdGfk).ToString());
            localWriter.Append(GetJsSdkSiteTracker().ToString());
            localWriter.Append(functionJsSendGfkImpression.ToString());

            localWriter.AppendLine("</script> ");
            localWriter.AppendLine(TagEndGfk);

            return localWriter.ToString();
        }



        private static StringBuilder GetFunctionJsSendGfkImpression()
        {
            string[] customParams = GetGfkCustomParams();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("function sendGfkImpression() {");
            stringBuilder.AppendLine("try {");
            stringBuilder.AppendLine("  var agent = gfkS2s.getAgent();");
            stringBuilder.AppendLine($" var customParams = {{}}; customParams.c1 = '{customParams[0]}'; customParams.c2 = '{customParams[1]}';");
            stringBuilder.AppendLine("agent.impression(\"default\", customParams);");
            stringBuilder.AppendLine("} catch (error) {console.log(error);}");
            stringBuilder.AppendLine("};");
#if !DEBUG
            stringBuilder.AppendLine("sendGfkImpression();");
#endif
            return stringBuilder;
        }



        public static void GetHtmlTagSelloGfkAuditoria(bool formatoFondoClaro = true)
        {
            string idFondoClaro = (formatoFondoClaro ? "1" : "2");
            StringBuilder localWriter = new StringBuilder();
            localWriter.Append($@"<div> 
                                    <a href=""https://insights.gfk.es/sello-gfk-dam?hs_preview=qRNhKeWr-133497780591"" target=""_blank""> 
                                        <img height=""120"" src=""https://campo.gfk.es/dam/seal?uuid=AB34D3B4-8D5D-451E-91F2-55FCE7C003C3&desid={formatoFondoClaro}"" alt=""Certificado bajo medición GfK DAM"">
                                    </a>
                                 </div>");
        }


        private static StringBuilder GetJsGfkS2sConf(string mediaIdGfk)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(" var gfkS2sConf = { ");
            stringBuilder.AppendLine($"  media: \"{mediaIdGfk}\",");
            stringBuilder.AppendLine($"  url: \"//es-{GetUrlConfigSensic()}\","); // for preproduction
            stringBuilder.AppendLine("   type: \"WEB\", ");
            stringBuilder.AppendLine($"  logLevel: \"{GetGfkLogLevel()}\", ");
            stringBuilder.AppendLine("   crashReporting: true");
            stringBuilder.AppendLine(" }; ");

            return stringBuilder;
        }



        private static StringBuilder GetJsSdkSiteTracker()
        {
            StringBuilder localWriter = new StringBuilder();
            localWriter.Append(@" 
                                    try {
                                        (function (w, d, c, s, id) {
                                            if (d.getElementById(id)) {
                                                return;
                                            }

                                            w.gfkS2sConf = c;
                                            w[id] = {};
                                            w[id].agents = [];
                                            var api = [""playStreamLive"", ""playStreamOnDemand"", ""stop"", ""skip"", ""screen"", ""volume"", ""impression""];
                                            w.gfks = (function () {
                                                function f(sA, e, cb) {
                                                    return function () {
                                                        sA.p = cb();
                                                        sA.queue.push({ f: e, a: arguments });
                                                    };
                                                }
                                                function s(c, pId, cb) {
                                                    var sA = { queue: [], config: c, cb: cb, pId: pId };
                                                    for (var i = 0; i < api.length; i++) {
                                                        var e = api[i];
                                                        sA[e] = f(sA, e, cb);
                                                    }
                                                    return sA;
                                                }
                                                return s;
                                            })();
                                            w[id].getAgent = function (cb, pId) {
                                                var a = {
                                                    a: new w.gfks(c, pId || """", cb || function () {
                                                        return 0;
                                                    })
                                                };
                                                function g(a, e) {
                                                    return function () {
                                                        return a.a[e].apply(a.a, arguments);
                                                    }
                                                };
                                                for (var i = 0; i < api.length; i++) {
                                                    var e = api[i];
                                                    a[e] = g(a, e);
                                                }
                                                w[id].agents.push(a);
                                                return a;
                                            };

                                            var lJS = function (eId, url) {
                                                var tag = d.createElement(s);
                                                var el = d.getElementsByTagName(s)[0];
                                                tag.id = eId;
                                                tag.async = true;
                                                tag.type = 'text/javascript';
                                                tag.src = url;
                                                tag.setAttribute('crossorigin', 'anonymous');
                                                el.parentNode.insertBefore(tag, el);
                                            };

                                            lJS(id, c.url);
                                        })(window, document, gfkS2sConf, 'script', 'gfkS2s');
                                    } catch (error) {
                                        console.log(error);
                                    }
                                ");

            return localWriter;
        }


        private static string[] GetGfkCustomParams()
        {

            string param1 = "epdata.es";
            string param2 = "homepage";

            return new string[] { param1, param2 };
        }


        private static string GetGfkMediaIdConfigSiteTrackerByPortal()
        {
            return "EpdataWeb";

        }


        private static string GetUrlConfigSensic()
        {
            /**
            * For preproduction config-preproduction.sensic.net/s2s-web.js
            * Una vez comprobado se debe mantener la url de producción config.sensic.net/s2s-web.js
            */

            return "config.sensic.net/s2s-web.js";
            //return "config-preproduction.sensic.net/s2s-web.js";

        }

        private static string GetGfkLogLevel()
        {
            // Log level posibles  "none" < "error" < "warn" < "info" < "log" < "debug"
            // Para testear debug, por defecto none

            return "none";
            //return "debug";



        }



    }
}
