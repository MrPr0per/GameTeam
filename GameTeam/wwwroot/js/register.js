import {showError} from './errors.js';
import {loadSidebar} from './sidebar.js';
import {loadHeader} from './header.js';


document.addEventListener('DOMContentLoaded', async function () {
    addPasswordEqualityCheck(); // Проверка того, что пароли равны после каждого введенного символа
    addSwitchingForms(); // Переключение форм логина и регистрации
    addSwitchingPasswordVisibility();
    addLoginFormSubmissionProcessing(); // Обработка формы входа
    addRegisterFormSubmissionProcessing(); // Обработка формы регистрации
    await loadSidebar();
    await loadHeader();
});

// Проверка равенства паролей
function addPasswordEqualityCheck() {
    const delayBeforeError = 0;

    const passwordInputs = document.querySelectorAll('#register-form input.password-input');
    const password1 = passwordInputs[0];
    const password2 = passwordInputs[1];
    const errorElement = document.getElementById('registration-error-block');
    const errorText = document.getElementById('registration-error-text');
    let showErrorTimeout;

    password1.addEventListener('input', checkPasswords);
    password2.addEventListener('input', checkPasswords);

    function checkPasswords() {
        const pass1 = password1.value;
        const pass2 = password2.value;

        if (pass1 && pass2 && pass1 !== pass2) {
            showErrorTimeout = setTimeout(() => {
                errorText.innerText = 'Пароли не совпадают';
                errorElement.style.display = 'block';
            }, delayBeforeError);
        } else {
            errorElement.style.display = 'none';
            clearTimeout(showErrorTimeout);
        }
    }
}

// Переключение между формами регистрации и логина
function addSwitchingForms() {
    const tabRegister = document.getElementById('tab-register');
    const tabLogin = document.getElementById('tab-login');
    const registerForm = document.getElementById('register-form');
    const loginForm = document.getElementById('login-form');
    const formContainer = document.querySelector('.form-container');

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
}

// Переключение видимости пароля
function addSwitchingPasswordVisibility() {
    document.querySelectorAll('.toggle-password').forEach(icon => {
        icon.addEventListener('click', function () {
            const form = this.closest('form');
            const inputs = form.querySelectorAll('.password-input');

            inputs.forEach(input => {
                input.type = input.type === 'password' ? 'text' : 'password';
            });

            if (form.id === 'register-form') {
                form.querySelectorAll('.toggle-password').forEach(icon => {
                    icon.classList.toggle('active');
                });
            } else {
                this.classList.toggle('active');
            }
        });
    });
}

// Обработка формы регистрации
function addRegisterFormSubmissionProcessing() {
    const form = document.getElementById('register-form');
    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData(form);
        const formValues = Object.fromEntries(formData.entries());
        const data = handleRegisterDataSubmit(formValues); // Используем отдельную функцию для регистрации

        if (formValues.password !== formValues.confirmPassword) {
            showError('Пароли не совпадают');
            return;
        }

        // Дополнительная валидация или проверка, которая специфична для регистрации
        if (!data.username || !data.email || !formValues.password) {
            showError('Пожалуйста, заполните все поля');
            return;
        }

        try {
            const saltResponse = await fetch('api/auth/salt', {method: 'GET'});

            const salt = await saltResponse.text();

            data.password = await hashPassword(formValues.password, salt);

            const response = await postData('/api/auth/register', data);

            if (response.ok) {
                window.location.href = '/profile'; // Перенаправление после успешной регистрации
            } else {
                const result = await response.json();
                showError(result.message || 'Ошибка регистрации');
            }
        } catch (error) {
            showError('Зарегистрироваться не удалось', error);
        }
    });
}

// Обработка формы входа
function addLoginFormSubmissionProcessing() {
    const form = document.getElementById('login-form');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData(form);
        const formValues = Object.fromEntries(formData.entries());
        const data = handleLoginSubmit(formValues); // Используем отдельную функцию для логина

        if (!data.email || !formValues.password) {
            showError('Пожалуйста, введите email и пароль');
            return;
        }

        try {
            const response = await postData('/api/auth/login', data);

            const [salt, challenge] = await response.json();
            const passwordSalt = await hashPassword(formValues.password, salt);

            const passwordChallanged = await hashPassword(passwordSalt, challenge);

            const responseAuth =
                await fetch('/api/auth/loginpass/', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({password: passwordChallanged}),
                });

            if (responseAuth.ok) {
                window.location.href = '/profile'; // Перенаправление после успешного входа
            } else {
                const result = await responseAuth.json();
                showError('Войти не удалось', result);
            }
        } catch (error) {
            showError('Войти не удалось', error);
        }
    });
}

// Функция для подготовки данных для регистрации
function handleRegisterDataSubmit(formValues) {
    return {
        username: formValues.username,
        email: formValues.email,
    };
}

function handleRegisterSubmit(formValues) {
    return {
        username: formValues.username,
        email: formValues.email,
        password: formValues.password,
    };
}

// Функция для подготовки данных для входа
function handleLoginSubmit(formValues) {
    return {
        email: formValues.email,
    };
}

// Функция для отправки POST-запроса
async function postData(url, data) {
    return await fetch(url, {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify(data),
    });
}

async function hashPassword(password, salt) {
    // 1. Кодируем пароль в UTF-8 (как в C#)
    const passwordBytes = new TextEncoder().encode(password);

    // 2. Декодируем соль из Base64 (как Convert.FromBase64String в C#)
    const saltBytes = Uint8Array.from(atob(salt), c => c.charCodeAt(0));

    // 3. Объединяем массивы (аналогично Buffer.BlockCopy)
    const combined = new Uint8Array(passwordBytes.length + saltBytes.length);
    combined.set(passwordBytes);
    combined.set(saltBytes, passwordBytes.length);

    // 4. Хешируем (SHA-256)
    const hashBuffer = await crypto.subtle.digest('SHA-256', combined);

    // 5. Конвертируем в Base64 (как Convert.ToBase64String)
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    const hashBase64 = btoa(String.fromCharCode.apply(null, hashArray));

    return hashBase64;
}