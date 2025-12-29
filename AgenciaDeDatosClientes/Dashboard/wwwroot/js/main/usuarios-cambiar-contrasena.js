async function generarPassword() {
    try {
        const response = await fetch('?handler=GenerarPassword');
        const data = await response.json();
        const nuevaPasswordInput = document.getElementById('Modelo_NuevaPassword');
        const confirmarPasswordInput = document.getElementById('Modelo_ConfirmarPassword');
        
        if (nuevaPasswordInput) nuevaPasswordInput.value = data.password;
        if (confirmarPasswordInput) confirmarPasswordInput.value = data.password;
    } catch (error) {
        console.error('Error al generar contraseña:', error);
        alert('Error al generar la contraseña. Por favor, inténtalo manualmente.');
    }
}

