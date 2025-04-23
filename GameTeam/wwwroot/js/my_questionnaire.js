let debugMode = true;

let isEditing = false;
let isPlaced = false;
let serverQuestionnaire = null; // то, что сохранено на сервере
let localQuestionnaire = { // локальные изменения, еще не запощенные
	title: '',
	description: '',
	games: [],
	goal: '',
	time: '',
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
			localQuestionnaire.games = Array.from(gamesList.querySelectorAll('li')).map(li => li.querySelector('span').textContent.trim());
			localQuestionnaire.goal = document.getElementById('questionnaire-goal').innerText.trim();
			// localQuestionnaire.time = document.getElementById('questionnaire-time').innerText.trim();
			localQuestionnaire.contacts = document.getElementById('questionnaire-contacts').innerText.trim();
		} else {
			localQuestionnaire = serverQuestionnaire;
		}
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
		displayQuestionnaire();
		updateStatusAndButtons();
	}

	// "Разместить анкету" / "Скрыть анкету"
	placeButton.addEventListener('click', function () {
		if (isEditing) return;
		if (isPlaced) {
			hideMyQuestionnaire()
				.then((success) => {
					if (!success) return;
					isPlaced = false;
					updateStatusAndButtons();
				});
		} else if (isFormValid()) {
			postMyQuestionnaire()
				.then((success) => {
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
		if (!isEditing) enableEditMode();
	});

	// "Очистить"
	clearButton.addEventListener('click', function () {
		if (isEditing) {
			localQuestionnaire = {title: '', description: '', games: [], goal: '', time: '', contacts: ''};
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
		document.getElementById('questionnaire-goal').innerHTML = localQuestionnaire.goal || '';
		// document.getElementById('questionnaire-time').innerHTML = localQuestionnaire.time || '';
		document.getElementById('questionnaire-contacts').innerHTML = localQuestionnaire.contacts || '';
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

	statusText.textContent = isPlaced ? 'Размещена' : 'Не размещена';
	const formValid = isFormValid();
	if (isEditing) {
		statusButtons.style.display = 'none';
		editButtons.style.display = 'flex';
		addGameSection.style.display = 'block';
		document.querySelectorAll('.required').forEach(star => star.style.display = 'inline');
	} else {
		statusButtons.style.display = 'flex';
		editButtons.style.display = 'none';
		addGameSection.style.display = 'none';
		document.querySelectorAll('.required').forEach(star => star.style.display = 'none');
		if (isPlaced) {
			placeButton.textContent = 'Скрыть анкету';
			placeButton.classList.remove('filled-button');
			placeButton.classList.add('outline-button');
			editButton.style.display = 'none';
			document.getElementById('edit-message').style.display = 'block';
		} else {
			placeButton.textContent = 'Разместить анкету';
			placeButton.classList.remove('outline-button');
			placeButton.classList.add('filled-button');
			placeButton.disabled = !formValid;
			editButton.style.display = 'inline-block';
			document.getElementById('edit-message').style.display = 'none';
			warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля';
		}
	}
}


// todo: убрать дублирование с профилем
async function loadMyQuestionnaire() {
	const response = await fetch('data/selfapplications', { method: 'GET', });
	
	if (response.status != 200) {
		serverQuestionnaire = {
			title: '',
			description: '',
			games: [],
			goal: '',
			time: '',
			contacts: '',
		};
	} else {
		const application = await response.json();

		serverQuestionnaire = transformQuestionnaire(application[0]); //Пока что загружаем первую анкету
	}
	
	localQuestionnaire = structuredClone(serverQuestionnaire);

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
	const response = await fetch('data/applicationid', { method: 'GET', });
	const applicationId = await response.text();
	return {
		id: applicationId, // todo: убрать заглушку
		title: localQuestionnaire.title,
		description: localQuestionnaire.description,
		contacts: localQuestionnaire.contacts,
		purposeName: 'test', // todo: добавить в интерфейс
		availabilities: [], // todo: добавить в интерфейс
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
	// Обрабатываем игры (с защитой от ошибок)
	let games = [];
	if (Array.isArray(questionnaireData.Games)) {
		games = questionnaireData.Games
			.map(game => game?.Name || '')
			.filter(name => name);
	}

	// Маппинг для целей
	const goalMap = {
		1: 'test',
		2: 'Соревнование',
		3: 'Обучение'
		// Добавьте другие соответствия по необходимости
	};

	// Получаем цель
	const goal = goalMap[questionnaireData.PurposeId];

	let time = '';
	if (Array.isArray(questionnaireData.Availabilities) && questionnaireData.Availabilities.length > 0) {
		time = questionnaireData.Availabilities
			.map(avail => {
				const day = avail.day_of_week || 'День не указан';
				const start = avail.start_time || '--:--';
				const end = avail.end_time || '--:--';
				return `${day}: ${start} - ${end}`;
			})
			.join(', ');
	}

	// Возвращаем преобразованный объект
	return {
		title: questionnaireData.Title,
		description: questionnaireData.Description,
		games: games,
		goal: goal,
		time: time,
		contacts: questionnaireData.Contacts
	};
}


async function hideMyQuestionnaire() {
	// todo
}


// todo: убрать дублирование
function showServerError(message, ...debugInfo) {
	if (debugMode) {
		console.log(message, debugInfo);
	} else {
		console.log(message); // todo: сделать плашку об ошибке
	}
}

