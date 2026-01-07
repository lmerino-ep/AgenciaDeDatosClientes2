function mostrarModalToken(tokenId, token) {
    const modal = document.getElementById('modal-token');
    const tokenDisplay = document.getElementById('token-display');
    const tokenValue = document.getElementById('token-value');

    if (tokenDisplay) tokenDisplay.textContent = tokenId;
    if (tokenValue) tokenValue.textContent = token;
    if (modal) modal.classList.add('modal-visible');
}

function cerrarModal() {
    const modal = document.getElementById('modal-token');
    if (modal) modal.classList.remove('modal-visible');
}

function copiarToken() {
    const tokenValue = document.getElementById('token-value');
    if (!tokenValue) return;

    const tokenText = tokenValue.textContent;
    navigator.clipboard.writeText(tokenText).then(() => {
        alert('Token copiado al portapapeles');
    }).catch(err => {
        console.error('Error al copiar:', err);
    });
}

function confirmarRevocacion(tipo) {
    let mensaje = '';
    switch(tipo) {
        case 'token1':
            mensaje = '¿Estás seguro de revocar el RefreshToken1?\n\nEste token dejará de ser válido inmediatamente.';
            break;
        case 'token2':
            mensaje = '¿Estás seguro de revocar el RefreshToken2?\n\nEste token dejará de ser válido inmediatamente.';
            break;
        case 'todos':
            mensaje = '¿Estás seguro de revocar TODOS los tokens?\n\nEsto incluye:\n- RefreshToken1\n- RefreshToken2\n- AccessToken activo\n\nTodos los tokens dejarán de ser válidos inmediatamente.';
            break;
    }

    return confirm(mensaje);
}

// Cerrar modal al hacer clic fuera
window.onclick = function(event) {
    const modal = document.getElementById('modal-token');
    if (modal && event.target === modal) {
        cerrarModal();
    }
}

