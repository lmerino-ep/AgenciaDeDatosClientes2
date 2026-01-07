


function fGetSelectorUnicoParaElemento(dom, aSelector = []) {
    let sSelector = null;
    if (dom !== null && dom !== undefined) {
        let oDomIframe = null;

        // Si tiene id, lo usamos directamente
        if (dom.id !== null && dom.id !== undefined && dom.id.length > 0) {
            sSelector = "#" + dom.id;

            // Si el elemento está dentro de un iframe, agregar el selector del iframe
            // Determinar el contexto del document (principal o dentro de un iframe)
            let oDoc = dom.ownerDocument || document;
            if (oDoc.defaultView !== window) {
                //Estamos dentro de un iframe
                let aDomIframe = Array.from(window.document.querySelectorAll('iframe'));
                oDomIframe = aDomIframe.find(x => x.contentDocument === oDoc);
            }
        } else {
            // Lo generamos según las clases y la jerarquía
            let aRuta = [];
            let iSalvavidas = 0;
            oDomIframe = null;
            while (dom.parentElement && iSalvavidas < 50 && oDomIframe === null) {
                let sSelectorElemento = dom.tagName.toLowerCase();

                let sClasesYAtributosData = "";

                // Si tiene clases, las agregamos:
                if (dom.className !== null && dom.className !== undefined && dom.className.length > 0) {
                    sClasesYAtributosData += "." + Array.from(dom.classList).join('.');
                }

                //Si tiene atributos data, los añadimos:
                for (let sNombrePropiedadData in dom.dataset) {
                    if (dom.dataset.hasOwnProperty(sNombrePropiedadData)) {
                        sClasesYAtributosData += `[data-${sNombrePropiedadData}="${dom.dataset[sNombrePropiedadData]}"]`;
                    }
                }

                // Si hay varios elementos del mismo tipo en el padre, mismas clases y mismos atributos data, agregamos un índice
                let aHermano = Array.from(dom.parentElement.children).filter(e =>
                    e.tagName === dom.tagName &&
                    e.className === dom.className &&
                    JSON.stringify(e.dataset) === JSON.stringify(dom.dataset)
                );
                if (aHermano.length > 1) {
                    if (sClasesYAtributosData.length > 0) {
                        sSelectorElemento += ":nth-child(" + String(aHermano.indexOf(dom) + 1) + " of " + sClasesYAtributosData + ")";
                    } else {
                        sSelectorElemento += ":nth-of-type(" + String(aHermano.indexOf(dom) + 1) + ")";                
                    }                   
                } else {
                    sSelectorElemento += sClasesYAtributosData;
                }

                // Si el elemento está dentro de un iframe, agregar el selector del iframe
                // Determinar el contexto del document (principal o dentro de un iframe)
                let oDoc = dom.ownerDocument || document;
                if (oDoc.defaultView !== window) {
                    let aDomIframe = Array.from(window.document.querySelectorAll('iframe'));
                    oDomIframe = aDomIframe.find(x => x.contentDocument === oDoc);
                }

                aRuta.unshift(sSelectorElemento);
                dom = dom.parentElement;

                iSalvavidas++;
            }
            sSelector = aRuta.join(" > ")
        }
        if (oDomIframe !== null) {
            aSelector.push(sSelector);
            return fGetSelectorUnicoParaElemento(oDomIframe, aSelector);
        }
        aSelector.push(sSelector);
        return aSelector.reverse(); //Invertimos el orden del array, pues lo hemos generado desde el elemento hacia el raíz del documento
    } else {
        return null;
    }
}

function fGetElementoDesdeSelectorEncontrado(aSelector, domPadre = document) {
    let domSelector = null;
    if (aSelector !== null && aSelector !== undefined) {
        for (let i = 0; i < aSelector.length; i++) {
            if (domSelector === null) {
                domSelector = domPadre.querySelector(aSelector[i]);
            } else {
                let domIframe = domSelector.contentDocument || domSelector.contentWindow.document;
                domSelector = domIframe.querySelector(aSelector[i]);
            }
        }
    }
    return domSelector;
}


