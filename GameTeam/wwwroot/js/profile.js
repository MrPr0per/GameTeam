// todo: сдизайнить, сверстать редактирование времени
// todo: ошибки:
// todo: 	придумать, куда впихнуть плашку с ошибкой (при фетче, при парсинге, при отправке данных)
// todo: апи не удаляет игры

const debugMode = true;

let realProfileInfo = {
	description: '',
	availabilitiesByDay: Array.from({length: 7}, () => []),
	games: [],
};
let displayedProfileInfo = structuredClone(realProfileInfo);

document.addEventListener('DOMContentLoaded', function () {
	addEventListeners();
	loadProfileInfo().then(() => {
		document.getElementById('edit-button').disabled = false;
		updateDisplayOfProfileInfo(false);
	});
});

function addEventListeners() {
	const tabs = document.querySelectorAll('.tab-link');
	const panes = document.querySelectorAll('.tab-pane');
	const textarea = document.getElementById('description');
	const editButton = document.getElementById('edit-button');
	const saveCancelButtons = document.getElementById('save-cancel-buttons');
	const saveButton = document.getElementById('save-button');
	const cancelButton = document.getElementById('cancel-button');
	const addGameButton = document.getElementById('add-game-button');
	const newGameInput = document.getElementById('new-game-input');

	// Переключение вкладок
	tabs.forEach(tab => {
		tab.addEventListener('click', function () {
			tabs.forEach(t => t.classList.remove('active'));
			this.classList.add('active');
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

	// Кнопка "Готово"
	saveButton.addEventListener('click', function () {
		saveChanges()
			.then(success => {
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
	renderDescription();
	renderFreeTime();
	renderGames(isEditing);
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

async function loadProfileInfo() {
	await fetch('/data/profile')
		.then(r => {
			if (r.ok) {
				return r.json();
			} else if (r.status === 401) {
				window.location.href = '/register';
			} else {
				showServerError('Ошибка при загрузке профиля с сервера', r);
			}
		})
		.then(jsonOrNoneIfError => {
			if (jsonOrNoneIfError !== undefined) {
				try {
					realProfileInfo = parseProfileInfo(jsonOrNoneIfError);
					displayedProfileInfo = structuredClone(realProfileInfo);
				} catch (e) {
					showServerError('Ошибка при обработке данных профиля',
						'(Данные прилетели не в том формате, в котором ожидалось)',
						jsonOrNoneIfError);
				}
			}
		});

	function parseProfileInfo(jsonFromApi) {
		return {
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
	}

	try {
		const postJson = {
			aboutDescription: displayedProfileInfo.description,
			skills: 'FPS, RPG, Strategy', // todo: убрать заглушку
			games: displayedProfileInfo.games.map((name) => ({id: -1, name: name})), // todo: убрать заглушку
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
				id: -1,
				dayOfWeek: day,
				startTime: `${formatter.format(interval.startTime)}+00:00`,
				endTime: `${formatter.format(interval.endTime)}+00:00`,
			});
		});
	});

	return result;
}

function showServerError(message, ...debugInfo) {
	if (debugMode) {
		console.log(message, debugInfo);
	} else {
		console.log(message); // todo: сделать плашку об ошибке
	}
}