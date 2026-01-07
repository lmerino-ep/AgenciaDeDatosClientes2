/**
 * Script para la página de edición de usuarios
 * Maneja el modal de confirmación de eliminación definitiva y el dropdown de usuarios hereditarios
 */

function obtenerValorDropdown() {
    const dropdown = document.querySelector('#formEliminarDefinitivamente .dropdownfiltrable .caja');
    if (!dropdown) return '';
    
    const textoSeleccionado = dropdown.value.trim();
    if (!textoSeleccionado) return '';
    
    // Buscar el elemento span que corresponde al texto seleccionado
    const elementos = document.querySelectorAll('#formEliminarDefinitivamente .dropdownfiltrable .elementos span');
    
    for (let i = 0; i < elementos.length; i++) {
        const textoElemento = elementos[i].textContent.trim() || elementos[i].innerHTML.trim();
        if (textoElemento === textoSeleccionado) {
            const valorId = elementos[i].getAttribute('data-valor');
            if (valorId !== null && valorId !== undefined && valorId !== '') {
                return valorId;
            }
        }
    }
    
    return '';
}

function confirmarEliminacionDefinitiva() {
    const idUsuarioHereditario = obtenerValorDropdown();
    
    if (!idUsuarioHereditario || idUsuarioHereditario === '') {
        alert('Debe seleccionar un usuario que herede la información antes de eliminar.');
        return false;
    }
    
    // Establecer el valor en el hidden input
    const hiddenInput = document.getElementById('idUsuarioHereditario');
    if (hiddenInput) {
        hiddenInput.value = idUsuarioHereditario;
    }
    
    // Mostrar modal de confirmación
    const modal = document.getElementById('modalConfirmarEliminacion');
    if (modal) {
        modal.style.display = 'flex';
    }
    
    return false;
}

function confirmarEliminacion() {
    const hiddenInput = document.getElementById('idUsuarioHereditario');
    const form = document.getElementById('formEliminarDefinitivamente');
    
    if (hiddenInput && form && hiddenInput.value && hiddenInput.value.trim() !== '') {
        cerrarModal();
        form.submit();
    } else {
        alert('Error: No se ha seleccionado un usuario hereditario.');
    }
}

function cerrarModal() {
    const modal = document.getElementById('modalConfirmarEliminacion');
    if (modal) {
        modal.style.display = 'none';
    }
}

// Configurar el formulario y el modal
document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('formEliminarDefinitivamente');
    if (!form) return;
    
    // Configurar el formulario para prevenir el envío y mostrar el modal
    form.addEventListener('submit', function(e) {
        e.preventDefault();
        confirmarEliminacionDefinitiva();
    });
    
    // Configurar botones del modal
    const btnConfirmar = document.getElementById('btnConfirmarEliminacion');
    const btnCancelar = document.getElementById('btnCancelarEliminacion');
    const modal = document.getElementById('modalConfirmarEliminacion');
    
    if (btnConfirmar) {
        btnConfirmar.addEventListener('click', confirmarEliminacion);
    }
    
    if (btnCancelar) {
        btnCancelar.addEventListener('click', cerrarModal);
    }
    
    // Cerrar al hacer clic fuera del modal
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                cerrarModal();
            }
        });
    }
});