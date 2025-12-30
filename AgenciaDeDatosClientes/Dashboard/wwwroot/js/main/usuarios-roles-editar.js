function toggleTodosRoles(checkbox) {
    const checkboxes = document.querySelectorAll('.rol-checkbox-input[type="checkbox"]');
    checkboxes.forEach(cb => cb.checked = checkbox.checked);
}

function confirmarCambios(event) {
    const checkboxes = document.querySelectorAll('.rol-checkbox-input[type="checkbox"]:checked');
    const cantidad = checkboxes.length;

    if (!confirm(`¿Estás seguro de asignar ${cantidad} rol(es) a este usuario?\n\nEsta acción quedará registrada en la auditoría.`)) {
        event.preventDefault();
        return false;
    }

    return true;
}

