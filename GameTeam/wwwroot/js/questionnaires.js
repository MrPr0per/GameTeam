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
        modalTemplate: document.getElementById('modal-template'),
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
                contacts: item.Contacts,
            }));

            questionnaires.forEach(q => {
                const questionnaireDiv = document.createElement('div');
                questionnaireDiv.className = 'questionnaire';

                const title = document.createElement('h2');
                title.textContent = q.title;

                const description = document.createElement('p');
                description.textContent = q.description;

                const button = document.createElement('button');
                button.className = 'filled-button';
                button.textContent = 'Подробнее';
                button.addEventListener('click', () => openModal(q));

                questionnaireDiv.appendChild(title);
                questionnaireDiv.appendChild(description);
                questionnaireDiv.appendChild(button);

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
                dom.myQuestionnairesLink.style.display = isAuthenticated ? 'flex' : 'none';
            }

            const profileElement = document.querySelectorAll('.auth-status.user-name');
            if (!isAuthenticated) {
                profileElement.forEach(el => el.href = '../pages/Register.html');
            } else {
                profileElement.forEach(el => el.href = '../pages/Profile.html');
            }

            if (response.ok) {
                return response.json().then(json => {
                    if (isAuthenticated && json && json['Username']) {
                        userNameElements.forEach(el => el.textContent = json['Username']);
                    } else {
                        userNameElements.forEach(el => el.textContent = 'Вход не выполнен');
                    }
                });
            } else {
                // Для статуса 401 или других ошибок устанавливаем "Вход не выполнен"
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

function openModal(questionnaire) {
    const modalOverlay = dom.modalTemplate.content.cloneNode(true).firstElementChild;
    const modalContent = modalOverlay.querySelector('.modal-content');

    modalContent.querySelector('h2').textContent = questionnaire.title;
    modalContent.querySelector('.modal-description').textContent = questionnaire.description;
    modalContent.querySelector('.modal-games').innerHTML = questionnaire.games.map(g => `<li>${g}</li>`).join('');
    modalContent.querySelector('.modal-purpose').textContent = questionnaire.purpose;
    modalContent.querySelector('.modal-availability').innerHTML = questionnaire.availability;
    modalContent.querySelector('.modal-contacts').textContent = questionnaire.contacts;

    modalContent.querySelector('.close-button').addEventListener('click', () => closeModal(modalOverlay));
    modalOverlay.addEventListener('click', (e) => {
        if (e.target === modalOverlay) {
            closeModal(modalOverlay);
        }
    });

    document.body.appendChild(modalOverlay);
}

function closeModal(modalOverlay) {
    modalOverlay.remove();
}

function getPurposeText(id) {
    const purposes = {
        1: 'Пофаниться',
        2: 'Поиграть в соревновательные режимы',
        3: 'Расслабиться',
        4: 'Поиграть в сюжетную игру',
        5: 'Для стриминга',
        6: 'Для заработка',
        7: 'Тренировка',
        8: 'Турнир',
    };
    return purposes[id] || 'Неизвестная цель';
}

function formatAvailabilities(availabilities) {
    const days = ['Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота', 'Воскресенье'];

    if (!availabilities || availabilities.length === 0) return 'Не указано';

    return availabilities.map(a => {
        const dayName = days[a.DayOfWeek] || 'Неизвестный день';
        const startHour = String(a.StartTime.Hour).padStart(2, '0');
        const startMinute = String(a.StartTime.Minute).padStart(2, '0');
        const endHour = String(a.EndTime.Hour).padStart(2, '0');
        const endMinute = String(a.EndTime.Minute).padStart(2, '0');

        return `${dayName}: ${startHour}:${startMinute} – ${endHour}:${endMinute}`;
    }).join('<br>');
}
