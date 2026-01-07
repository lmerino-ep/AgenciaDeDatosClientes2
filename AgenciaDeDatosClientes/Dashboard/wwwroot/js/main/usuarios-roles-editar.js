function toggleTodosRoles(checkbox) {
    // Usar atributo data-js-rol para separar la lógica JS de los estilos CSS
    const checkboxes = document.querySelectorAll('[data-js-rol][type="checkbox"]');
    checkboxes.forEach(cb => cb.checked = checkbox.checked);
}

function confirmarCambios(event) {
    // Usar atributo data-js-rol para separar la lógica JS de los estilos CSS
    const checkboxes = document.querySelectorAll('[data-js-rol][type="checkbox"]:checked');
    const cantidad = checkboxes.length;

    if (!confirm(`¿Estás seguro de asignar ${cantidad} rol(es) a este usuario?\n\nEsta acción quedará registrada en la auditoría.`)) {
        event.preventDefault();
        return false;
    }

    return true;
}

