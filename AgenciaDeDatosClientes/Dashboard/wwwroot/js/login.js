/**
 * Muestra un modal de cargando durante el login
 */
function fMostrarCargando(sNonce) {
    // Crear overlay de cargando
    const divOverlay = document.createElement('div');
    divOverlay.id = 'modal-cargando';
    divOverlay.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.7);
        z-index: 9999;
        display: flex;
        justify-content: center;
        align-items: center;
        flex-direction: column;
    `;

    // Crear spinner
    const divSpinner = document.createElement('div');
    divSpinner.style.cssText = `
        border: 4px solid #f3f3f3;
        border-top: 4px solid #3498db;
        border-radius: 50%;
        width: 50px;
        height: 50px;
        animation: spin 1s linear infinite;
    `;

    // Crear mensaje
    const divMensaje = document.createElement('div');
    divMensaje.textContent = 'Iniciando sesión...';
    divMensaje.style.cssText = `
        color: white;
        font-size: 18px;
        margin-top: 20px;
        font-family: Arial, sans-serif;
    `;

    // Agregar estilos de animación
    const style = document.createElement('style');
    style.setAttribute('nonce', sNonce);
    style.textContent = `
        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
    `;

    // Ensamblar y agregar al DOM
    document.head.appendChild(style);
    divOverlay.appendChild(divSpinner);
    divOverlay.appendChild(divMensaje);
    document.body.appendChild(divOverlay);

    return true; // Permitir que el formulario se envíe normalmente
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
            divError.style.display = 'block';
            divError.style.visibility = 'visible';
            divError.style.padding = '15px';
            divError.style.marginBottom = '20px';
            divError.style.backgroundColor = '#f8d7da';
            divError.style.color = '#721c24';
            divError.style.border = '1px solid #f5c6cb';
            divError.style.borderRadius = '4px';
        }
    }
})();
