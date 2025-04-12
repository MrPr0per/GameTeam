document.addEventListener('DOMContentLoaded', function () {
    const tabRegister = document.getElementById('tab-register');
    const tabLogin = document.getElementById('tab-login');
    const registerForm = document.getElementById('register-form');
    const loginForm = document.getElementById('login-form');
    const formContainer = document.querySelector('.form-container');

    addPasswordEqualityCheck();


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

function addPasswordEqualityCheck() {
    const delayBeforeError = 1000;

    // Получаем оба поля ввода пароля
    const passwordInputs = document.querySelectorAll('#register-form input.password-input');
    const password1 = passwordInputs[0];
    const password2 = passwordInputs[1];
    const errorElement = document.getElementById('password-error');

    // Добавляем обработчики событий с задержкой
    password1.addEventListener('input', checkPasswords);
    password2.addEventListener('input', checkPasswords);

    // Функция для проверки совпадения паролей
    function checkPasswords() {
        // Получаем значения из обоих полей
        const pass1 = password1.value;
        const pass2 = password2.value;
        console.log(pass1, pass2)
        if (pass1 && pass2 && pass1 !== pass2) {
            setTimeout(() => errorElement.style.display = 'none', delayBeforeError)
        } else {
            errorElement.style.display = 'block'; // отображаем
        }
    }
}

// document.querySelector('#register-form').addEventListener()
// // Инициализация валидации для всех форм
// document.querySelectorAll('.auth-form').forEach(form => {
//     form.addEventListener('submit', function (e) {
//         let isValid = true;
//         // todo:
//         // - вход
//         //  - обработка ответа, редирект или ошибка
//         // - регистрация
//         //  - обработка ответа, редирект или ошибка
//         //  - равенство паролей
//
//
//         if (!isValid) e.preventDefault();
//     });
// });
