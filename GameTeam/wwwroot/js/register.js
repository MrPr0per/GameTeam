document.addEventListener('DOMContentLoaded', function () {
	addPasswordEqualityCheck(); // проверка того, что пароли равны после каждого введенного символа
	addSwitchingForms(); // переключение форм логина и регистрации
	addSwitchingPasswordVisibility();
	addLoginFormSubmissionProcessing();
	addRegisterFormSubmissionProcessing();
});

function addPasswordEqualityCheck() {
	const delayBeforeError = 0; // todo: сейчас с задержкой выглядит не очень, с анимацией должно быть лучше

	// Получаем оба поля ввода пароля
	const passwordInputs = document.querySelectorAll('#register-form input.password-input');
	const password1 = passwordInputs[0];
	const password2 = passwordInputs[1];
	const errorElement = document.getElementById('registration-error-block');
	const errorText = document.getElementById('registration-error-text');
	let showErrorTimeout;

	// Добавляем обработчики событий с задержкой
	password1.addEventListener('input', checkPasswords);
	password2.addEventListener('input', checkPasswords);

	// Функция для проверки совпадения паролей
	function checkPasswords() {
		// Получаем значения из обоих полей
		const pass1 = password1.value;
		const pass2 = password2.value;
		// если не совпадают - добавляем таймер с появлением ошибки
		if (pass1 && pass2 && pass1 !== pass2) {
			showErrorTimeout = setTimeout(() => {
				errorText.innerText = 'Пароли не совпадают';
				errorElement.style.display = 'block';
			}, delayBeforeError);
		}
		// если совпадают - убираем и ошибку и таймер
		else {
			errorElement.style.display = 'none';
			clearTimeout(showErrorTimeout);
		}
	}
}

function addSwitchingForms() {
	const tabRegister = document.getElementById('tab-register');
	const tabLogin = document.getElementById('tab-login');
	const registerForm = document.getElementById('register-form');
	const loginForm = document.getElementById('login-form');
	const formContainer = document.querySelector('.form-container');
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
}

function addSwitchingPasswordVisibility() {
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
}

function addRegisterFormSubmissionProcessing() {
	setupFormSubmission({
		formId: 'register-form',
		errorBlockId: 'registration-error-block',
		errorTextId: 'registration-error-text',
		apiUrl: '/api/auth/register',
		getData: (formValues) => ({
			username: formValues.username,
			email: formValues.email,
			password: formValues.password,
			confirmPassword: formValues.password,
		}),
		defaultServerErrorMessage: 'Ошибка регистрации',
	});
}

function addLoginFormSubmissionProcessing() {
	setupFormSubmission({
		formId: 'login-form',
		errorBlockId: 'login-error-block',
		errorTextId: 'login-error-text',
		apiUrl: '/api/auth/login',
		getData: (formValues) => ({
			email: formValues.email,
			password: formValues.password,
			username: 'testuser',
		}), // TODO: убрать заглушку
		defaultServerErrorMessage: 'Ошибка входа',
	});
}

function setupFormSubmission({formId, errorBlockId, errorTextId, apiUrl, getData, defaultServerErrorMessage}) {
	const form = document.getElementById(formId);
	const errorBlock = document.getElementById(errorBlockId);
	const errorText = document.getElementById(errorTextId);

	form.addEventListener('submit', async function (e) {
		e.preventDefault();

		const formData = new FormData(form);
		const formValues = Object.fromEntries(formData.entries());
		const data = getData(formValues);

		try {
			const response = await fetch(apiUrl, {
				method: 'POST',
				headers: {'Content-Type': 'application/json'},
				body: JSON.stringify(data),
			});

			if (response.ok) {
				window.location.href = '/';
			} else {
				const result = await response.json();
				showError(result.message || defaultServerErrorMessage);
			}
		} catch {
			showError('Сервер недоступен');
		}
	});

	function showError(message) {
		errorBlock.style.display = 'block';
		errorText.innerText = message;
	}
}