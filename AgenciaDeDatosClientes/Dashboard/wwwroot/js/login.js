/**
 * Muestra un modal de cargando durante el login
 */
function fMostrarCargando(sNonce) {
    CargarCssDinamicamente("/css/plugins/modalcargando.css", sNonce);
    CargarScriptDinamicamente(false, true, "/js/plugins/modalcargando.min.js", sNonce, null, function () {
        let oCargando = new ModalCargando();
        oCargando.Mostrar();
    });
}

/**
 * Muestra alertas de error si hay errores de validación
 */
(function() {
    const aDomError = document.querySelectorAll('#divError li');
    let aError = [];

    for (const domError of aDomError) {
        let sError = domError.innerText.trim();
        if (sError.length > 0) {
            aError.push(sError);
        }
    }

    if (aError.length > 0) {
        const divError = document.getElementById('divError');
        if (divError) {
            divError.classList.remove('div-error-oculto');
            divError.classList.add('div-error-visible');
        }
    }
})();
