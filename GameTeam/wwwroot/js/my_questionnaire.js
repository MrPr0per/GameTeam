let debugMode = true;

const daysOfWeek = ['Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота', 'Воскресенье'];

let isEditing = false;
let isPlaced = false;
let serverQuestionnaire = null; // то, что сохранено на сервере
let localQuestionnaire = { // локальные изменения, еще не запощенные
	title: '',
	description: '',
	games: [],
	goal: '',
	availabilities: [], 
	contacts: '',
};

document.addEventListener('DOMContentLoaded', function () {
	addEventListeners();
	// todo: пока считаем, что у одного пользователя одна анкета
	loadMyQuestionnaire()
		.then(() => {
			// Изначальное отображение анкеты и статуса
			displayQuestionnaire();
			updateStatusAndButtons();
		});
	loadAndRenderUserName();
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
	const questionnaireContent = document.getElementById('questionnaire-content');
	const statusText = document.getElementById('status-text');
	const gamesList = document.getElementById('questionnaire-games');
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
	function disableEditMode(saveChanges = true) {
		isEditing = false;
		if (saveChanges) {
			localQuestionnaire.title = document.getElementById('questionnaire-title').innerText.trim();
			localQuestionnaire.description = document.getElementById('questionnaire-description').innerText.trim();
			localQuestionnaire.games = Array.from(gamesList.querySelectorAll('li')).map(
				li => li.querySelector('span').textContent.trim()
			);

			localQuestionnaire.contacts = document.getElementById('questionnaire-contacts').innerText.trim();
		} else {
			localQuestionnaire = JSON.parse(JSON.stringify(serverQuestionnaire));
		}
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
		displayQuestionnaire();
		updateStatusAndButtons();
	}


	// "Разместить анкету" / "Скрыть анкету"
	placeButton.addEventListener('click', function () {
		if (isEditing) return;
		if (isPlaced) {
			hideMyQuestionnaire().then((success) => {
				if (!success) return;
				isPlaced = false;
				updateStatusAndButtons();
			});
		} else if (isFormValid()) {
			postMyQuestionnaire().then((success) => {
				if (success) {
					isPlaced = true;
					updateStatusAndButtons();
				}
			});
		} else {
			warningMessage.textContent = 'Заполните все обязательные поля';
		}
	});

	// "Редактировать анкету"
	editButton.addEventListener('click', function () {
		enableEditMode();
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

// Функция для отображения анкеты
function displayQuestionnaire(updateGamesOnly = false) {
	if (!updateGamesOnly) {
		document.getElementById('questionnaire-title').innerHTML = localQuestionnaire.title || '';
		document.getElementById('questionnaire-description').innerHTML = localQuestionnaire.description || '';
		document.getElementById('questionnaire-contacts').innerHTML = localQuestionnaire.contacts || '';

		// Обработка цели
		const goalContainer = document.getElementById('questionnaire-goal');
		if (isEditing) {
			const select = document.createElement('select');
			select.id = 'goal-select';
			const options = [
				'Пофаниться',
				'Поиграть в соревновательные режимы',
				'Расслабиться',
				'Поиграть в сюжетную игру',
				'Для стриминга',
				'Для заработка',
				'Тренировка',
				'Турнир'
			];
			options.forEach(option => {
				const opt = document.createElement('option');
				opt.value = option;
				opt.textContent = option;
				if (option === localQuestionnaire.goal) opt.selected = true;
				select.appendChild(opt);
			});
			goalContainer.innerHTML = '';
			goalContainer.appendChild(select);
			select.addEventListener('change', function () {
				localQuestionnaire.goal = this.value;
			});
		} else {
			goalContainer.innerHTML = `<p>${localQuestionnaire.goal || ''}</p>`;
		}

		// Обработка времени
		const timeContainer = document.getElementById('questionnaire-time');
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
			timeContainer.innerHTML = timeHtml;
			timeContainer.querySelectorAll('.time-input').forEach(input => {
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
				});
			});
		} else {
			let timeHtml = '';
			daysOfWeek.forEach((day, index) => {
				const availability = localQuestionnaire.availabilities.find(a => a.day === index);
				const timeStr = availability && availability.start && availability.end ? `${availability.start}-${availability.end}` : '-';
				timeHtml += `<div class="time-row"><span>${day}:</span> ${timeStr}</div>`;
			});
			timeContainer.innerHTML = timeHtml;
		}
	}
	const gamesList = document.getElementById('questionnaire-games');
	gamesList.innerHTML = '';
	localQuestionnaire.games.forEach((game, index) => {
		const li = document.createElement('li');
		li.className = 'game-item';
		if (isEditing) li.classList.add('editing');

		const contentDiv = document.createElement('div');
		contentDiv.className = 'game-item-content';

		const gameText = document.createElement('span');
		gameText.textContent = game;
		contentDiv.appendChild(gameText);

		if (isEditing) {
			const deleteButton = document.createElement('span');
			deleteButton.className = 'delete-game';
			deleteButton.textContent = '✕';
			deleteButton.addEventListener('click', () => {
				localQuestionnaire.games.splice(index, 1);
				displayQuestionnaire(true);
				updateStatusAndButtons();
			});
			contentDiv.appendChild(deleteButton);
		}
		li.appendChild(contentDiv);
		gamesList.appendChild(li);
	});
}

// Функция для проверки валидности формы
function isFormValid() {
	const title = localQuestionnaire.title.trim();
	const games = localQuestionnaire.games;
	const goal = localQuestionnaire.goal.trim();
	const contacts = localQuestionnaire.contacts.trim();
	// Время (availabilities) не является обязательным
	return title !== '' && games.length > 0 && goal !== '' && contacts !== '';
}

// Функция для обновления статуса и кнопок
function updateStatusAndButtons() {
	const placeButton = document.getElementById('place-button');
	const editButton = document.getElementById('edit-button');
	const editButtons = document.getElementById('edit-buttons');
	const statusButtons = document.getElementById('status-buttons');
	const statusText = document.getElementById('status-text');
	const addGameSection = document.getElementById('add-game-section');
	const warningMessage = document.getElementById('warning-message');

	// Обновляем текст статуса
	statusText.textContent = isPlaced ? 'Размещена' : 'Не размещена';
	const formValid = isFormValid();

	if (isEditing) {
		// Скрываем кнопки "Разместить" и "Редактировать"
		if (statusButtons) statusButtons.style.display = 'none';
		// Показываем кнопки "Сохранить", "Отмена", "Очистить"
		if (editButtons) editButtons.style.display = 'flex';
		if (addGameSection) addGameSection.style.display = 'block';
		document.querySelectorAll('.required').forEach(star => {
			star.style.display = 'inline';
		});
		if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля';
	} else {
		// Показываем кнопки "Разместить" и "Редактировать"
		if (statusButtons) statusButtons.style.display = 'flex';
		// Скрываем кнопки "Сохранить", "Отмена", "Очистить"
		if (editButtons) editButtons.style.display = 'none';
		if (addGameSection) addGameSection.style.display = 'none';
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

// todo: убрать дублирование с профилем
async function loadMyQuestionnaire() {
	const response = await fetch('data/selfapplications', { method: 'GET' });

	if (response.status != 200) {
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
			serverQuestionnaire = transformQuestionnaire(application[0]); //Пока что загружаем первую анкету
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

	// todo: пока делаем вид, что с сервака всегда прилетает пустота

	// await fetch('/data/questionnaire')
	// 	.then(r => {
	// 		if (r.ok) {
	// 			return r.json();
	// 		} else if (r.status === 401) {
	// 			window.location.href = '/register';
	// 		} else {
	// 			showServerError('Ошибка при загрузке анкеты с сервера', r);
	// 		}
	// 	})
	// 	.then(jsonOrNoneIfError => {
	// 		if (jsonOrNoneIfError === undefined) return;
	// 		try {
	// 			serverQuestionnaire = parseQuestionaire(jsonOrNoneIfError);
	// 			localQuestionnaire = structuredClone(serverQuestionnaire);
	// 		} catch (e) {
	// 			showServerError('Ошибка при обработке анкеты',
	// 				'(Данные прилетели не в том формате, в котором ожидалось)',
	// 				jsonOrNoneIfError);
	// 		}
	// 	});
}

function loadAndRenderUserName() {
	fetch('/data/profile')
		.then(r => {
			if (r.ok) {
				return r.json();
			} else if (r.status === 401) {
				window.location.href = '/register';
			} else {
				console.error('Ошибка при загрузке профиля', r);
			}
		})
		.then(json => {
			if (json !== undefined) {
				const name = json['Username'];
				document.querySelectorAll('.user-name').forEach(el => el.textContent = name);
			}
		})
		.catch(err => {
			console.error('Ошибка при получении имени пользователя:', err);
		});
}

async function getJsonForPostQuestionnaire() {
	let applicationId = -1;
	if (serverQuestionnaire.id === -1) {
		const response = await fetch('data/applicationid', { method: 'GET' });
		applicationId = await response.text();
	} else {
		applicationId = serverQuestionnaire.id;
	}
	return {
		id: applicationId,
		title: localQuestionnaire.title,
		description: localQuestionnaire.description,
		contacts: localQuestionnaire.contacts,
		purposeName: localQuestionnaire.goal, // Используем goal как purposeName
		availabilities: localQuestionnaire.availabilities
			.map(a => ({
				dayOfWeek: a.day,
				startTime: a.start ? `${a.start}:00+03:00` : '',
				endTime: a.end ? `${a.end}:00+03:00` : ''
			}))
			.filter(a => a.startTime && a.endTime),
		games: localQuestionnaire.games,
	};
	// // нужный формат
	// let test {
	// 	"id": -1,
	// 	"title": "Заявка на поиск командыыы",
	// 	"description": "Ищу команду для турниров по Dota 2",
	// 	"contacts": "player@example.com",
	// 	"purposeName": "test",
	// 	"availabilities": [
	// 	{
	// 		"dayOfWeek": 0,
	// 		"startTime": "18:00:00+03:00",
	// 		"endTime": "22:00:00+03:00"
	// 	},
	// 	{
	// 		"dayOfWeek": 5,
	// 		"startTime": "18:00:00+03:00",
	// 		"endTime": "22:00:00+03:00"
	// 	}
	// ],
	// 	"games": [
	// 	"Dota 2", "Valorant"
	// ]
	// }
	//
	// // сейчас:
	// let localQuestionnaire = { // локальные изменения, еще не запощенные
	// 	title: '',
	// 	description: '',
	// 	games: [],
	// 	goal: '',
	// 	time: '',
	// 	contacts: '',
	// };
}

async function postMyQuestionnaire() {
	const response = await fetch('/data/application', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
		},
		body: JSON.stringify(await getJsonForPostQuestionnaire()),
	});
	if (response.ok) return true;
	showServerError('Ошибка при отправке анкеты', response);
	return false;
}

function transformQuestionnaire(questionnaireData) {
	// Обрабатываем игры
	let games = [];
	if (Array.isArray(questionnaireData.Games)) {
		games = questionnaireData.Games
			.map(game => game?.Name || '')
			.filter(name => name);
	}

	// Маппинг для целей
	const goalMap = {
		1: 'Пофаниться',
		2: 'Поиграть в соревновательные режимы',
		3: 'Расслабиться',
		4: 'Поиграть в сюжетную игру',
		5: 'Для стриминга',
		6: 'Для заработка',
		7: 'Тренировка',
		8: 'Турнир'
	};

	// Получаем цель
	const goal = goalMap[questionnaireData.PurposeId] || '';

	let availabilities = [];
	if (Array.isArray(questionnaireData.Availabilities) && questionnaireData.Availabilities.length > 0) {
		availabilities = questionnaireData.Availabilities.map(avail => ({
			day: avail.day_of_week,
			start: avail.start_time ? avail.start_time.slice(0, 5) : '',
			end: avail.end_time ? avail.end_time.slice(0, 5) : ''
		}));
	}

	// Возвращаем преобразованный объект
	return {
		id: questionnaireData.Id,
		title: questionnaireData.Title,
		description: questionnaireData.Description,
		games: games,
		goal: goal,
		availabilities: availabilities,
		contacts: questionnaireData.Contacts
	};
}

async function hideMyQuestionnaire() {
	// todo
	return true; // Заглушка
}


// todo: убрать дублирование
function showServerError(message, ...debugInfo) {
	if (debugMode) {
		console.log(message, debugInfo);
	} else {
		console.log(message); // todo: сделать плашку об ошибке
	}
}