function fPostSpa(sRuta, oParametros, oCargando, domSpa, domSelectorDomElementoClickado, fCallbackAntesDelRepintado, fCallbackDespuesDelRepintado) {
    let aSelectorDomElementoClickado = null;
    if (domSelectorDomElementoClickado !== null && domSelectorDomElementoClickado !== undefined) {
        aSelectorDomElementoClickado = fGetSelectorUnicoParaElemento(domSelectorDomElementoClickado);
    }
    var fLogicaLlamadaPost = function () {
        try {
            this.oCargando.Mostrar();

            // guardamos la posición relativa con respecto al viewport del usuario
            let iPosicionRelativaConRespectoAlViewport = null;
            if (this.aSelectorDomElementoClickado !== null && this.aSelectorDomElementoClickado !== undefined && this.aSelectorDomElementoClickado.length > 0) {
                let domSelector = fGetElementoDesdeSelectorEncontrado(this.aSelectorDomElementoClickado);
                if (domSelector !== null && domSelector !== undefined) {
                    iPosicionRelativaConRespectoAlViewport = domSelector.getBoundingClientRect().top - window.scrollY;
                } else {
                    iPosicionRelativaConRespectoAlViewport = 0;
                }
            }

            fetch(this.sRuta, {
                method: 'POST',
                headers: {
                    "Content-type": "application/json",
                    "Accept": "application/json",
                    "XSRF-TOKEN": window.AntiForgeryToken === null || window.AntiForgeryToken === undefined ? document.querySelector('meta[name="csrf-token"]')?.getAttribute('content') : window.AntiForgeryToken
                },
                body: JSON.stringify(this.oParametros)
            }).then(function json(oRespuesta) {
                oRespuesta.json().then(function (jRespuesta) {
                    if (jRespuesta.bCorrecto) {
                        if (this.domSpa !== null && this.domSpa !== undefined) {
                            if (this.fCallbackAntesDelRepintado !== null && this.fCallbackAntesDelRepintado !== undefined) {
                                this.fCallbackAntesDelRepintado(jRespuesta);
                            }

                            //Repintamos
                            this.domSpa.innerHTML = jRespuesta.sHtml;

                            requestAnimationFrame(function () { //Actualizamos después del repintado:
                                requestAnimationFrame(function () { // Esperamos un frame extra para asegurar el repintado
                                    setTimeout(function () { // Asegura que el navegador haya finalizado el reflow
                                        //Intentamos restaurar el scroll al que teníamos antes de hacer ajax y teniendo en cuenta que el repintado puede haber cambiado la altura total de la página
                                        let domNuevoElementoIgualQueElQueFueClickado = fGetElementoDesdeSelectorEncontrado(this.aSelectorDomElementoClickado, this.domSpa);
                                        if (domNuevoElementoIgualQueElQueFueClickado !== null && domNuevoElementoIgualQueElQueFueClickado !== undefined) {
                                            window.scrollTo({
                                                top: domNuevoElementoIgualQueElQueFueClickado.getBoundingClientRect().top - this.iPosicionRelativaConRespectoAlViewport,
                                                behavior: "instant"
                                            });
                                        }
                                        if (this.fCallbackDespuesDelRepintado !== null && this.fCallbackDespuesDelRepintado !== undefined) {
                                            this.fCallbackDespuesDelRepintado(jRespuesta);
                                        }
                                        this.oCargando.Ocultar();
                                    }.bind({
                                        domSpa: this.domSpa,
                                        oCargando: this.oCargando,
                                        fCallbackDespuesDelRepintado: this.fCallbackDespuesDelRepintado,
                                        aSelectorDomElementoClickado: this.aSelectorDomElementoClickado,
                                        iPosicionRelativaConRespectoAlViewport: this.iPosicionRelativaConRespectoAlViewport
                                    }), 0);
                                }.bind({
                                    domSpa: this.domSpa,
                                    oCargando: this.oCargando,
                                    fCallbackDespuesDelRepintado: this.fCallbackDespuesDelRepintado,
                                    aSelectorDomElementoClickado: this.aSelectorDomElementoClickado,
                                    iPosicionRelativaConRespectoAlViewport: this.iPosicionRelativaConRespectoAlViewport
                                }));
                            }.bind({
                                domSpa: this.domSpa,
                                oCargando: this.oCargando,
                                fCallbackDespuesDelRepintado: this.fCallbackDespuesDelRepintado,
                                aSelectorDomElementoClickado: this.aSelectorDomElementoClickado,
                                iPosicionRelativaConRespectoAlViewport: this.iPosicionRelativaConRespectoAlViewport
                            }));
                        }
                    } else {
                        oResponsiveAlert.Error(jRespuesta.sMotivo, true);
                        this.oCargando.Ocultar();
                    }
                }.bind({
                    domSpa: this.domSpa,
                    oCargando: this.oCargando,
                    fCallbackAntesDelRepintado: this.fCallbackAntesDelRepintado,
                    fCallbackDespuesDelRepintado: this.fCallbackDespuesDelRepintado,
                    aSelectorDomElementoClickado: this.aSelectorDomElementoClickado,
                    iPosicionRelativaConRespectoAlViewport: this.iPosicionRelativaConRespectoAlViewport
                }));
            }.bind({
                oParametros: this.oParametros,
                domSpa: this.domSpa,
                oCargando: this.oCargando,
                fCallbackAntesDelRepintado: this.fCallbackAntesDelRepintado,
                fCallbackDespuesDelRepintado: this.fCallbackDespuesDelRepintado,
                aSelectorDomElementoClickado: aSelectorDomElementoClickado,
                iPosicionRelativaConRespectoAlViewport: iPosicionRelativaConRespectoAlViewport
            })).catch(function (error) {
                oResponsiveAlert.Error("No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde", true);
                this.oCargando.Ocultar();
            });
        } catch (error) {
            oResponsiveAlert.Error("No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde", true);
            this.oCargando.Ocultar();
        }
    }.bind({
        sRuta: sRuta,
        oParametros: oParametros,
        domSpa: domSpa,
        oCargando: oCargando,
        fCallbackAntesDelRepintado: fCallbackAntesDelRepintado,
        fCallbackDespuesDelRepintado: fCallbackDespuesDelRepintado,
        aSelectorDomElementoClickado: aSelectorDomElementoClickado
    });

    fLogicaLlamadaPost();
}


function fInsertarHtmlEnNodeText(domNodeText, iPosicion, sHtml) {
    var domDiv = document.createElement('div');
    domDiv.innerHTML = sHtml.trim();

    var domPadre = domNodeText.parentElement;

    if (domNodeText.nodeType === 3) {
        //domNodeText es un nodo de tipo texto:
        //Dividimos en 2 el text node e insertamos nuestra etiqueta
        domPadre.insertBefore(domDiv.firstChild, domNodeText.splitText(iPosicion));
    } else {
        domPadre.insertBefore(domDiv.firstChild, domNodeText);
    }
}