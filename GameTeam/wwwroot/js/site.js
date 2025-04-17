document.addEventListener('DOMContentLoaded', function () {
    const tabRegister = document.getElementById('tab-register');
    const tabLogin = document.getElementById('tab-login');
    const registerForm = document.getElementById('register-form');
    const loginForm = document.getElementById('login-form');
    const formContainer = document.querySelector('.form-container');

    // Инициализация валидации для всех форм
    document.querySelectorAll('.auth-form').forEach(form => {
        form.addEventListener('submit', function(e) {
            let isValid = true;
            //ТУТ НУЖНО ДОПИСАТЬ ВСЯКИЕ ПРОВЕРКИ ПОЛЕЙ


            if (!isValid) e.preventDefault();
        });
    });

    // Переключение форм
    tabLogin.addEventListener('click', () => {
        tabLogin.classList.add('active');
        tabRegister.classList.remove('active');
        loginForm.style.display = 'flex';
        registerForm.style.display = 'none';
        formContainer.classList.remove('register-mode');
    });

    tabRegister.addEventListener('click', () => {
        tabRegister.classList.add('active');
        tabLogin.classList.remove('active');
        registerForm.style.display = 'flex';
        loginForm.style.display = 'none';
        formContainer.classList.add('register-mode');
    });

    // Обработчик переключения видимости паролей
    document.querySelectorAll('.toggle-password').forEach(icon => {
        icon.addEventListener('click', function () {
            const form = this.closest('form');
            const inputs = form.querySelectorAll('.password-input');
            
            // Переключаем видимость полей
            inputs.forEach(input => {
                input.type = input.type === 'password' ? 'text' : 'password';
            });
            
            // Синхронизация иконок в форме регистрации
            if (form.id === 'register-form') {
                form.querySelectorAll('.toggle-password').forEach(icon => {
                    icon.classList.toggle('active');
                });
            } else {
                this.classList.toggle('active');
            }
        });
    });
});