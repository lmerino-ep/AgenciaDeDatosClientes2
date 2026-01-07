function fInicializaDropdownFiltrables() {
    var fDispararEventoOnChange = function (dom) {
        if ("createEvent" in document) {
            var evt = document.createEvent("HTMLEvents");
            evt.initEvent("change", false, true);
            dom.dispatchEvent(evt);
        } else {
            dom.fireEvent("onchange");
        }
    };

    var aDomDropdownFiltrable = document.getElementsByClassName("dropdownfiltrable");

    var aDom = new Array();

    var fGetIndice = function (dom) {
        var iSOS = 15;
        var i = 0;
        var sId = null;
        while (sId === null && i < iSOS) {
            if (dom.dataset.id !== null && dom.dataset.id !== undefined) {
                sId = dom.dataset.id;
            } else {
                dom = dom.parentElement;
            }
            i++;
        }
        return sId == null ? null : aDom.findIndex(x => x.iId === parseInt(sId));
    };

    var fCargarListadoEnCallbackInput = function (domElementos, domIconoUp, domIconoDown) {
        var aDomGrupo = domElementos.querySelectorAll("p");
        for (var j = 0; j < aDomGrupo.length; j++) {
            var aDomElemento = aDomGrupo[j].querySelectorAll("span");
            for (var k = 0; k < aDomElemento.length; k++) {
                aDomElemento[k].innerHTML = aDomElemento[k].dataset.texto;
                aDomElemento[k].classList.remove("oculto");
            }
            aDomGrupo[j].classList.remove("oculto");
        }
        domElementos.classList.add("visible");
        domIconoDown.classList.add("visible");
        domIconoUp.classList.remove("visible");
    };

    var fFormatearTexto = function (sTextoOriginal, sSeparador) {
        var aCaracteresEspeciales = [{ original: "á", conversion: "a" }, { original: "é", conversion: "e" },
        { original: "ú", conversion: "u" }, { original: "í", conversion: "i" }, { original: "ó", conversion: "o" },
        { original: "ü", conversion: "u" }];

        var sTexto = sTextoOriginal.toLowerCase();
        var i;

        sSeparador = sSeparador.toLowerCase();
        var aCaracterEspecialParaConvertir = new Array();
        for (i = 0; i < aCaracteresEspeciales.length; i++) {
            var aSeparador = sSeparador.split(aCaracteresEspeciales[i].conversion);
            if (aSeparador === 1) {
                aCaracterEspecialParaConvertir.push(aCaracteresEspeciales[i]);
            }
        }
        if (aCaracterEspecialParaConvertir.length === 0) {
            aCaracterEspecialParaConvertir = aCaracteresEspeciales;
        }

        for (i = 0; i < aCaracterEspecialParaConvertir.length; i++) {
            sTexto = sTexto.split(aCaracterEspecialParaConvertir[i].original).join(aCaracterEspecialParaConvertir[i].conversion);
            sSeparador = sSeparador.split(aCaracterEspecialParaConvertir[i].original).join(aCaracterEspecialParaConvertir[i].conversion);
        }

        var aSplit = sTexto.split(sSeparador);
        var sTextoFormateado = null;

        if (aSplit.length > 1) {
            var indice = 0;
            var iLongitudSeparador = sSeparador.length;
            var iLongitudTexto = sTextoOriginal.length;
            sTextoFormateado = "";
            for (i = 0; i < aSplit.length; i++) {
                var iLongitudSplit = aSplit[i].length;
                var sSubstring = sTextoOriginal.substring(indice, indice + iLongitudSplit);
                sTextoFormateado += sSubstring;
                indice += iLongitudSplit;
                if (indice === iLongitudTexto) {
                    break;
                }
                sTextoFormateado += "<strong  class='letraencontrada'>" + sTextoOriginal.substring(indice, indice + iLongitudSeparador) + "</strong>";
                indice += iLongitudSeparador;
            }
        }

        return sTextoFormateado;
    };

    for (var i = 0; i < aDomDropdownFiltrable.length; i++) {
        if (aDomDropdownFiltrable[i].dataset.id === null || aDomDropdownFiltrable[i].dataset.id === undefined) {
            aDomDropdownFiltrable[i].dataset.id = String(i);
            var oDom = {
                iId: i,
                domCaja: aDomDropdownFiltrable[i].querySelector(".caja"),
                domElementos: aDomDropdownFiltrable[i].querySelector(".elementos"),
                domIconoUp: aDomDropdownFiltrable[i].querySelector(".icono-up"),
                domIconoDown: aDomDropdownFiltrable[i].querySelector(".icono-down"),
                domBoton: aDomDropdownFiltrable[i].querySelector(".boton"),
                fTeclaPulsada: function (oEvento) {
                    var iIndice = fGetIndice(oEvento.target);
                    if (iIndice !== null) {
                        var sTextoUsuario = aDom[iIndice].domCaja.value;
                        var aDomGrupo = aDom[iIndice].domElementos.querySelectorAll("p");
                        for (var j = 0; j < aDomGrupo.length; j++) {
                            var aDomElemento = aDomGrupo[j].querySelectorAll("span");
                            var bAlMenosUnElementoDeGrupoEncontrado = false;
                            for (var k = 0; k < aDomElemento.length; k++) {
                                var sTextoFormateado = fFormatearTexto(aDomElemento[k].dataset.texto, sTextoUsuario);
                                if (sTextoFormateado === null) {
                                    aDomElemento[k].innerHTML = aDomElemento[k].dataset.texto;
                                    aDomElemento[k].classList.add("oculto");
                                } else {
                                    if (aDomElemento[k].dataset.habilitado !== undefined && aDomElemento[k].dataset.habilitado.toLowerCase() === "true") {
                                        aDomElemento[k].innerHTML = sTextoFormateado;
                                        aDomElemento[k].classList.remove("oculto");
                                        bAlMenosUnElementoDeGrupoEncontrado = true;
                                    }
                                }
                            }
                            if (bAlMenosUnElementoDeGrupoEncontrado) {
                                aDomGrupo[j].classList.remove("oculto");
                            } else {
                                aDomGrupo[j].classList.add("oculto");
                            }
                        }
                        aDom[iIndice].domElementos.classList.add("visible");
                        aDom[iIndice].domIconoDown.classList.add("visible");
                        aDom[iIndice].domIconoUp.classList.remove("visible");
                    }
                }
            };
            aDom.push(oDom);

            oDom.domBoton.addEventListener("click", function (oEvento) {
                var iIndice = fGetIndice(oEvento.target);
                if (iIndice !== null) {
                    if (aDom[iIndice].domElementos.classList.contains("visible")) {
                        aDom[iIndice].domElementos.classList.remove("visible");
                        aDom[iIndice].domIconoDown.classList.remove("visible");
                        aDom[iIndice].domIconoUp.classList.add("visible");
                    } else {
                        aDom[iIndice].domElementos.classList.add("visible");
                        aDom[iIndice].domIconoDown.classList.add("visible");
                        aDom[iIndice].domIconoUp.classList.remove("visible");
                        fCargarListadoEnCallbackInput(aDom[iIndice].domElementos, aDom[iIndice].domIconoUp, aDom[iIndice].domIconoDown);
                    }
                }
            }, false);

            oDom.domBoton.addEventListener("focusout", function (oEvento) {
                var iIndice = fGetIndice(oEvento.target);
                if (iIndice !== null) {
                    if (oEvento.relatedTarget !== null && oEvento.relatedTarget.dataset.valor !== undefined && oEvento.relatedTarget.dataset.texto !== undefined && oEvento.relatedTarget.dataset.habilitado !== undefined && oEvento.relatedTarget.dataset.habilitado.toLowerCase() === "true") {
                        if (aDom[iIndice].domCaja.value !== oEvento.relatedTarget.dataset.texto) {
                            aDom[iIndice].domCaja.value = oEvento.relatedTarget.dataset.texto;
                            fDispararEventoOnChange(aDom[iIndice].domCaja);
                        }
                    }
                    aDom[iIndice].domElementos.classList.remove("visible");
                    aDom[iIndice].domIconoDown.classList.remove("visible");
                    aDom[iIndice].domIconoUp.classList.add("visible");

                    aDom[iIndice].domCaja.removeEventListener("input", aDom[iIndice].fTeclaPulsada, false);
                }
            }, false);

            oDom.domCaja.addEventListener("focusout", function (oEvento) {
                var iIndice = fGetIndice(oEvento.target);
                if (iIndice !== null) {
                    if (oEvento.relatedTarget !== null && oEvento.relatedTarget.dataset.valor !== undefined && oEvento.relatedTarget.dataset.texto !== undefined && oEvento.relatedTarget.dataset.habilitado !== undefined && oEvento.relatedTarget.dataset.habilitado.toLowerCase() === "true") {
                        if (aDom[iIndice].domCaja.value !== oEvento.relatedTarget.dataset.texto) {
                            aDom[iIndice].domCaja.value = oEvento.relatedTarget.dataset.texto;
                            fDispararEventoOnChange(aDom[iIndice].domCaja);
                        }
                    }
                    aDom[iIndice].domElementos.classList.remove("visible");
                    aDom[iIndice].domIconoDown.classList.remove("visible");
                    aDom[iIndice].domIconoUp.classList.add("visible");

                    aDom[iIndice].domCaja.removeEventListener("input", aDom[iIndice].fTeclaPulsada, false);
                }
            }, false);

            oDom.domCaja.addEventListener("focusin", function (oEvento) {
                var iIndice = fGetIndice(oEvento.target);
                if (iIndice !== null) {
                    if (aDom[iIndice].domCaja.value.length > 0) {
                        aDom[iIndice].fTeclaPulsada({ target: aDom[iIndice].domCaja });
                    }
                    aDom[iIndice].domCaja.addEventListener("input", aDom[iIndice].fTeclaPulsada, false);
                }
            }, false);

            let aDomValor = oDom.domElementos.querySelectorAll("span");
            let aValor = new Array();
            for (const oDomValor of aDomValor) {
                if (oDomValor.dataset.url !== null && oDomValor.dataset.url !== undefined && oDomValor.dataset.url.length > 0) {
                    aValor.push({ sTexto: oDomValor.innerHTML, sUrl: oDomValor.dataset.url });
                }
            }
            oDom.domCaja.addEventListener("change", function () {
                let sTexto = this.dom.value;
                let oValor = this.aValor.find(x => x.sTexto === sTexto);
                if (oValor !== null && oValor !== undefined) {
                    window.location.href = oValor.sUrl;
                }
            }.bind({ dom: oDom.domCaja, aValor: aValor }), false);
        }
    }
}

(function () { fInicializaDropdownFiltrables(); })();
