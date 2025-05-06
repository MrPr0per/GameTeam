import { buttonsActivator } from './buttonsActivator.js';

let debugMode = true;

const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

let isEditing = false;
let isPlaced = false;
let serverQuestionnaire = null;
let localQuestionnaire = {
	title: '',
	description: '',
	games: [],
	goal: '',
	availabilities: [],
	contacts: '',
};

function loadComponents() {
	// Загрузка sidebar
	fetch('../pages/Sidebar.html')
		.then(response => response.text())
		.then(html => {
			document.getElementById('sidebar-placeholder').innerHTML = html;
		})
		.catch(err => {
			console.error('Ошибка загрузки sidebar:', err);
		});

	// Загрузка header и его логики
	fetch('../pages/Header.html')
		.then(response => response.text())
		.then(html => {
			document.getElementById('header-placeholder').innerHTML = html;
			const script = document.createElement('script');
			script.src = '../js/header.js';
			document.body.appendChild(script);
		})
		.catch(err => {
			console.error('Ошибка загрузки header:', err);
		});
}

document.addEventListener('DOMContentLoaded', function () {
	// Загружаем компоненты
	loadComponents();

	// Инициализация анкеты
	addEventListeners();
	loadMyQuestionnaire()
		.then(() => {
			displayQuestionnaire();
			updateStatusAndButtons();
		});
});

function addEventListeners() {
	const placeButton = document.getElementById('place-button');
	const editButton = document.getElementById('edit-button');
	const clearButton = document.getElementById('clear-button');
	const cancelButton = document.getElementById('cancel-button');
	const saveButton = document.getElementById('save-button');
	const addGameButton = document.getElementById('add-game-button');
	const newGameInput = document.getElementById('new-game-input');
	const editButtons = document.getElementById('edit-buttons');
	const statusButtons = document.getElementById('status-buttons');
	const questionnaireContent = document.querySelector('.questionnaire-content');
	const statusText = document.getElementById('status-text');
	const gamesList = document.querySelector('.games-list');
	const addGameSection = document.getElementById('add-game-section');
	const warningMessage = document.getElementById('warning-message');

	// Включение режима редактирования
	function enableEditMode() {
		if (isEditing) return;
		isEditing = true;
		serverQuestionnaire = JSON.parse(JSON.stringify(localQuestionnaire));
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => {
			el.contentEditable = true;
			el.innerHTML = el.innerText.trim();
		});
		displayQuestionnaire();
		updateStatusAndButtons();
	}

	// Выключение режима редактирования
	async function disableEditMode(saveChanges = true) {
		isEditing = false;
		if (saveChanges) {
			localQuestionnaire.title = document.querySelector('.questionnaire-title').innerText.trim();
			localQuestionnaire.description = document.querySelector('.questionnaire-description').innerText.trim();
			localQuestionnaire.games = Array.from(gamesList.querySelectorAll('.game-name')).map(span => span.textContent.trim());
			localQuestionnaire.contacts = document.querySelector('.questionnaire-contacts').innerText.trim();

			if (isFormValid()) {
				const success = await postMyQuestionnaire();
				if (success) {
					serverQuestionnaire = JSON.parse(JSON.stringify(localQuestionnaire));
				} else {
					localQuestionnaire = JSON.parse(JSON.stringify(serverQuestionnaire));
				}
			} else {
				warningMessage.textContent = 'Заполните все обязательные поля';
				localQuestionnaire = JSON.parse(JSON.stringify(serverQuestionnaire));
			}
		} else {
			localQuestionnaire = JSON.parse(JSON.stringify(serverQuestionnaire));
		}
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
		displayQuestionnaire();
		updateStatusAndButtons();
	}

	// "Редактировать анкету"
	editButton.addEventListener('click', enableEditMode);

	// "Разместить анкету" / "Скрыть анкету"
	placeButton.addEventListener('click', function () {
		if (isEditing) return;
		if (isPlaced) {
			hideMyQuestionnaire().then(success => {
				if (!success) return;
				isPlaced = false;
				updateStatusAndButtons();
			});
		} else {
			if (!isFormValid()) {
				warningMessage.textContent = 'Заполните все обязательные поля';
				return;
			}
			hideMyQuestionnaire().then(success => {
				if (!success) return;
				isPlaced = true;
				updateStatusAndButtons();
			});
		}
	});

	// "Очистить"
	clearButton.addEventListener('click', function () {
		if (isEditing) {
			localQuestionnaire = { title: '', description: '', games: [], goal: '', availabilities: [], contacts: '' };
			displayQuestionnaire();
			updateStatusAndButtons();
		}
	});

	// "Отмена"
	cancelButton.addEventListener('click', function () {
		if (isEditing) disableEditMode(false);
	});

	// "Сохранить изменения"
	saveButton.addEventListener('click', function () {
		if (isEditing) disableEditMode(true);
	});

	// "Добавить игру"
	addGameButton.addEventListener('click', function () {
		const newGame = newGameInput.value.trim();
		if (newGame) {
			localQuestionnaire.games.push(newGame);
			displayQuestionnaire(true);
			newGameInput.value = '';
			updateStatusAndButtons();
		}
	});
}

