document.addEventListener('DOMContentLoaded', function () {
    const placeButton = document.getElementById('place-button');
    const editButton = document.getElementById('edit-button');
    const clearButton = document.getElementById('clear-button');
    const cancelButton = document.getElementById('cancel-button');
    const saveButton = document.getElementById('save-button');
    const addGameButton = document.getElementById('add-game-button');
    const newGameInput = document.getElementById('new-game-input');
    const editButtons = document.getElementById('edit-buttons');
    const statusButtons = document.getElementById('status-buttons');
    const questionnaireContent = document.getElementById('questionnaire-content');
    const statusText = document.getElementById('status-text');
    const gamesList = document.getElementById('questionnaire-games');
    const addGameSection = document.getElementById('add-game-section');
    const warningMessage = document.getElementById('warning-message');

    let isEditing = false;
    let isPlaced = false;
    let originalData = null;

    // Данные анкеты 
    let questionnaireData = {
        title: "",
        description: "",
        games: [],
        goal: "",
        time: "",
        contacts: ""
    };

    // Функция для проверки валидности формы
    function isFormValid() {
        const title = questionnaireData.title.trim();
        const games = questionnaireData.games;
        const goal = questionnaireData.goal.trim();
        const contacts = questionnaireData.contacts.trim();
        return title !== '' && games.length > 0 && goal !== '' && contacts !== '';
    }

    // Функция для отображения анкеты
    function displayQuestionnaire(updateGamesOnly = false) {
        if (!updateGamesOnly) {
            document.getElementById('questionnaire-title').innerHTML = questionnaireData.title || '';
            document.getElementById('questionnaire-description').innerHTML = questionnaireData.description || '';
            document.getElementById('questionnaire-goal').innerHTML = questionnaireData.goal || '';
            document.getElementById('questionnaire-time').innerHTML = questionnaireData.time || '';
            document.getElementById('questionnaire-contacts').innerHTML = questionnaireData.contacts || '';
        }
        gamesList.innerHTML = '';
        questionnaireData.games.forEach((game, index) => {
            const li = document.createElement('li');
            li.className = 'game-item';
            if (isEditing) li.classList.add('editing');

            const contentDiv = document.createElement('div');
            contentDiv.className = 'game-item-content';

            const gameText = document.createElement('span');
            gameText.textContent = game;
            contentDiv.appendChild(gameText);

            if (isEditing) {
                const deleteButton = document.createElement('span');
                deleteButton.className = 'delete-game';
                deleteButton.textContent = '✕';
                deleteButton.addEventListener('click', () => {
                    questionnaireData.games.splice(index, 1);
                    displayQuestionnaire(true);
                    updateStatusAndButtons();
                });
                contentDiv.appendChild(deleteButton);
            }
            li.appendChild(contentDiv);
            gamesList.appendChild(li);
        });
    }

    // Функция для обновления статуса и кнопок
    function updateStatusAndButtons() {
        statusText.textContent = isPlaced ? "Размещена" : "Не размещена";
        const formValid = isFormValid();
        if (isEditing) {
            statusButtons.style.display = 'none';
            editButtons.style.display = 'flex';
            addGameSection.style.display = 'block';
            document.querySelectorAll('.required').forEach(star => star.style.display = 'inline');
        } else {
            statusButtons.style.display = 'flex';
            editButtons.style.display = 'none';
            addGameSection.style.display = 'none';
            document.querySelectorAll('.required').forEach(star => star.style.display = 'none');
            if (isPlaced) {
                placeButton.textContent = 'Скрыть анкету';
                placeButton.classList.remove('filled-button');
                placeButton.classList.add('outline-button');
                editButton.style.display = 'none';
                document.getElementById('edit-message').style.display = 'block';
            } else {
                placeButton.textContent = 'Разместить анкету';
                placeButton.classList.remove('outline-button');
                placeButton.classList.add('filled-button');
                placeButton.disabled = !formValid;
                editButton.style.display = 'inline-block';
                document.getElementById('edit-message').style.display = 'none';
                warningMessage.textContent = formValid ? '' : 'Заполните все обязательные поля';
            }
        }
    }

    // Изначальное отображение анкеты и статуса
    displayQuestionnaire();
    updateStatusAndButtons();

    // Включение режима редактирования
    function enableEditMode() {
        isEditing = true;
        originalData = JSON.parse(JSON.stringify(questionnaireData));
        questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => {
            el.contentEditable = true;
            const textContent = el.innerText.trim();
            el.innerHTML = textContent;
        });
        displayQuestionnaire();
        updateStatusAndButtons();
    }

    // Выключение режима редактирования
    function disableEditMode(saveChanges = true) {
        isEditing = false;
        if (saveChanges) {
            questionnaireData.title = document.getElementById('questionnaire-title').innerText.trim();
            questionnaireData.description = document.getElementById('questionnaire-description').innerText.trim();
            questionnaireData.games = Array.from(gamesList.querySelectorAll('li')).map(li => li.querySelector('span').textContent.trim());
            questionnaireData.goal = document.getElementById('questionnaire-goal').innerText.trim();
            questionnaireData.time = document.getElementById('questionnaire-time').innerText.trim();
            questionnaireData.contacts = document.getElementById('questionnaire-contacts').innerText.trim();
        } else {
            questionnaireData = originalData;
        }
        questionnaireContent.querySelectorAll('[contenteditable]').forEach(el => el.contentEditable = false);
        displayQuestionnaire();
        updateStatusAndButtons();
    }

    // "Разместить анкету" / "Скрыть анкету"
    placeButton.addEventListener('click', function () {
        if (isEditing) return;
        if (isPlaced) {
            alert('Анкета скрыта!');
            isPlaced = false;
            updateStatusAndButtons();
        } else if (isFormValid()) {
            fetch('http://localhost:5013/data/application', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(), // todo
            })
                .then(response => response.json())
                .then(data => console.log(data))
                .catch(error => console.error('Error:', error));

            alert('Анкета размещена!');
            isPlaced = true;
            updateStatusAndButtons();
        } else {
            warningMessage.textContent = 'Заполните все обязательные поля';
        }
    });

    // "Редактировать анкету"
    editButton.addEventListener('click', function () {
        if (!isEditing) enableEditMode();
    });

    // "Очистить"
    clearButton.addEventListener('click', function () {
        if (isEditing) {
            questionnaireData = {title: "", description: "", games: [], goal: "", time: "", contacts: ""};
            displayQuestionnaire();
            updateStatusAndButtons();
        }
    });

    // "Отмена"
    cancelButton.addEventListener('click', function () {
        if (isEditing) disableEditMode(false);
    });

    // "Сохранить изменения"
    saveButton.addEventListener('click', function () {
        if (isEditing) disableEditMode(true);
    });

    // "Добавить игру"
    addGameButton.addEventListener('click', function () {
        const newGame = newGameInput.value.trim();
        if (newGame) {
            questionnaireData.games.push(newGame);
            displayQuestionnaire(true);
            newGameInput.value = '';
            updateStatusAndButtons();
        }
    });
});