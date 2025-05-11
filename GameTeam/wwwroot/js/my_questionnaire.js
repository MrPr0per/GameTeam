import { buttonsActivator } from '../js/buttonsActivator.js';
import { loadHeader } from '../js/header.js';
import { loadSidebar, initSidebar } from '../js/sidebar.js'; 

let debugMode = true;

const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

const state = {
	isEditing: false,
	isPlaced: false,
	serverQuestionnaire: null,
	localQuestionnaire: {
		title: '',
		description: '',
		games: [],
		goal: '',
		availabilities: [],
		contacts: '',
		members: [],
		ownerUsername: '', 
	},
	usersToRemove: [], 
};


async function loadComponents() {
	await loadSidebar();
	await loadHeader();
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
		if (state.isEditing) return;
		state.isEditing = true;
		state.serverQuestionnaire = JSON.parse(JSON.stringify(state.localQuestionnaire));
		state.usersToRemove = []; // Сбрасываем список удаляемых пользователей
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => {
			el.contentEditable = true;
			el.innerHTML = el.innerText.trim();
		});
		displayQuestionnaire();
		updateStatusAndButtons();
	}

	// Выключение режима редактирования
	async function disableEditMode(saveChanges = true) {
		state.isEditing = false;
		if (saveChanges) {
			state.localQuestionnaire.title = document.querySelector('.questionnaire-title').innerText.trim();
			state.localQuestionnaire.description = document.querySelector('.questionnaire-description').innerText.trim();
			state.localQuestionnaire.games = Array.from(gamesList.querySelectorAll('.game-name')).map(span => span.textContent.trim());
			state.localQuestionnaire.contacts = document.querySelector('.questionnaire-contacts').innerText.trim();

			if (isFormValid()) {
				const success = await postMyQuestionnaire();
				if (success) {
					state.serverQuestionnaire = JSON.parse(JSON.stringify(state.localQuestionnaire));
					state.usersToRemove = []; 
				} else {
					state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
					state.usersToRemove = []; 
				}
			} else {
				warningMessage.textContent = 'Заполните все обязательные поля (название, игра, цель и контакты)';
				state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
				state.usersToRemove = []; 
			}
		} else {
			state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
			state.usersToRemove = []; 
		}
		questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
		displayQuestionnaire();
		updateStatusAndButtons();
	}

	// "Редактировать анкету"
	editButton.addEventListener('click', enableEditMode);

	// "Разместить анкету" / "Скрыть анкету"
	placeButton.addEventListener('click', function () {
		if (state.isEditing) return;
		if (state.isPlaced) {
			hideMyQuestionnaire().then(success => {
				if (!success) return;
				state.isPlaced = false;
				updateStatusAndButtons();
			});
		} else {
			if (!isFormValid()) {
				warningMessage.textContent = 'Заполните все обязательные поля (название, игра, цель и контакты)';
				return;
			}
			hideMyQuestionnaire().then(success => {
				if (!success) return;
				state.isPlaced = true;
				updateStatusAndButtons();
			});
		}
	});

	// "Отмена"
	cancelButton.addEventListener('click', function () {
		if (state.isEditing) disableEditMode(false);
	});

	// "Сохранить изменения"
	saveButton.addEventListener('click', function () {
		if (state.isEditing) disableEditMode(true);
	});

	// "Добавить игру"
	addGameButton.addEventListener('click', function () {
		const newGame = newGameInput.value.trim();
		if (newGame) {
			state.localQuestionnaire.games.push(newGame);
			displayQuestionnaire(true);
			newGameInput.value = '';
			updateStatusAndButtons();
		}
	});
}

