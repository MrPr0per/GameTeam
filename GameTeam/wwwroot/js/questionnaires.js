import {initFilters, getCurrentFilter, applyFiltersButton} from '../js/filters.js';
import {createQuestionnaire} from '../js/questionnaire-template.js';
import {loadHeader} from '../js/header.js';

const state = {
    offset: 0,
    get limit() {
        return 5;
    },
    loading: false,
    endReached: false,
};

let dom = null;

document.addEventListener('DOMContentLoaded', async function () {
    await loadSidebar();
    await loadHeader(); // Подгружаем header после sidebar

    dom = loadDomElements();
    await initFilters();
    loadAndRenderQuestionnaires();
    dom.loadMoreButton.addEventListener('click', loadAndRenderQuestionnaires);
    applyFiltersButton.addEventListener('click', applyFilters);
});

async function loadSidebar() {
    const response = await fetch('../pages/Sidebar.html');
    const sidebarHtml = await response.text();
    const layout = document.querySelector('.layout');
    layout.insertAdjacentHTML('afterbegin', sidebarHtml);
}

function loadDomElements() {
    return {
        questionnairesContainer: document.querySelector('.questionnaires-container'),
        loadMoreButton: document.getElementById('load-more-button'),
    };
}

function applyFilters() {
    state.offset = 0;
    state.endReached = false;
    clearQuestionnaires();
    loadAndRenderQuestionnaires();
}

function clearQuestionnaires() {
    while (dom.questionnairesContainer.firstChild) {
        dom.questionnairesContainer.removeChild(dom.questionnairesContainer.firstChild);
    }
    dom.loadMoreButton.style.display = 'block';
    dom.loadMoreButton.disabled = false;
}

async function loadAndRenderQuestionnaires() {
    if (state.loading || state.endReached) return;

    state.loading = true;

    if (dom.loadMoreButton) {
        dom.loadMoreButton.textContent = 'Загрузка...';
        dom.loadMoreButton.disabled = true;
    }

    const currentFilter = getCurrentFilter();
    try {
        const payload = {
            games: currentFilter.games,
        };
        if (currentFilter.purpose !== null) {
            payload.purposeName = getPurposeText(currentFilter.purpose);
        }
        const response = await fetch(`/data/applications/${state.offset}/${state.offset + state.limit}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        });
        const data = await response.json();

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

        for (const q of questionnaires) {
            const questionnaire = await createQuestionnaire(q);
            const bottomSection = questionnaire.querySelector('.bottom-section');
            const joinButton = document.createElement('button');
            joinButton.className = 'filled-button';
            joinButton.textContent = 'Вступить';
            bottomSection.appendChild(joinButton);

            dom.questionnairesContainer.appendChild(questionnaire);
        }

        state.offset += data.length;

        if (data.length < state.limit && dom.loadMoreButton) {
            dom.loadMoreButton.style.display = 'none';
            state.endReached = true;
        }
    } catch (error) {
        console.error('Ошибка при загрузке анкет:', error);
    } finally {
        state.loading = false;
        if (!state.endReached && dom.loadMoreButton) {
            dom.loadMoreButton.textContent = 'Загрузить ещё';
            dom.loadMoreButton.disabled = false;
        }
    }
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
    return purposes[id];
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