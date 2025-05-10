import { createQuestionnaire } from '../js/questionnaire-template.js';
import { loadHeader, showNotificationMessage } from '../js/header.js';

const state = {
    loading: false,
};

let dom = null;

document.addEventListener('DOMContentLoaded', async function () {
    await loadSidebar();
    await loadHeader();

    dom = loadDomElements();
    await loadAndRenderRequests();
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

async function loadAndRenderRequests() {
    if (state.loading) return;

    state.loading = true;

    try {
        const response = await fetch('/team/requests');
        const applicationIds = await response.json();

        if (!Array.isArray(applicationIds) || applicationIds.length === 0) {
            dom.questionnairesContainer.innerHTML = '<p class="no-teams-message">Вы ещё не подали заявки на вступление в команды!</p>';
            return;
        }

        const questionnaires = [];
        for (const id of applicationIds) {
            const appResponse = await fetch(`/data/application/${id}`);
            if (appResponse.ok) {
                const item = await appResponse.json();
                questionnaires.push({
                    id: item.Id,
                    title: item.Title,
                    description: item.Description,
                    games: item.Games.map(g => g.Name),
                    purpose: getPurposeText(item.PurposeId),
                    availability: formatAvailabilities(item.Availabilities),
                    members: item.OwnerUsername ? [item.OwnerUsername, ...(item.Members ? item.Members.map(m => m.Username) : [])] : (item.Members ? item.Members.map(m => m.Username) : []),
                });
            } else {
                console.warn(`Не удалось загрузить анкету с ID ${id}`);
            }
        }

        if (questionnaires.length === 0) {
            dom.questionnairesContainer.innerHTML = '<p class="no-teams-message">Вы ещё не подали заявки на вступление в команды!</p>';
            return;
        }

        for (const q of questionnaires) {
            const questionnaire = await createQuestionnaire(q);
            const content = questionnaire.querySelector('.questionnaire-content');
            let bottomSection = questionnaire.querySelector('.bottom-section');

            if (!bottomSection) {
                bottomSection = document.createElement('div');
                bottomSection.className = 'bottom-section';
                content.appendChild(bottomSection);
            } else {
                content.appendChild(bottomSection);
            }

            const cancelButton = document.createElement('button');
            cancelButton.className = 'outline-button';
            cancelButton.textContent = 'Отменить заявку';
            cancelButton.addEventListener('click', () => showCancelConfirmation(q.id));
            bottomSection.appendChild(cancelButton);

            dom.questionnairesContainer.appendChild(questionnaire);
        }
    } catch (error) {
        console.error('Ошибка при загрузке заявок:', error);
        dom.questionnairesContainer.innerHTML = '<p>Ошибка загрузки заявок.</p>';
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

function showCancelConfirmation(applicationId) {
    const modal = document.createElement('div');
    modal.className = 'confirmation-modal';
    modal.innerHTML = `
        <div class="confirmation-content">
            <p>Уверены, что хотите отменить свою заявку?</p>
            <div class="confirmation-actions">
                <button class="confirm-yes">Да</button>
                <button class="confirm-no">Нет</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    modal.querySelector('.confirm-yes').addEventListener('click', async () => {
        try {
            const response = await fetch(`/team/cancel/${applicationId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
            });
            if (response.ok) {
                showNotificationMessage('Заявка успешно отменена!', false);
                dom.questionnairesContainer.innerHTML = '';
                await loadAndRenderRequests();
            } else {
                showNotificationMessage('Ошибка при отмене заявки.', true);
            }
        } catch (error) {
            console.error('Ошибка при отмене заявки:', error);
            showNotificationMessage('Ошибка при отмене заявки.', true);
        }
        modal.remove();
    });

    modal.querySelector('.confirm-no').addEventListener('click', () => {
        modal.remove();
    });
}