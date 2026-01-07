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
});

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

