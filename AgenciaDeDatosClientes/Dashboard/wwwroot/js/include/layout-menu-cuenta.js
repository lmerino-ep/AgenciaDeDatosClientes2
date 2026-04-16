/**
 * Carga asíncrona del menú de cuenta y del menú móvil (opciones mi cuenta).
 */
function fOpcionesMiCuenta() {
    try {
        var domContenido = document.getElementById('contenido-opciones-mi-cuenta');
        if (domContenido !== null && domContenido !== undefined) {
            var oParametros = {};
            var sRuta = '/opciones-mi-cuenta';
            fetch(sRuta, {
                method: 'POST',
                headers: {
                    'Content-type': 'application/json',
                    Accept: 'application/json'
                },
                body: JSON.stringify(oParametros)
            }).then(function json(oRespuesta) {
                oRespuesta.json().then(function (jRespuesta) {
                    if (jRespuesta.bCorrecto) {
                        domContenido.innerHTML = jRespuesta.sHtml;
                        domContenido.dataset.relleno = 'true';
                    } else {
                        console.error(jRespuesta.sMotivo);
                    }
                });
            }).catch(function () {
                console.error('No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde');
            });
        }
    } catch (error) {
        console.error('No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde');
    }
}

function fOpcionesMenuMovil() {
    try {
        var domContenido = document.getElementById('contenido-menu-movil');
        if (domContenido !== null && domContenido !== undefined) {
            var oParametros = {};
            var sRuta = '/opciones-mi-cuenta';
            fetch(sRuta, {
                method: 'POST',
                headers: {
                    'Content-type': 'application/json',
                    Accept: 'application/json'
                },
                body: JSON.stringify(oParametros)
            }).then(function json(oRespuesta) {
                oRespuesta.json().then(function (jRespuesta) {
                    if (jRespuesta.bCorrecto) {
                        domContenido.innerHTML = jRespuesta.sHtml;
                        domContenido.dataset.relleno = 'true';
                    } else {
                        console.error(jRespuesta.sMotivo);
                    }
                });
            }).catch(function () {
                console.error('No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde');
            });
        }
    } catch (error) {
        console.error('No pudimos conectar con el servidor, por favor, inténtelo de nuevo más tarde');
    }
}
