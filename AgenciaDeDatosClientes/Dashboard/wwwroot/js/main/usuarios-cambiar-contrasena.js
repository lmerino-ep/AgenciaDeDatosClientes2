async function generarPassword() {
    try {
        const response = await fetch('?handler=GenerarPassword');
        const data = await response.json();
        const nuevaPasswordInput = document.getElementById('modelo-nueva-password');
        const confirmarPasswordInput = document.getElementById('modelo-confirmar-password');
        
        if (nuevaPasswordInput) nuevaPasswordInput.value = data.password;
        if (confirmarPasswordInput) confirmarPasswordInput.value = data.password;
    } catch (error) {
        console.error('Error al generar contraseña:', error);
        alert('Error al generar la contraseña. Por favor, inténtalo manualmente.');
    }
}

function togglePasswordVisibility(button) {
    const targetId = button.getAttribute('data-toggle-password');
    const passwordInput = document.getElementById(targetId);
    const showIcon = button.querySelector('.eye-icon-show');
    const hideIcon = button.querySelector('.eye-icon-hide');
    
    if (passwordInput) {
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            showIcon.style.display = 'none';
            hideIcon.style.display = 'block';
        } else {
            passwordInput.type = 'password';
            showIcon.style.display = 'block';
            hideIcon.style.display = 'none';
        }
    }
}

document.addEventListener('DOMContentLoaded', function() {
    const btnGenerarPassword = document.querySelector('[data-js-generar-password]');
    if (btnGenerarPassword) {
        btnGenerarPassword.addEventListener('click', generarPassword);
    }
    
    // Configurar toggles de visibilidad de contraseña
    const passwordToggles = document.querySelectorAll('[data-toggle-password]');
    passwordToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            togglePasswordVisibility(this);
        });
    });
});

