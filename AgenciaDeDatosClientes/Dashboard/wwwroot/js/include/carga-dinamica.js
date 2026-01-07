function CargarScriptDinamicamente(bAsync, bDefer, sUrl, sNonce, sDependencia = null, fResolve = null, fReject = null) {
    const domScriptYaInsertado = document.querySelector("script[src='" + sUrl + "']");
    if (domScriptYaInsertado === null || domScriptYaInsertado === undefined) {
        const bHayDependenciaDeScript = sDependencia !== null && sDependencia !== undefined && sDependencia.length > 0;
        let domScriptDependenciaYaCargado = null;
        if (bHayDependenciaDeScript) {
            domScriptDependenciaYaCargado = document.querySelector("script[src='" + sDependencia + "']");
        }
        if (bHayDependenciaDeScript && (domScriptDependenciaYaCargado === null || domScriptDependenciaYaCargado === undefined)) {
            //Esperamos
            const rIC = requestIdleCallback ?? setTimeout
            rIC(function () {
                CargarScriptDinamicamente(this.bAsync, this.bDefer, this.sUrl, this.sNonce, this.sDependencia, this.fResolve, this.fReject);
            }.bind({ bAsync: bAsync, bDefer: bDefer, sUrl: sUrl, sNonce: sNonce, sDependencia: sDependencia, fResolve: fResolve, fReject: fReject }));
        } else {
            const domScript = document.createElement("script");
            domScript.type = "text/javascript";
            if (bAsync) {
                domScript.setAttribute("async", "async");
            } else {
                if (bDefer) {
                    domScript.setAttribute("defer", "defer");
                }
            }
            domScript.setAttribute("nonce", sNonce ?? document.body.dataset.nonce);
            domScript.setAttribute("fetchpriority", "low");
            domScript.src = sUrl;

            if (fResolve !== null && fResolve !== undefined) {
                domScript.addEventListener("load", (oEvento) => {
                    fResolve({ status: true });
                });
            }
            if (fReject !== null && fReject !== undefined) {
                domScript.addEventListener("error", (oEvento) => {
                    fReject(new Error("Error al cargar el script"));
                });
            }

            document.body.appendChild(domScript);
        }
    } else {
        if (fResolve !== null && fResolve !== undefined) {
            fResolve({ status: true });
        }
    }
}

function CargarCssDinamicamente(sUrl, sNonce, fResolve = null, fReject = null) {
    var domCssYaInsertado = document.querySelector("link[href='" + sUrl + "']");
    if (domCssYaInsertado === null || domCssYaInsertado === undefined) {
        const domCss = document.createElement("link");
        domCss.setAttribute("nonce", sNonce ?? document.body.dataset.nonce);
        domCss.type = "text/css";
        domCss.rel = "stylesheet";
        domCss.media = "all";
        domCss.href = sUrl;

        if (fResolve !== null && fResolve !== undefined) {
            domCss.addEventListener("load", (oEvento) => {
                fResolve({ status: true });
            });
        }
        if (fReject !== null && fReject !== undefined) {
            domCss.addEventListener("error", (oEvento) => {
                fReject(new Error("Error al cargar el css"));
            });
        }

        document.body.appendChild(domCss);
    } else {
        if (fResolve !== null && fResolve !== undefined) {
            fResolve({ status: true });
        }
    }
}