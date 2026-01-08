let oCargando = null;

// Configurar el formulario de sobrescribir
document.addEventListener('DOMContentLoaded', function () {
    const formSobrescribir = document.querySelector('[data-js-form-sobrescribir]');
    if (formSobrescribir) {
        // Inicializar ModalCargando
        oCargando = new ModalCargando();

        formSobrescribir.addEventListener('submit', function (e) {
            // Validar que hay archivo seleccionado antes de mostrar el modal
            const inputArchivo = document.querySelector('input[type="file"]');
            if (inputArchivo && inputArchivo.files && inputArchivo.files.length > 0) {
                oCargando.Mostrar();
            }
        });
    }
});