function displayQuestionnaire(updateGamesOnly = false) {
	const questionnaireContent = document.querySelector('.questionnaire-content');

	let membersSection = questionnaireContent.querySelector('.members-section');
	if (!membersSection) {
		membersSection = document.createElement('div');
		membersSection.className = 'questionnaire-section members-section';
		membersSection.innerHTML = `
            <label>Участники:</label>
            <div class="members-list"></div>
        `;
		const contactsSection = questionnaireContent.querySelector('.contacts-section');
		if (contactsSection) {
			questionnaireContent.insertBefore(membersSection, contactsSection);
		} else {
			questionnaireContent.insertBefore(membersSection, questionnaireContent.querySelector('.bottom-section'));
		}
	}

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

	let contactsNote = contactsSection.querySelector('.contacts-note');
	if (state.isEditing) {
		if (!contactsNote) {
			contactsNote = document.createElement('p');
			contactsNote.className = 'contacts-note';
			contactsNote.style.fontSize = '13px';
			contactsSection.appendChild(contactsNote);
		}
		contactsNote.innerHTML = 'P.S. Не бойтесь вводить контакты — их увидят только те счастливчики, которых вы примете в команду! 😎';
	} else {
		if (contactsNote) {
			contactsNote.remove();
		}
	}

	if (!updateGamesOnly) {
		const titleElement = questionnaireContent.querySelector('.questionnaire-title');
		titleElement.innerHTML = state.localQuestionnaire.title || '';
		const descriptionElement = questionnaireContent.querySelector('.questionnaire-description');
		descriptionElement.innerHTML = state.localQuestionnaire.description || '';
		const contactsElement = questionnaireContent.querySelector('.questionnaire-contacts');
		contactsElement.innerHTML = state.localQuestionnaire.contacts || '';

		const membersList = membersSection.querySelector('.members-list');
		membersList.innerHTML = '';
		const allMembers = state.localQuestionnaire.ownerUsername
			? [{ username: state.localQuestionnaire.ownerUsername, userId: null }, ...state.localQuestionnaire.members]
			: state.localQuestionnaire.members;
		if (allMembers && allMembers.length > 0) {
			allMembers.forEach((member, index) => {
				const memberItem = document.createElement('div');
				memberItem.className = 'member-item';
				if (state.isEditing) memberItem.classList.add('editing');
				const memberName = document.createElement('span');
				memberName.className = 'member-name';
				memberName.textContent = member.username;
				memberName.addEventListener('click', () => {
					window.location.href = `/profile/${member.username}`;
				});

				if (index === 0 && state.localQuestionnaire.ownerUsername) {
					const crown = document.createElement('img');
					crown.src = '../img/crown.svg';
					crown.className = 'owner-crown';
					crown.alt = 'Owner Crown';
					memberItem.appendChild(crown);
				}

				memberItem.appendChild(memberName);
				if (state.isEditing && index !== 0 && member.userId) {
					const deleteButton = document.createElement('span');
					deleteButton.className = 'delete-member';
					deleteButton.textContent = '✕';
					deleteButton.addEventListener('click', () => {
						showRemoveMemberConfirmation(member.userId, member.username, index - 1); // index - 1, так как ownerUsername не в members
					});
					memberItem.appendChild(deleteButton);
				}
				membersList.appendChild(memberItem);
			});
		} else {
			membersList.textContent = 'Участники отсутствуют';
		}

		const purposeContainer = questionnaireContent.querySelector('.questionnaire-purpose');
		if (state.isEditing) {
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
				if (option === state.localQuestionnaire.goal) opt.selected = true;
				select.appendChild(opt);
			});
			purposeContainer.innerHTML = '';
			select.addEventListener('change', function () {
				state.localQuestionnaire.goal = this.value;
			});
			purposeContainer.appendChild(select);
		} else {
			purposeContainer.innerHTML = `<p>${state.localQuestionnaire.goal || ''}</p>`;
		}

		const availabilityContainer = questionnaireContent.querySelector('.availability');
		availabilityContainer.innerHTML = '';
		if (state.isEditing) {
			let timeHtml = '';
			daysOfWeek.forEach((day, index) => {
				const availability = state.localQuestionnaire.availabilities.find(a => a.day === index) || { start: '', end: '' };
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
					let availability = state.localQuestionnaire.availabilities.find(a => a.day === day);
					if (!availability) {
						availability = { day: day, start: '', end: '' };
						state.localQuestionnaire.availabilities.push(availability);
					}
					availability[type] = value;
					state.localQuestionnaire.availabilities = state.localQuestionnaire.availabilities.filter(a => a.start || a.end);
				});
			});
		} else {
			let timeHtml = '';
			daysOfWeek.forEach((day, index) => {
				const availability = state.localQuestionnaire.availabilities.find(a => a.day === index);
				const timeStr = availability && availability.start && availability.end ? `${availability.start} - ${availability.end}` : '—';
				timeHtml += `<div class="time-row"><span>${day}:</span> ${timeStr}</div>`;
			});
			availabilityContainer.innerHTML = timeHtml;
		}
	}

	const gamesList = questionnaireContent.querySelector('.games-list');
	gamesList.innerHTML = '';
	state.localQuestionnaire.games.forEach((game, index) => {
		const li = document.createElement('li');
		li.className = 'game-item';
		if (state.isEditing) li.classList.add('editing');
		const gameName = document.createElement('span');
		gameName.className = 'game-name';
		gameName.textContent = game;
		li.appendChild(gameName);
		if (state.isEditing) {
			const deleteButton = document.createElement('span');
			deleteButton.className = 'delete-game';
			deleteButton.textContent = '✕';
			deleteButton.addEventListener('click', () => {
				state.localQuestionnaire.games.splice(index, 1);
				displayQuestionnaire(true);
				updateStatusAndButtons();
			});
			li.appendChild(deleteButton);
		}
		gamesList.appendChild(li);
	});
}

