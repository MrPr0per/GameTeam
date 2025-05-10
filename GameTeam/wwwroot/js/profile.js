import {buttonsActivator} from '../js/buttonsActivator.js';
import {loadHeader} from '../js/header.js';
import {showServerError} from './errors.js';

let realProfileInfo = {
	name: '',
	email: '',
	description: '',
	availabilitiesByDay: Array.from({length: 7}, () => []),
	games: [],
};
let displayedProfileInfo = structuredClone(realProfileInfo);


document.addEventListener('DOMContentLoaded', function () {
	// Загружаем компоненты
	loadComponents();

	addEventListeners();
	// Определяем, на себя мы смотрим или не на себя
	const usernameFromUrl = tryGetUsernameFromUrl();
	const isItMyselfProfile = usernameFromUrl === null;
	if (isItMyselfProfile) {
		loadProfileInfo().then(() => {
			document.getElementById('edit-button').disabled = false;
			updateDisplayOfProfileInfo(false);
		});
	} else {
		document.getElementById('edit-button').style.display = 'none';
		document.getElementById('logout-button').style.display = 'none';
		loadProfileInfo(usernameFromUrl).then(() => updateDisplayOfProfileInfo(false));
	}
});

function tryGetUsernameFromUrl() {
	const decodedPath = getDecodedPathname();
	const pathParts = decodedPath.split('/').filter(Boolean);

	// Проверяем структуру URL: ["profile" или "Profile.html", "{username}"]
	if (pathParts.length === 2 && pathParts[0].toLowerCase().includes('profile')) {
		return pathParts[1];
	}
	return null;

	function getDecodedPathname() {
		try {
			return decodeURIComponent(window.location.pathname);
		} catch (e) {
			console.error('Error decoding URL:', e);
			return window.location.pathname; // Возвращаем как есть в случае ошибки
		}
	}
}

async function loadSidebar() {
	const response = await fetch('../pages/Sidebar.html');
	const sidebarHtml = await response.text();
	document.getElementById('sidebar-placeholder').innerHTML = sidebarHtml;
}

async function loadComponents() {
	await loadSidebar();
	await loadHeader();
}


function addEventListeners() {
	const tabs = document.querySelectorAll('.tab-link');
	const panes = document.querySelectorAll('.tab-pane');
	const textarea = document.getElementById('description');
	const editButton = document.getElementById('edit-button');
	const logoutButton = document.getElementById('logout-button');
	const saveCancelButtons = document.getElementById('save-cancel-buttons');
	const saveButton = document.getElementById('save-button');
	const cancelButton = document.getElementById('cancel-button');
	const addGameButton = document.getElementById('add-game-button');
	const newGameInput = document.getElementById('new-game-input');

	// Переключение вкладок
	tabs.forEach(tab => {
		tab.addEventListener('click', function () {
			tabs.forEach(t => {
				t.classList.remove('active');
				const img = t.querySelector('img');
				if (t.getAttribute('data-tab') === 'about') {
					img.src = '../img/about.svg';
				} else if (t.getAttribute('data-tab') === 'hobbies') {
					img.src = '../img/plays.svg';
				}
			});

			this.classList.add('active');
			const img = this.querySelector('img');
			if (this.getAttribute('data-tab') === 'about') {
				img.src = '../img/about-active.svg';
			} else if (this.getAttribute('data-tab') === 'hobbies') {
				img.src = '../img/plays-active.svg';
			}

			panes.forEach(pane => pane.classList.remove('active'));
			const tabId = this.getAttribute('data-tab');
			document.getElementById(tabId).classList.add('active');
		});
	});

	// Динамическое изменение высоты штучки с описанием
	textarea.addEventListener('input', function () {
		this.style.height = 'auto';
		this.style.height = this.scrollHeight + 'px';
	});
	textarea.style.height = textarea.scrollHeight + 'px';

	// Кнопка "Редактировать профиль"
	editButton.addEventListener('click', function () {
		editButton.textContent = 'Редактирование';
		editButton.disabled = true;
		saveCancelButtons.style.display = 'flex';
		textarea.removeAttribute('readonly');
		updateDisplayOfProfileInfo(true);
		document.getElementById('add-game-section').style.display = 'flex';
	});

	// Кнопка "Выйти"
	logoutButton.addEventListener('click', function () {
		buttonsActivator.setPending(logoutButton);
		fetch('/api/auth/logout', {method: 'POST'})
			.then(r => {
				if (r.ok) {
					window.location.href = '/'; // Редиректимся на главную
				} else {
					showServerError('Выйти не удалось');
				}
			})
			.finally(() => buttonsActivator.resetPending(logoutButton));
	});

	// Кнопка "Готово"
	saveButton.addEventListener('click', function () {
		buttonsActivator.setPending(saveButton);
		saveChanges()
			.then(success => {
				buttonsActivator.resetPending(saveButton);
				if (success) {
					editButton.textContent = 'Редактировать профиль';
					editButton.disabled = false;
					saveCancelButtons.style.display = 'none';
					textarea.setAttribute('readonly', true);
					document.getElementById('add-game-section').style.display = 'none';
					updateDisplayOfProfileInfo(false);
				} else showServerError('Не удалось сохранить изменения');
			});
	});

	// Кнопка "Отмена"
	cancelButton.addEventListener('click', function () {
		editButton.textContent = 'Редактировать профиль';
		editButton.disabled = false;
		saveCancelButtons.style.display = 'none';
		textarea.setAttribute('readonly', true);
		document.getElementById('add-game-section').style.display = 'none';
		displayedProfileInfo = structuredClone(realProfileInfo);
		updateDisplayOfProfileInfo(false);
	});

	// Добавление новой игры
	addGameButton.addEventListener('click', function () {
		const newGame = newGameInput.value.trim();
		if (newGame) {
			displayedProfileInfo.games.push(newGame);
			renderGames(true);
			newGameInput.value = '';
		}
	});
}

