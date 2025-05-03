import {initFilters, getCurrentFilter, applyFiltersButton} from './filters.js';

const state = {
    offset: 0,
    get limit() {
        return 11;
    },
    loading: false,
    endReached: false,
    isAuthenticated: false,
};

let dom = null;


document.addEventListener('DOMContentLoaded', async function () {
    dom = loadDomElements();
    await initFilters();
    loadAndRenderQuestionnaires();
    loadAndRenderUserName();
    dom.loadMoreButton.addEventListener('click', loadAndRenderQuestionnaires);
    applyFiltersButton.addEventListener('click', applyFilters);
});

function loadDomElements() {
    return {
        questionnairesContainer: document.querySelector('.questionnaires-container'),
        loadMoreButton: document.getElementById('load-more-button'),
        myQuestionnairesLink: document.querySelector('.my-q'),
    };
}

function applyFilters() {
    state.offset = 0;
    state.endReached = false;
    clearQuestionnaires();
    loadAndRenderQuestionnaires();
}

function clearQuestionnaires() {
    // Удаляем все дочерние элементы контейнера
    while (dom.questionnairesContainer.firstChild) {
        dom.questionnairesContainer.removeChild(dom.questionnairesContainer.firstChild);
    }
    // Восстанавливаем кнопку "Загрузить ещё", если она была удалена
    dom.loadMoreButton.style.display = 'block';
    dom.loadMoreButton.disabled = false;
}