function displayQuestionnaire(updateGamesOnly = false) {
	const questionnaireContent = document.querySelector('.questionnaire-content');

	let contactsSection = questionnaireContent.querySelector('.contacts-section');
	if (!contactsSection) {
		contactsSection = document.createElement('div');
		contactsSection.className = 'questionnaire-section contacts-section';
		contactsSection.innerHTML = `
            <label>Контакты:<span class="required" style="display: none;"></span></label>
            <p class="questionnaire-contacts" contenteditable="false" data-placeholder="Введите ваши контакты (например, Telegram)"></p>
        `;
		questionnaireContent.insertBefore(contactsSection, questionnaireContent.querySelector('.bottom-section'));
	}

	if (!updateGamesOnly) {
		const titleElement = questionnaireContent.querySelector('.questionnaire-title');
		titleElement.innerHTML = localQuestionnaire.title || '';
		const descriptionElement = questionnaireContent.querySelector('.questionnaire-description');
		descriptionElement.innerHTML = localQuestionnaire.description || '';
		const contactsElement = questionnaireContent.querySelector('.questionnaire-contacts');
		contactsElement.innerHTML = localQuestionnaire.contacts || '';

		const purposeContainer = questionnaireContent.querySelector('.questionnaire-purpose');
		if (isEditing) {
			const select = document.createElement('select');
			select.id = 'goal-select';
			const options = [
				'Пофаниться',
				'Посоревноваться',
				'Расслабиться',
				'Поиграть в сюжетную игру',
				'Для стриминга',
				'Для заработка',
				'Тренировка',
				'Турнир',
			];
			options.forEach(option => {
				const opt = document.createElement('option');
				opt.value = option;
				opt.textContent = option;
				if (option === localQuestionnaire.goal) opt.selected = true;
				select.appendChild(opt);
			});
			purposeContainer.innerHTML = '';
			purposeContainer.appendChild(select);
			select.addEventListener('change', function () {
				localQuestionnaire.goal = this.value;
			});
		} else {
			purposeContainer.innerHTML = `<p>${localQuestionnaire.goal || ''}</p>`;
		}

		const availabilityContainer = questionnaireContent.querySelector('.availability');
		availabilityContainer.innerHTML = '';
		if (isEditing) {
			let timeHtml = '';
			daysOfWeek.forEach((day, index) => {
				const availability = localQuestionnaire.availabilities.find(a => a.day === index) || { start: '', end: '' };
				timeHtml += `
                    <div class="time-row">
                        <span>${day}:</span>
                        <input type="time" class="time-input" data-day="${index}" data-type="start" value="${availability.start || ''}">
                        <span>-</span>
                        <input type="time" class="time-input" data-day="${index}" data-type="end" value="${availability.end || ''}">
                    </div>
                `;
			});
			availabilityContainer.innerHTML = timeHtml;
			availabilityContainer.querySelectorAll('.time-input').forEach(input => {
				input.addEventListener('change', function () {
					const day = parseInt(this.dataset.day);
					const type = this.dataset.type;
					const value = this.value.trim();
					let availability = localQuestionnaire.availabilities.find(a => a.day === day);
					if (!availability) {
						availability = { day: day, start: '', end: '' };
						localQuestionnaire.availabilities.push(availability);
					}
					availability[type] = value;
					localQuestionnaire.availabilities = localQuestionnaire.availabilities.filter(a => a.start || a.end);
				});
			});
		} else {
			let timeHtml = '';
			daysOfWeek.forEach((day, index) => {
				const availability = localQuestionnaire.availabilities.find(a => a.day === index);
				const timeStr = availability && availability.start && availability.end ? `${availability.start} - ${availability.end}` : '—';
				timeHtml += `<div class="time-row"><span>${day}:</span> ${timeStr}</div>`;
			});
			availabilityContainer.innerHTML = timeHtml;
		}
	}
	const gamesList = questionnaireContent.querySelector('.games-list');
	gamesList.innerHTML = '';
	localQuestionnaire.games.forEach((game, index) => {
		const li = document.createElement('li');
		li.className = 'game-item';
		if (isEditing) li.classList.add('editing');
		const gameName = document.createElement('span');
		gameName.className = 'game-name';
		gameName.textContent = game;
		li.appendChild(gameName);
		if (isEditing) {
			const deleteButton = document.createElement('span');
			deleteButton.className = 'delete-game';
			deleteButton.textContent = '✕';
			deleteButton.addEventListener('click', () => {
				localQuestionnaire.games.splice(index, 1);
				displayQuestionnaire(true);
				updateStatusAndButtons();
			});
			li.appendChild(deleteButton);
		}
		gamesList.appendChild(li);
	});
}

