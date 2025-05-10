import { createQuestionnaire } from '../js/questionnaire-template.js';
import { loadHeader, showNotificationMessage } from '../js/header.js';
import { loadSidebar, initSidebar } from '../js/sidebar.js'; 

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
            dom.questionnairesContainer.innerHTML = '<p class="no-teams-message">Пока что вы не состоите ни в одной команде, скорее исправьте это!)</p>';
            return;
        }

        const questionnaires = data.map(item => ({
            id: item.Id, 
            title: item.Title,
            description: item.Description,
            games: item.Games.map(g => g.Name),
            purpose: getPurposeText(item.PurposeId),
            availability: formatAvailabilities(item.Availabilities),
            contacts: item.Contacts,
            members: item.OwnerUsername ? [item.OwnerUsername, ...(item.Members ? item.Members.map(m => m.Username) : [])] : (item.Members ? item.Members.map(m => m.Username) : []),
        }));

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

            if (q.contacts) {
                const contactsSection = document.createElement('div');
                contactsSection.className = 'questionnaire-section';
                contactsSection.innerHTML = `
                    <label>Контакты</label>
                    <div>${q.contacts}</div>
                `;
                content.insertBefore(contactsSection, bottomSection); 
            }

            const leaveButton = document.createElement('button');
            leaveButton.className = 'filled-button';
            leaveButton.textContent = 'Покинуть команду';
            leaveButton.addEventListener('click', () => showLeaveConfirmation(q.id));
            bottomSection.appendChild(leaveButton);

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

// Функция для показа модального окна подтверждения
function showLeaveConfirmation(teamId) {
    const modal = document.createElement('div');
    modal.className = 'confirmation-modal';
    modal.innerHTML = `
        <div class="confirmation-content">
            <p>Уверены, что хотите покинуть команду?</p>
            <div class="confirmation-actions">
                <button class="confirm-yes">Да</button>
                <button class="confirm-no">Нет</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    modal.querySelector('.confirm-yes').addEventListener('click', async () => {
        try {
            const response = await fetch(`/team/leave/${teamId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
            });
            if (response.ok) {
                showNotificationMessage('Вы вышли из команды!', false);
                dom.questionnairesContainer.innerHTML = '';
                await loadAndRenderTeams();
            } else {
                showNotificationMessage('Ошибка при выходе из команды.', true);
            }
        } catch (error) {
            console.error('Ошибка при выходе из команды:', error);
            showNotificationMessage('Ошибка при выходе из команды.', true);
        }
        modal.remove();
    });

    modal.querySelector('.confirm-no').addEventListener('click', () => {
        modal.remove();
    });
}