function loadAndRenderQuestionnaires() {
    if (state.loading || state.endReached) return;

    state.loading = true;

    if (dom.loadMoreButton) {
        dom.loadMoreButton.textContent = 'Загрузка...';
        dom.loadMoreButton.disabled = true;
    }

    const currentFilter = getCurrentFilter();
    fetch(`/data/applications/${state.offset}/${state.offset + state.limit}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            purposeName: getPurposeText(currentFilter.purpose),
            games: currentFilter.games,
        }),
    })
        .then(response => response.json())
        .then(data => {
            if (!Array.isArray(data) || data.length === 0) {
                state.endReached = true;
                if (dom.loadMoreButton) {
                    dom.loadMoreButton.style.display = 'none';
                }
                return;
            }

            const questionnaires = data.map(item => ({
                title: item.Title,
                description: item.Description,
                games: item.Games.map(g => g.Name),
                purpose: getPurposeText(item.PurposeId),
                availability: formatAvailabilities(item.Availabilities),
            }));

            questionnaires.forEach(q => {
                const questionnaireDiv = document.createElement('div');
                questionnaireDiv.className = 'questionnaire';

                const contentDiv = document.createElement('div');
                contentDiv.className = 'questionnaire-content';

                const title = document.createElement('h2');
                title.textContent = q.title;
                contentDiv.appendChild(title);

                const descriptionSection = document.createElement('div');
                descriptionSection.className = 'questionnaire-section description-section';
                const descriptionP = document.createElement('p');
                descriptionP.textContent = q.description;
                descriptionSection.appendChild(descriptionP);
                contentDiv.appendChild(descriptionSection);

                const gamesSection = document.createElement('div');
                gamesSection.className = 'questionnaire-section';
                const gamesLabel = document.createElement('label');
                gamesLabel.textContent = 'Игры:';
                const gamesUl = document.createElement('ul');
                gamesUl.className = 'games-list';
                q.games.forEach(game => {
                    const li = document.createElement('li');
                    li.className = 'game-item';
                    li.innerHTML = `<span class="game-name">${game}</span>`;
                    gamesUl.appendChild(li);
                });
                gamesSection.appendChild(gamesLabel);
                gamesSection.appendChild(gamesUl);
                contentDiv.appendChild(gamesSection);

                const purposeSection = document.createElement('div');
                purposeSection.className = 'questionnaire-section';
                const purposeLabel = document.createElement('label');
                purposeLabel.textContent = 'Цель:';
                const purposeP = document.createElement('p');
                purposeP.textContent = q.purpose;
                purposeSection.appendChild(purposeLabel);
                purposeSection.appendChild(purposeP);
                contentDiv.appendChild(purposeSection);

                const availabilitySection = document.createElement('div');
                availabilitySection.className = 'questionnaire-section';
                availabilitySection.id = 'questionnaire-time';
                const availabilityLabel = document.createElement('label');
                availabilityLabel.textContent = 'Время:';
                const availabilityDiv = document.createElement('div');
                availabilityDiv.className = 'availability';
                availabilityDiv.innerHTML = q.availability;
                availabilitySection.appendChild(availabilityLabel);
                availabilitySection.appendChild(availabilityDiv);
                contentDiv.appendChild(availabilitySection);

                const bottomSection = document.createElement('div');
                bottomSection.className = 'bottom-section';
                const joinButton = document.createElement('button');
                joinButton.className = 'filled-button';
                joinButton.textContent = 'Вступить';
                bottomSection.appendChild(joinButton);
                contentDiv.appendChild(bottomSection);

                questionnaireDiv.appendChild(contentDiv);

                dom.questionnairesContainer.appendChild(questionnaireDiv);
            });

            state.offset += data.length;

            if (data.length < state.limit && dom.loadMoreButton) {
                dom.loadMoreButton.style.display = 'none';
                state.endReached = true;
            }
        })
        .then(response => response.json())
        .then(data => {
            if (!Array.isArray(data) || data.length === 0) {
                state.endReached = true;
                if (dom.loadMoreButton) {
                    dom.loadMoreButton.style.display = 'none';
                }
                return;
            }

            const questionnaires = data.map(item => ({
                title: item.Title,
                description: item.Description,
                games: item.Games.map(g => g.Name),
                purpose: getPurposeText(item.PurposeId),
                availability: formatAvailabilities(item.Availabilities),
            }));

            questionnaires.forEach(q => {
                const questionnaireDiv = document.createElement('div');
                questionnaireDiv.className = 'questionnaire';

                const contentDiv = document.createElement('div');
                contentDiv.className = 'questionnaire-content';

                const title = document.createElement('h2');
                title.textContent = q.title;
                contentDiv.appendChild(title);

                const descriptionSection = document.createElement('div');
                descriptionSection.className = 'questionnaire-section description-section';
                const descriptionP = document.createElement('p');
                descriptionP.textContent = q.description;
                descriptionSection.appendChild(descriptionP);
                contentDiv.appendChild(descriptionSection);

                const gamesSection = document.createElement('div');
                gamesSection.className = 'questionnaire-section';
                const gamesLabel = document.createElement('label');
                gamesLabel.textContent = 'Игры:';
                const gamesUl = document.createElement('ul');
                gamesUl.className = 'games-list';
                q.games.forEach(game => {
                    const li = document.createElement('li');
                    li.className = 'game-item';
                    li.innerHTML = `<span class="game-name">${game}</span>`;
                    gamesUl.appendChild(li);
                });
                gamesSection.appendChild(gamesLabel);
                gamesSection.appendChild(gamesUl);
                contentDiv.appendChild(gamesSection);

                const purposeSection = document.createElement('div');
                purposeSection.className = 'questionnaire-section';
                const purposeLabel = document.createElement('label');
                purposeLabel.textContent = 'Цель:';
                const purposeP = document.createElement('p');
                purposeP.textContent = q.purpose;
                purposeSection.appendChild(purposeLabel);
                purposeSection.appendChild(purposeP);
                contentDiv.appendChild(purposeSection);

                const availabilitySection = document.createElement('div');
                availabilitySection.className = 'questionnaire-section';
                availabilitySection.id = 'questionnaire-time';
                const availabilityLabel = document.createElement('label');
                availabilityLabel.textContent = 'Время:';
                const availabilityDiv = document.createElement('div');
                availabilityDiv.className = 'availability';
                availabilityDiv.innerHTML = q.availability;
                availabilitySection.appendChild(availabilityLabel);
                availabilitySection.appendChild(availabilityDiv);
                contentDiv.appendChild(availabilitySection);

                const bottomSection = document.createElement('div');
                bottomSection.className = 'bottom-section';
                const joinButton = document.createElement('button');
                joinButton.className = 'filled-button';
                joinButton.textContent = 'Вступить';
                bottomSection.appendChild(joinButton);
                contentDiv.appendChild(bottomSection);

                questionnaireDiv.appendChild(contentDiv);

                dom.questionnairesContainer.appendChild(questionnaireDiv);
            });

            state.offset += data.length;

            if (data.length < state.limit && dom.loadMoreButton) {
                dom.loadMoreButton.style.display = 'none';
                state.endReached = true;
            }
        })
        .catch(error => {
            console.error('Ошибка при загрузке анкет:', error);
        })
        .finally(() => {
            state.loading = false;
            if (!state.endReached && dom.loadMoreButton) {
                dom.loadMoreButton.textContent = 'Загрузить ещё';
                dom.loadMoreButton.disabled = false;
            }
        });
}

function loadAndRenderUserName() {
    fetch('/data/profile')
        .then(response => {
            state.isAuthenticated = response.headers.get('X-Is-Authenticated') === 'true';
            const userNameElements = document.querySelectorAll('.user-name');

            if (dom.myQuestionnairesLink) {
                dom.myQuestionnairesLink.style.display = state.isAuthenticated ? 'flex' : 'none';
            }

            const profileElement = document.querySelectorAll('.auth-status.user-name');
            if (!state.isAuthenticated) {
                profileElement.forEach(el => el.href = '../pages/Register.html');
            } else {
                profileElement.forEach(el => el.href = '../pages/Profile.html');
            }

            if (response.ok) {
                return response.json().then(json => {
                    if (state.isAuthenticated && json && json['Username']) {
                        userNameElements.forEach(el => el.textContent = json['Username']);
                    } else {
                        userNameElements.forEach(el => el.textContent = 'Вход не выполнен');
                    }
                });
            } else {
                userNameElements.forEach(el => el.textContent = 'Вход не выполнен');
                if (dom.myQuestionnairesLink) {
                    dom.myQuestionnairesLink.style.display = 'none';
                }
                return Promise.reject('Profile load error');
            }
        })
        .catch(err => {
            console.error('Ошибка при получении имени пользователя:', err);
            document.querySelectorAll('.user-name').forEach(el => el.textContent = 'Вход не выполнен');
        });
}

function getPurposeText(id) {
    const purposes = {
        2: 'Пофаниться',
        3: 'Посоревноваться',
        4: 'Расслабиться',
        5: 'Поиграть в сюжетную игру',
        6: 'Для стриминга',
        1: 'Для заработка',
        7: 'Тренировка',
        8: 'Турнир',
    };
    return purposes[id] || 'Неизвестная цель';
}

function formatAvailabilities(availabilities) {
    const days = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

    if (!availabilities || availabilities.length === 0) {
        return '<div class="time-row no-time">Пользователь не указал время — похоже, готов играть круглосуточно :))</div>';
    }

    const daySchedules = Array(7).fill(null).map(() => []);

    availabilities.forEach(a => {
        if (a.DayOfWeek >= 0 && a.DayOfWeek < 7) {
            const startHour = String(a.StartTime.Hour).padStart(2, '0');
            const startMinute = String(a.StartTime.Minute).padStart(2, '0');
            const endHour = String(a.EndTime.Hour).padStart(2, '0');
            const endMinute = String(a.EndTime.Minute).padStart(2, '0');
            daySchedules[a.DayOfWeek].push(`${startHour}:${startMinute} – ${endHour}:${endMinute}`);
        }
    });

    const availabilityLines = days
        .map((day, index) => {
            if (daySchedules[index].length > 0) {
                return `<div class="time-row">${day}: ${daySchedules[index].join(', ')}</div>`;
            }
            return null;
        })
        .filter(line => line !== null);

    return availabilityLines.join('');
}