function updateDisplayOfProfileInfo(isEditing) {
	renderPersonalInfo();
	renderDescription();
	renderFreeTime();
	renderGames(isEditing);
}

function renderPersonalInfo() {
	document.getElementById('user-name-main').textContent = displayedProfileInfo.name;
	document.getElementById('user-email').textContent = displayedProfileInfo.email;
}

function renderDescription() {
	const descriptionField = document.getElementById('description');
	descriptionField.value = displayedProfileInfo.description;
}

function renderFreeTime() {
	const formatter = new Intl.DateTimeFormat('ru-RU', {
		timeZone: 'UTC',
		hour: '2-digit',
		minute: '2-digit',
	});
	for (let i = 0; i < 7; i++) {
		const dayTimeSlot = document.querySelector(`#free-time > ul > li:nth-child(${i + 1}) > span`);
		dayTimeSlot.textContent = displayedProfileInfo.availabilitiesByDay[i]
			.map((segment) => `${formatter.format(segment.startTime)} - ${formatter.format(segment.endTime)}`)
			.join(', ');
	}
}

function renderGames(isEditing) {
	const gamesList = document.getElementById('games-list');
	gamesList.innerHTML = '';
	displayedProfileInfo.games.forEach((game, index) => {
		const li = document.createElement('li');
		const gameSpan = document.createElement('span');
		gameSpan.className = 'game-name';
		gameSpan.textContent = game;
		li.appendChild(gameSpan);
		if (isEditing) {
			const removeButton = document.createElement('span');
			removeButton.textContent = '×';
			removeButton.className = 'remove-game';
			removeButton.addEventListener('click', () => {
				displayedProfileInfo.games.splice(index, 1);
				renderGames(isEditing);
			});
			li.appendChild(removeButton);
		}
		gamesList.appendChild(li);
	});
}

function show404(username) {
	const profileContainer = document.getElementById('profileContainer');
	const safeUsername = document.createTextNode(`Пользователь "${username}" не найден.`);
	const errorDiv = document.createElement('div');
	errorDiv.className = 'error-message';
	errorDiv.appendChild(safeUsername);
	profileContainer.innerHTML = '';
	profileContainer.appendChild(errorDiv);
}

