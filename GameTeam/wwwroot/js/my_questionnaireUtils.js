import { state, daysOfWeek } from './my_questionnaire.js';

export function displayQuestionnaire(updateGamesOnly = false) {
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
                        showRemoveMemberConfirmation(member.userId, member.username, index - 1);
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

export function showRemoveMemberConfirmation(userId, username, index) {
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
        state.usersToRemove.push(userId);
        state.localQuestionnaire.members.splice(index, 1);
        displayQuestionnaire();
        updateStatusAndButtons();
        modal.remove();
    });

    modal.querySelector('.confirm-no').addEventListener('click', () => {
        modal.remove();
    });
}

export function isFormValid() {
    const title = state.localQuestionnaire.title.trim();
    const games = state.localQuestionnaire.games;
    const goal = state.localQuestionnaire.goal.trim();
    const contacts = state.localQuestionnaire.contacts.trim();
    return title !== '' && games.length > 0 && goal !== '' && contacts !== '';
}

export function updateStatusAndButtons() {
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
        if (warningMessage) warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля (название анкеты, хотя бы одна игра, цель и контакты)';
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