function isFormValid() {
	const title = localQuestionnaire.title.trim();
	const games = localQuestionnaire.games;
	const goal = localQuestionnaire.goal.trim();
	const contacts = localQuestionnaire.contacts.trim();
	return title !== '' && games.length > 0 && goal !== '' && contacts !== '';
}

function updateStatusAndButtons() {
	const placeButton = document.getElementById('place-button');
	const editButton = document.getElementById('edit-button');
	const editButtons = document.getElementById('edit-buttons');
	const statusButtons = document.getElementById('status-buttons');
	const statusText = document.getElementById('status-text');
	const addGameSection = document.getElementById('add-game-section');
	const warningMessage = document.getElementById('warning-message');

	statusText.textContent = isPlaced ? 'Размещена' : 'Не размещена';
	const formValid = isFormValid();

	if (isEditing) {
		if (statusButtons) statusButtons.style.display = 'none';
		if (editButtons) editButtons.style.display = 'flex';
		if (addGameSection) addGameSection.classList.add('active');
		document.querySelectorAll('.required').forEach(star => {
			star.style.display = 'inline';
		});
		if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля';
	} else {
		if (statusButtons) statusButtons.style.display = 'flex';
		if (editButtons) editButtons.style.display = 'none';
		if (addGameSection) addGameSection.classList.remove('active');
		document.querySelectorAll('.required').forEach(star => {
			star.style.display = 'none';
		});

		if (isPlaced) {
			placeButton.textContent = 'Скрыть анкету';
			placeButton.classList.remove('filled-button');
			placeButton.classList.add('outline-button');
			editButton.style.display = 'none';
			document.getElementById('edit-message').style.display = 'block';
			if (warningMessage) warningMessage.textContent = '';
		} else {
			placeButton.textContent = 'Разместить анкету';
			placeButton.classList.remove('outline-button');
			placeButton.classList.add('filled-button');
			placeButton.disabled = !formValid;
			editButton.style.display = 'inline-block';
			document.getElementById('edit-message').style.display = 'none';
			if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля';
		}
	}
}

async function loadMyQuestionnaire() {
	const response = await fetch('/data/selfapplications', { method: 'GET' });

	if (!response.ok) {
		serverQuestionnaire = {
			id: -1,
			title: '',
			description: '',
			games: [],
			goal: '',
			availabilities: [],
			contacts: '',
		};
	} else {
		const application = await response.json();
		if (application[0]) {
			serverQuestionnaire = transformQuestionnaire(application[0]);
			isPlaced = !application[0].IsHidden;
		} else {
			serverQuestionnaire = {
				id: -1,
				title: '',
				description: '',
				games: [],
				goal: '',
				availabilities: [],
				contacts: '',
			};
		}
	}

	localQuestionnaire = JSON.parse(JSON.stringify(serverQuestionnaire));
}

