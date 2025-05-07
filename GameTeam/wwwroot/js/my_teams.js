import { createQuestionnaire } from '../js/questionnaire-template.js';
import { loadHeader } from '../js/header.js';

const state = {
    loading: false,
};

let dom = null;

document.addEventListener('DOMContentLoaded', async function () {
    await loadSidebar();
    await loadHeader();

    dom = loadDomElements();
    await loadAndRenderTeams();
});

async function loadSidebar() {
    const response = await fetch('../pages/Sidebar.html');
    const sidebarHtml = await response.text();
    document.getElementById('sidebar-placeholder').innerHTML = sidebarHtml;
}

function loadDomElements() {
    return {
        questionnairesContainer: document.querySelector('.questionnaires-container'),
    };
}

async function loadAndRenderTeams() {
    if (state.loading) return;

    state.loading = true;

    try {
        const response = await fetch('/data/teamapplications');
        const data = await response.json();

        if (!Array.isArray(data) || data.length === 0) {
            dom.questionnairesContainer.innerHTML = '<p>Пока что вы не состоите ни в одной команде, скорее исправьте это!)</p>';
            return;
        }

        const questionnaires = data.map(item => ({
            title: item.Title,
            description: item.Description,
            games: item.Games.map(g => g.Name),
            purpose: getPurposeText(item.PurposeId),
            availability: formatAvailabilities(item.Availabilities),
            contacts: item.Contacts, // Добавляем поле контактов
        }));

        for (const q of questionnaires) {
            const questionnaire = await createQuestionnaire(q);
            // Динамически добавляем секцию контактов
            if (q.contacts) {
                const content = questionnaire.querySelector('.questionnaire-content');
                const contactsSection = document.createElement('div');
                contactsSection.className = 'questionnaire-section';
                contactsSection.innerHTML = `
                    <label>Контакты</label>
                    <div>${q.contacts}</div>
                `;
                content.appendChild(contactsSection);
            }
            dom.questionnairesContainer.appendChild(questionnaire);
        }
    } catch (error) {
        console.error('Ошибка при загрузке анкет команд:', error);
        dom.questionnairesContainer.innerHTML = '<p>Ошибка загрузки команд.</p>';
    } finally {
        state.loading = false;
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