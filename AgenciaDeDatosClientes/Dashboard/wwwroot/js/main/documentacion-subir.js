let oCargando = null;

// Configurar el formulario de subida
document.addEventListener('DOMContentLoaded', function () {
    const formSubir = document.querySelector('[data-js-form-subir]');
    if (formSubir) {
        // Inicializar ModalCargando
        oCargando = new ModalCargando();

        formSubir.addEventListener('submit', function (e) {
            // Validar que hay archivos seleccionados antes de mostrar el modal
            const inputArchivos = document.getElementById('Archivos');
            if (inputArchivos && inputArchivos.files && inputArchivos.files.length > 0) {
                oCargando.Mostrar();
            }
        });
    }

    // Configurar drag & drop en toda la página
    configurarDragAndDrop();
});

function configurarDragAndDrop() {
    const inputArchivos = document.getElementById('Archivos');
    if (!inputArchivos) return;

    // Crear overlay visual para drag & drop
    const overlay = document.createElement('div');
    overlay.id = 'drag-drop-overlay';
    overlay.className = 'drag-drop-overlay';
    overlay.innerHTML = '<div class="drag-drop-message"><i class="icono-upload"></i><p>Suelta los archivos aquí para subirlos</p></div>';
    document.body.appendChild(overlay);

    // Prevenir el comportamiento por defecto del navegador
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        document.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    let dragCounter = 0;

    // Mostrar overlay cuando se arrastra sobre la página
    document.addEventListener('dragenter', function (e) {
        // Verificar que se están arrastrando archivos
        if (e.dataTransfer.types.includes('Files')) {
            dragCounter++;
            overlay.classList.add('active');
        }
    }, false);

    document.addEventListener('dragover', function (e) {
        // Prevenir comportamiento por defecto y permitir drop
        if (e.dataTransfer.types.includes('Files')) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
        }
    }, false);

    // Ocultar overlay cuando se sale del área
    document.addEventListener('dragleave', function (e) {
        dragCounter--;
        if (dragCounter === 0) {
            overlay.classList.remove('active');
        }
    }, false);

    // Manejar el drop de archivos
    document.addEventListener('drop', function (e) {
        dragCounter = 0;
        overlay.classList.remove('active');
        
        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            // Crear un DataTransfer para añadir los archivos al input
            const dataTransfer = new DataTransfer();
            
            // Añadir archivos existentes si los hay
            if (inputArchivos.files) {
                for (let i = 0; i < inputArchivos.files.length; i++) {
                    dataTransfer.items.add(inputArchivos.files[i]);
                }
            }
            
            // Añadir los nuevos archivos
            for (let i = 0; i < files.length; i++) {
                dataTransfer.items.add(files[i]);
            }
            
            // Asignar los archivos al input
            inputArchivos.files = dataTransfer.files;
            
            // Disparar evento change para que el navegador actualice el input
            const event = new Event('change', { bubbles: true });
            inputArchivos.dispatchEvent(event);
        }
    }, false);
}