function showRemoveMemberConfirmation(userId, username, index) {
	const modal = document.createElement('div');
	modal.className = 'confirmation-modal';
	modal.innerHTML = `
        <div class="confirmation-content">
            <p>Вы уверены, что хотите удалить ${username} из своей команды?</p>
            <div class="confirmation-actions">
                <button class="confirm-yes">Да</button>
                <button class="confirm-no">Нет</button>
            </div>
        </div>
    `;
	document.body.appendChild(modal);

	modal.querySelector('.confirm-yes').addEventListener('click', () => {
		state.usersToRemove.push(userId); // Добавляем userId в список для удаления
		state.localQuestionnaire.members.splice(index, 1); // Удаляем локально
		displayQuestionnaire(); // Обновляем отображение
		updateStatusAndButtons();
		modal.remove();
	});

	modal.querySelector('.confirm-no').addEventListener('click', () => {
		modal.remove();
	});
}

function isFormValid() {
	const title = state.localQuestionnaire.title.trim();
	const games = state.localQuestionnaire.games;
	const goal = state.localQuestionnaire.goal.trim();
	const contacts = state.localQuestionnaire.contacts.trim();
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

	statusText.textContent = state.isPlaced ? 'ваша анкета размещена во вкладке поиск команды' : 'ваша анкета скрыта';
	const formValid = isFormValid();

	if (state.isEditing) {
		if (statusButtons) statusButtons.style.display = 'none';
		if (editButtons) editButtons.style.display = 'flex';
		if (addGameSection) addGameSection.classList.add('active');
		document.querySelectorAll('.required').forEach(star => {
			star.style.display = 'inline';
		});
		if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля (название анкеты, хотя бы одна игры, цель и контакты)';
	} else {
		if (statusButtons) statusButtons.style.display = 'flex';
		if (editButtons) editButtons.style.display = 'none';
		if (addGameSection) addGameSection.classList.remove('active');
		document.querySelectorAll('.required').forEach(star => {
			star.style.display = 'none';
		});

		if (state.isPlaced) {
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
			if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля (название, игра, цель и контакты)';
		}
	}
}

async function loadMyQuestionnaire() {
	const response = await fetch('/data/selfapplications', { method: 'GET' });

	if (!response.ok) {
		state.serverQuestionnaire = {
			id: -1,
			title: '',
			description: '',
			games: [],
			goal: '',
			availabilities: [],
			contacts: '',
			members: [],
			ownerUsername: '',
		};
	} else {
		const application = await response.json();
		if (application[0]) {
			state.serverQuestionnaire = transformQuestionnaire(application[0]);
			state.isPlaced = !application[0].IsHidden;
		} else {
			state.serverQuestionnaire = {
				id: -1,
				title: '',
				description: '',
				games: [],
				goal: '',
				availabilities: [],
				contacts: '',
				members: [],
				ownerUsername: '',
			};
		}
	}

	state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
}

async function getJsonForPostQuestionnaire() {
	let applicationId = -1;
	if (state.serverQuestionnaire.id === -1) {
		const response = await fetch('data/applicationid', { method: 'GET' });
		applicationId = await response.text();
	} else {
		applicationId = state.serverQuestionnaire.id;
	}
	const availabilities = state.localQuestionnaire.availabilities
		.filter(a => a.start && a.end)
		.map(a => ({
			dayOfWeek: a.day,
			startTime: a.start ? `${a.start}:00+00:00` : '',
			endTime: a.end ? `${a.end}:00+00:00` : ''
		}));
	return {
		id: applicationId,
		title: state.localQuestionnaire.title,
		description: state.localQuestionnaire.description,
		contacts: state.localQuestionnaire.contacts,
		purposeName: state.localQuestionnaire.goal,
		availabilities: availabilities,
		games: state.localQuestionnaire.games,
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
	let success = response.ok;
	if (success) {
		state.serverQuestionnaire = JSON.parse(JSON.stringify(state.localQuestionnaire));
		state.serverQuestionnaire.id = jsonData.id;
	} else {
		showServerError('Ошибка при отправке анкеты', response);
	}

	if (state.usersToRemove.length > 0 && success) {
		for (const userId of state.usersToRemove) {
			const removeResponse = await fetch(`/team/remove/${userId}/${jsonData.id}`, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json',
				},
			});
			if (!removeResponse.ok) {
				showServerError(`Ошибка при удалении пользователя ${userId}`, removeResponse);
				success = false;
			}
		}
	}

	if (success) {
		state.usersToRemove = []; 
	}
	return success;
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

	let members = [];
	if (Array.isArray(questionnaireData.Members)) {
		members = questionnaireData.Members.map(member => ({
			userId: member.UserId,
			username: member.Username,
		}));
	}

	return {
		id: questionnaireData.Id,
		title: questionnaireData.Title || '',
		description: questionnaireData.Description || '',
		games: games,
		goal: goal,
		availabilities: availabilities,
		contacts: questionnaireData.Contacts || '',
		members: members,
		ownerUsername: questionnaireData.OwnerUsername || '', 
	};
}

async function hideMyQuestionnaire() {
	if (!state.serverQuestionnaire || state.serverQuestionnaire.id === -1) {
		showServerError('Анкета не найдена');
		return false;
	}

	const action = state.isPlaced ? 'hide' : 'show';
	const url = `/data/${action}/${state.serverQuestionnaire.id}`;

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