async function getJsonForPostQuestionnaire() {
	let applicationId = -1;
	if (serverQuestionnaire.id === -1) {
		const response = await fetch('data/applicationid', { method: 'GET' });
		applicationId = await response.text();
	} else {
		applicationId = serverQuestionnaire.id;
	}
	const availabilities = localQuestionnaire.availabilities
		.filter(a => a.start && a.end)
		.map(a => ({
			dayOfWeek: a.day,
			startTime: a.start ? `${a.start}:00+00:00` : '',
			endTime: a.end ? `${a.end}:00+00:00` : ''
		}));
	return {
		id: applicationId,
		title: localQuestionnaire.title,
		description: localQuestionnaire.description,
		contacts: localQuestionnaire.contacts,
		purposeName: localQuestionnaire.goal,
		availabilities: availabilities,
		games: localQuestionnaire.games,
	};
}

async function postMyQuestionnaire() {
	const jsonData = await getJsonForPostQuestionnaire();
	console.log('Sending to server:', jsonData);
	const response = await fetch('/data/application', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
		},
		body: JSON.stringify(jsonData),
	});
	if (response.ok) {
		serverQuestionnaire = JSON.parse(JSON.stringify(localQuestionnaire));
		serverQuestionnaire.id = jsonData.id;
		return true;
	}
	showServerError('Ошибка при отправке анкеты', response);
	return false;
}

function transformQuestionnaire(questionnaireData) {
	let games = [];
	if (Array.isArray(questionnaireData.Games)) {
		games = questionnaireData.Games
			.map(game => game?.Name || '')
			.filter(name => name);
	}

	const goalMap = {
		2: 'Пофаниться',
		3: 'Посоревноваться',
		4: 'Расслабиться',
		5: 'Поиграть в сюжетную игру',
		6: 'Для стриминга',
		1: 'Для заработка',
		7: 'Тренировка',
		8: 'Турнир',
	};

	const goal = goalMap[questionnaireData.PurposeId] || '';

	let availabilities = [];
	if (Array.isArray(questionnaireData.Availabilities)) {
		availabilities = questionnaireData.Availabilities.map(avail => {
			const startHour = avail.StartTime.Hour.toString().padStart(2, '0');
			const startMinute = avail.StartTime.Minute.toString().padStart(2, '0');
			const endHour = avail.EndTime.Hour.toString().padStart(2, '0');
			const endMinute = avail.EndTime.Minute.toString().padStart(2, '0');
			return {
				day: avail.DayOfWeek,
				start: `${startHour}:${startMinute}`,
				end: `${endHour}:${endMinute}`
			};
		});
	}

	return {
		id: questionnaireData.Id,
		title: questionnaireData.Title || '',
		description: questionnaireData.Description || '',
		games: games,
		goal: goal,
		availabilities: availabilities,
		contacts: questionnaireData.Contacts || ''
	};
}

async function hideMyQuestionnaire() {
	if (!serverQuestionnaire || serverQuestionnaire.id === -1) {
		showServerError('Анкета не найдена');
		return false;
	}

	const action = isPlaced ? 'hide' : 'show';
	const url = `/data/${action}/${serverQuestionnaire.id}`;

	try {
		const response = await fetch(url, {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json',
			},
		});

		if (response.ok) {
			return true;
		} else {
			showServerError(`Ошибка при ${action === 'hide' ? 'скрытии' : 'показе'} анкеты`, response);
			return false;
		}
	} catch (error) {
		showServerError(`Ошибка при ${action === 'hide' ? 'скрытии' : 'показе'} анкеты`, error);
		return false;
	}
}

function showServerError(message, ...debugInfo) {
	if (debugMode) {
		console.log(message, debugInfo);
	} else {
		console.log(message);
	}
}