async function loadProfileInfo(username) {
	const url = username === undefined ? '/data/profile' : `/data/profile/${username}`;
	const response = await fetch(url);
	if (response.status === 401) {
		window.location.href = '/register';
		return;
	}
	if (response.status === 404) {
		show404(username);
		return;
	}
	if (!response.ok) {
		showServerError('Ошибка при загрузке профиля с сервера', response);
		return;
	}
	const json = await response.json();
	try {
		realProfileInfo = parseProfileInfo(json);
		displayedProfileInfo = structuredClone(realProfileInfo);
		// После загрузки профиля обновляем имя и уведомления
		renderPersonalInfo();
		// updateNotificationBell();
	} catch (e) {
		showServerError('Ошибка при обработке данных профиля', e, json);
	}

	function parseProfileInfo(jsonFromApi) {
		return {
			name: jsonFromApi['Username'],
			email: jsonFromApi['Email'],
			description: jsonFromApi['Description'],
			availabilitiesByDay: getTimeSegmentsByDayOfWeek(jsonFromApi['Availabilities']),
			games: jsonFromApi['Games'].map(el => el['Name']),
		};
	}
}

async function saveChanges() {
	// возвращает успешно ли прошло сохранение

	// вытаскиваем из хтмла в displayedProfileInfo
	displayedProfileInfo.description = document.getElementById('description').value;
	// todo: вытаскивать данные из времени

	if (deepEqual(realProfileInfo, displayedProfileInfo)) {
		return true; // если ничего не изменилось, то и отправлять ничего не надо
		return true;
	}

	try {
		const postJson = {
			aboutDescription: displayedProfileInfo.description,
			skills: 'FPS, RPG, Strategy', // todo: убрать заглушку
			games: displayedProfileInfo.games,
			availabilities: formatAvailabilitiesByDayToPostJson(displayedProfileInfo.availabilitiesByDay),
		};
		const r = await fetch('/data/upsert', {
			method: 'POST',
			headers: {'Content-Type': 'application/json'},
			body: JSON.stringify(postJson),
		});

		if (!r.ok) {
			return false;
		}

		realProfileInfo = structuredClone(displayedProfileInfo);
		return true;

	} catch (e) {
		return false;
	}

	function deepEqual(a, b) {
		if (a === b) return true;
		if (typeof a !== 'object' || typeof b !== 'object' || !a || !b) return false;
		if (Array.isArray(a) !== Array.isArray(b)) return false;

		const keysA = Object.keys(a);
		const keysB = Object.keys(b);

		if (keysA.length !== keysB.length) return false;
		for (const key of keysA) {
			if (!keysB.includes(key) || !deepEqual(a[key], b[key])) return false;
		}

		return true;
	}
}

function getTimeSegmentsByDayOfWeek(availabilities) {
	// Создает массив из 7 дней, где в каждом лежит массив промежутков времени
	// каждый промежуток - это пара из времени начала и времени конца по местному времени в формате hh:mm
	const weekSchedule = Array.from({length: 7}, () => []);

	for (const availability of availabilities) {
		const dayOfWeek = availability['DayOfWeek'];
		const startTime = availability['StartTime'];
		const endTime = availability['EndTime'];

		const parseTime = timeJson => {
			const time = new Date();
			time.setUTCHours(
				timeJson['Hour'] - (timeJson['Offset']['Seconds'] / 3600),
				timeJson['Minute'],
				timeJson['Second'],
				timeJson['Millisecond'],
			);
			return time;
		};

		const timeSlot = {
			startTime: parseTime(startTime),
			endTime: parseTime(endTime),
		};

		weekSchedule[dayOfWeek].push(timeSlot);
	}

	weekSchedule.forEach(daySlots => {
		daySlots.sort((a, b) => a.startTime.localeCompare(b.startTime));
	});

	return weekSchedule;
}

function formatAvailabilitiesByDayToPostJson(availabilitiesByDay) {
	// перегоняет из формата [0: [промежуток, ...], ..., 6: [промежуток, ...]] (группы промежутков по дням недели)
	// где промежуток: {startTime: Date, endTime: Date}
	// (в

	// в формат [
	//         {
	//             "id": -1,
	//             "dayOfWeek": <день недели>,
	//             "startTime": "18:00:00+03:00",
	//             "endTime": "22:00:00+03:00"
	//         }, ...
	//     ]

	const formatter = new Intl.DateTimeFormat('en-GB', {
		timeZone: 'UTC',
		hour: '2-digit',
		minute: '2-digit',
		second: '2-digit',
	});
	const result = [];

	availabilitiesByDay.forEach((intervals, day) => {
		intervals.forEach(interval => {
			result.push({
				dayOfWeek: day,
				startTime: `${formatter.format(interval.startTime)}+00:00`,
				endTime: `${formatter.format(interval.endTime)}+00:00`,
			});
		});
	});

	return result;
}