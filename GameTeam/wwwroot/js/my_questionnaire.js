import { loadHeader } from '../js/header.js';
import { loadSidebar } from '../js/sidebar.js';
import { showError } from './errors.js';
import { displayQuestionnaire, isFormValid, updateStatusAndButtons } from '../js/my_questionnaireUtils.js';

export const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

export const state = {
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
    loadComponents();
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

    function enableEditMode() {
        if (state.isEditing) return;
        state.isEditing = true;
        state.serverQuestionnaire = JSON.parse(JSON.stringify(state.localQuestionnaire));
        state.usersToRemove = [];
        document.querySelectorAll('[contenteditable]').forEach(el => {
            el.contentEditable = true;
            el.innerHTML = el.innerText.trim();
        });
        displayQuestionnaire();
        updateStatusAndButtons();
    }

    async function disableEditMode(saveChanges = true) {
        state.isEditing = false;
        if (saveChanges) {
            state.localQuestionnaire.title = document.querySelector('.questionnaire-title').innerText.trim();
            state.localQuestionnaire.description = document.querySelector('.questionnaire-description').innerText.trim();
            state.localQuestionnaire.games = Array.from(document.querySelector('.games-list').querySelectorAll('.game-name')).map(span => span.textContent.trim());
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
                document.getElementById('warning-message').textContent = 'Заполните все обязательные поля (название, игра, цель и контакты)';
                state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
                state.usersToRemove = [];
            }
        } else {
            state.localQuestionnaire = JSON.parse(JSON.stringify(state.serverQuestionnaire));
            state.usersToRemove = [];
        }
        document.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
        displayQuestionnaire();
        updateStatusAndButtons();
    }

    editButton.addEventListener('click', enableEditMode);

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
                document.getElementById('warning-message').textContent = 'Заполните все обязательные поля (название, игра, цель и контакты)';
                return;
            }
            hideMyQuestionnaire().then(success => {
                if (!success) return;
                state.isPlaced = true;
                updateStatusAndButtons();
            });
        }
    });

    cancelButton.addEventListener('click', function () {
        if (state.isEditing) disableEditMode(false);
    });

    saveButton.addEventListener('click', function () {
        if (state.isEditing) disableEditMode(true);
    });

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
        showError('Ошибка при отправке анкеты', response);
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
                showError(`Ошибка при удалении пользователя ${userId}`, removeResponse);
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
        showError('Анкета не найдена');
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
            showError(`Ошибка при ${action === 'hide' ? 'скрытии' : 'показе'} анкеты`, response);
            return false;
        }
    } catch (error) {
        showError(`Ошибка при ${action === 'hide' ? 'скрытии' : 'показе'} анкеты`, error);
        return false;
    }
}