let oCargando = new ModalCargando();

document.addEventListener('DOMContentLoaded', function () {
    // Configurar enlaces de descarga para mostrar el modal
    // Usar atributo data-js-descargar para separar la lógica JS de los estilos CSS
    const enlacesDescarga = document.querySelectorAll('[data-js-descargar]');
    enlacesDescarga.forEach(function (enlace) {
        enlace.addEventListener('click', function (e) {
            e.preventDefault();
            const url = this.getAttribute('href') || this.getAttribute('data-js-descargar');
            descargarArchivo(url);
        });
    });
    
    // Configurar botones de eliminación de archivos
    const botonesEliminar = document.querySelectorAll('[data-js-eliminar-archivo]');
    botonesEliminar.forEach(function (boton) {
        boton.addEventListener('click', function (e) {
            e.preventDefault();
            const key = boton.dataset.key;
            const nombre = boton.dataset.nombre || 'este archivo';
            mostrarModalEliminacion(key, nombre);
        });
    });
});

function mostrarModalEliminacion(key, nombre) {
    const modal = document.getElementById('modal-eliminar-archivo');
    if (!modal) return;

    // Establecer el key en el modal
    modal.dataset.key = key;
    document.getElementById('nombre-archivo-eliminar').textContent = nombre;

    // Mostrar el modal
    modal.style.display = 'flex';
}

function ocultarModalEliminacion() {
    const modal = document.getElementById('modal-eliminar-archivo');
    if (modal) {
        modal.style.display = 'none';
    }
}

function volverVersionAnterior() {
    const modal = document.getElementById('modal-eliminar-archivo');
    if (!modal) return;

    const key = modal.dataset.key;
    if (!key) return;

    // Cerrar el modal de eliminación primero
    ocultarModalEliminacion();

    // Mostrar modal de carga
    oCargando.Mostrar();

    // Obtener CarpetaRuta de la URL actual si existe
    const urlParams = new URLSearchParams(window.location.search);
    const carpetaRuta = urlParams.get('CarpetaRuta');

    // Crear formulario y enviarlo
    const form = document.createElement('form');
    form.method = 'POST';
    
    // Construir la URL con handler y CarpetaRuta si existe
    let actionUrl = window.location.pathname + '?handler=VolverVersionAnterior';
    if (carpetaRuta) {
        actionUrl += '&CarpetaRuta=' + encodeURIComponent(carpetaRuta);
    }
    form.action = actionUrl;
    
    // Añadir antiforgery token (usar window.AntiForgeryToken si está disponible, sino buscar input)
    let tokenValue = null;
    if (window.AntiForgeryToken) {
        tokenValue = window.AntiForgeryToken;
    } else {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            tokenValue = tokenInput.value;
        }
    }
    
    if (tokenValue) {
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = tokenValue;
        form.appendChild(tokenInput);
    }

    const keyInput = document.createElement('input');
    keyInput.type = 'hidden';
    keyInput.name = 'key';
    keyInput.value = key;
    form.appendChild(keyInput);

    document.body.appendChild(form);
    form.submit();
}

function eliminarCompletamente() {
    const modal = document.getElementById('modal-eliminar-archivo');
    if (!modal) return;

    const key = modal.dataset.key;
    if (!key) return;

    // Cerrar el modal de eliminación primero
    ocultarModalEliminacion();

    // Mostrar modal de carga
    oCargando.Mostrar();

    // Obtener CarpetaRuta de la URL actual si existe
    const urlParams = new URLSearchParams(window.location.search);
    const carpetaRuta = urlParams.get('CarpetaRuta');

    // Crear formulario y enviarlo
    const form = document.createElement('form');
    form.method = 'POST';
    
    // Construir la URL con handler y CarpetaRuta si existe
    let actionUrl = window.location.pathname + '?handler=EliminarCompletamente';
    if (carpetaRuta) {
        actionUrl += '&CarpetaRuta=' + encodeURIComponent(carpetaRuta);
    }
    form.action = actionUrl;
    
    // Añadir antiforgery token (usar window.AntiForgeryToken si está disponible, sino buscar input)
    let tokenValue = null;
    if (window.AntiForgeryToken) {
        tokenValue = window.AntiForgeryToken;
    } else {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            tokenValue = tokenInput.value;
        }
    }
    
    if (tokenValue) {
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = tokenValue;
        form.appendChild(tokenInput);
    }

    const keyInput = document.createElement('input');
    keyInput.type = 'hidden';
    keyInput.name = 'key';
    keyInput.value = key;
    form.appendChild(keyInput);

    document.body.appendChild(form);
    form.submit();
}

function descargarArchivo(url) {
    // Mostrar modal de carga
    oCargando.Mostrar();

    // Crear un elemento <a> temporal para iniciar la descarga
    const link = document.createElement('a');
    link.href = url;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Ocultar el modal después de un tiempo razonable
    // Nota: No podemos detectar cuando termina la descarga del navegador,
    // así que ocultamos después de un tiempo estimado
    setTimeout(function () {
        oCargando.Ocultar();
    }, 2000);
}

