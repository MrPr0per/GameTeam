document.addEventListener('DOMContentLoaded', function () {
    addEventListeners()
    loafProfileInfo()
});

function addEventListeners() {
    const tabs = document.querySelectorAll('.tab-link');
    const panes = document.querySelectorAll('.tab-pane');
    const textarea = document.getElementById('description');
    const editButton = document.getElementById('edit-button');
    const saveCancelButtons = document.getElementById('save-cancel-buttons');
    const saveButton = document.getElementById('save-button');
    const cancelButton = document.getElementById('cancel-button');
    const addGameButton = document.getElementById('add-game-button');
    const newGameInput = document.getElementById('new-game-input');
    let isEditing = false;
    let games = ['Майнкрафт', 'Дота'];
    let originalGames = [...games];
    let originalDescription = textarea.value;

    // Переключение вкладок
    tabs.forEach(tab => {
        tab.addEventListener('click', function () {
            tabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            panes.forEach(pane => pane.classList.remove('active'));
            const tabId = this.getAttribute('data-tab');
            document.getElementById(tabId).classList.add('active');
        });
    });

    // Динамическое изменение высоты штучки с описанием
    textarea.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = this.scrollHeight + 'px';
    });
    textarea.style.height = textarea.scrollHeight + 'px';

    // Кнопка "Редактировать профиль"
    editButton.addEventListener('click', function () {
        editButton.textContent = 'Редактирование';
        editButton.disabled = true;
        saveCancelButtons.style.display = 'flex';
        isEditing = true;
        originalGames = [...games];
        originalDescription = textarea.value;
        textarea.removeAttribute('readonly');
        renderGames();
        document.getElementById('add-game-section').style.display = 'flex';
    });

    // Кнопка "Готово"
    saveButton.addEventListener('click', function () {
        editButton.textContent = 'Редактировать профиль';
        editButton.disabled = false;
        saveCancelButtons.style.display = 'none';
        isEditing = false;
        textarea.setAttribute('readonly', true);
        renderGames();
        document.getElementById('add-game-section').style.display = 'none';
    });

    // Кнопка "Отмена"
    cancelButton.addEventListener('click', function () {
        editButton.textContent = 'Редактировать профиль';
        editButton.disabled = false;
        saveCancelButtons.style.display = 'none';
        isEditing = false;
        games = [...originalGames];
        textarea.value = originalDescription;
        textarea.setAttribute('readonly', true);
        renderGames();
        document.getElementById('add-game-section').style.display = 'none';
    });

    // Добавление новой игры
    addGameButton.addEventListener('click', function () {
        const newGame = newGameInput.value.trim();
        if (newGame) {
            games.push(newGame);
            renderGames();
            newGameInput.value = '';
        }
    });


    function renderGames() {
        const gamesList = document.getElementById('games-list');
        gamesList.innerHTML = '';
        games.forEach((game, index) => {
            const li = document.createElement('li');
            const gameSpan = document.createElement('span');
            gameSpan.className = 'game-name';
            gameSpan.textContent = game;
            li.appendChild(gameSpan);
            if (isEditing) {
                const removeButton = document.createElement('span');
                removeButton.textContent = '×';
                removeButton.className = 'remove-game';
                removeButton.addEventListener('click', () => {
                    games.splice(index, 1);
                    renderGames();
                });
                li.appendChild(removeButton);
            }
            gamesList.appendChild(li);
        });
    }

    renderGames();
}


function loafProfileInfo() {
    fetch('/data/profile')
        .then(r => {
            if (r.ok) {
                substituteProfileInfo(r.json())
            } else {
                showFetchProfileInfoErrorMessage(r)
            }
        })
}

function substituteProfileInfo(dataInfo) {
    const descriptionField = document.getElementById('description');
    const freeTimeTag = document.getElementById('free-time');
    const gamesContainer =;

}

function showFetchProfileInfoErrorMessage(r) {
    if (r.status === 401) {
        console.log('Вы неавторизованы')
    } else {
        console.log(`Не обработанная ошибка: ${r}`)
    }
}
