import { applyFilters } from '../js/questionnaires.js';

const currentFilter = {
    games: [], // Массив выбранных игр
    purpose: null, // ID выбранной цели
};

export function getCurrentFilter() {
    return currentFilter;
}

export let applyFiltersButton;

// Храним оригинальный массив объектов для доступа к Id
let gameObjects = [];

export async function initFilters() {
    try {
        // Ждём контейнеры фильтров
        const filterContainer = await waitForElement('.filter-container', 5000);
        const selectedFiltersContainer = await waitForElement('.selected-filters', 5000);
        if (!filterContainer || !selectedFiltersContainer) {
            console.error('Не найдены контейнеры для фильтров:', { filterContainer, selectedFiltersContainer });
            return;
        }

        const filterTemplate = await loadFilterTemplate();
        if (!filterTemplate) {
            console.error('Не удалось загрузить шаблон фильтров');
            return;
        }

        try {
            const response = await fetch('/data/games', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' },
            });
            if (response.ok) {
                gameObjects = await response.json();
            } else {
                console.error('Ошибка загрузки игр с API:', response.status);
                gameObjects = [{ Name: 'Fallback Game' }]; // Заглушка
            }
        } catch (error) {
            console.error('Ошибка при выполнении запроса /data/games:', error);
            gameObjects = [{ Name: 'Fallback Game' }]; // Заглушка
        }

        if (!Array.isArray(gameObjects) || !gameObjects.every(game => game.Name && typeof game.Name === 'string')) {
            console.error('API /data/games вернул некорректный формат данных, ожидался массив объектов с полем Name:', gameObjects);
            return;
        }

        const games = gameObjects.map(game => game.Name);

        // TODO: Заменить статические данные на загрузку с API
        const purposes = [
            {id: 2, text: 'Пофаниться'},
            {id: 3, text: 'Посоревноваться'},
            {id: 4, text: 'Расслабиться'},
            {id: 5, text: 'Поиграть в сюжетную игру'},
            {id: 6, text: 'Для стриминга'},
            {id: 7, text: 'Тренировка'},
            {id: 8, text: 'Турнир'},
            {id: 1, text: 'Для заработка'},
        ];

        // Вставка шаблона фильтров в DOM
        const filterDropdown = filterTemplate.content.cloneNode(true).firstElementChild;
        filterContainer.appendChild(filterDropdown);

        // Получение элементов интерфейса фильтров после вставки шаблона
        const filterToggle = document.querySelector('.filter-toggle');
        const filterDropdownElement = document.querySelector('.filter-dropdown');
        const gamesFilter = filterDropdownElement.querySelector('.games-filter');
        const gamesList = gamesFilter.querySelector('.games-list');
        const gameSearch = gamesFilter.querySelector('.game-search');
        const purposeFilter = filterDropdownElement.querySelector('.purpose-filter');
        const clearFiltersButton = filterDropdownElement.querySelector('.clear-filters-button');
        applyFiltersButton = filterDropdownElement.querySelector('.apply-filters-button');

        const gamesPerPage = 15;
        let currentPage = 1;
        let filteredGames = games;

        // Инициализация фильтров игр и целей
        initGamesFilter();
        initPurposeFilter();
        setupEventListeners();

        // Загружает файл Filters.html
        async function loadFilterTemplate() {
            try {
                const response = await fetch('../pages/Filters.html');
                const html = await response.text();
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');
                return doc.getElementById('filter-template');
            } catch (error) {
                console.error('Ошибка загрузки шаблона фильтров:', error);
                return null;
            }
        }

        function initGamesFilter() {
            renderGamesList(filteredGames, currentPage);
            renderPagination(filteredGames);
        }

        // Отрисовывает список игр для текущей страницы
        function renderGamesList(gamesToRender, page) {
            const start = (page - 1) * gamesPerPage;
            const end = start + gamesPerPage;
            const paginatedGames = gamesToRender.slice(start, end);

            gamesList.innerHTML = ''; // Очистка текущего списка
            paginatedGames.forEach(game => {
                const option = document.createElement('div');
                option.className = 'filter-option';
                const isChecked = currentFilter.games.includes(game) ? 'checked' : '';
                const highlightedGame = highlightSearch(game, gameSearch.value);
                option.innerHTML = `
                    <input type="checkbox" id="game-${game}" value="${game}" ${isChecked}>
                    <label for="game-${game}">${highlightedGame}</label>
                `;
                gamesList.appendChild(option);
            });
        }

        // Подсвечивает совпадения в названиях игр при поиске
        function highlightSearch(game, searchText) {
            if (!searchText) return game;
            const regex = new RegExp(`(${searchText})`, 'gi');
            return game.replace(regex, '<span class="highlight">$1</span>');
        }

        // Отрисовывает кнопки пагинации для списка игр
        function renderPagination(gamesToRender) {
            const totalPages = Math.ceil(gamesToRender.length / gamesPerPage);
            const paginationContainer = gamesFilter.querySelector('.pagination');
            paginationContainer.innerHTML = '';

            if (totalPages > 1) {
                const prevButton = document.createElement('button');
                prevButton.innerHTML = `
                    <svg viewBox="0 0 24 24">
                        <path d="M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z"/>
                    </svg>
                `;
                prevButton.disabled = currentPage === 1;
                prevButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    if (currentPage > 1) {
                        currentPage--;
                        renderGamesList(filteredGames, currentPage);
                        renderPagination(filteredGames);
                    }
                });
                paginationContainer.appendChild(prevButton);

                const nextButton = document.createElement('button');
                nextButton.innerHTML = `
                    <svg viewBox="0 0 24 24">
                        <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
                    </svg>
                `;
                nextButton.disabled = currentPage === totalPages;
                nextButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    if (currentPage < totalPages) {
                        currentPage++;
                        renderGamesList(filteredGames, currentPage);
                        renderPagination(filteredGames);
                    }
                });
                paginationContainer.appendChild(nextButton);
            }
        }

        // Инициализирует фильтр целей, добавляя кнопки
        function initPurposeFilter() {
            purposes.forEach(purpose => {
                const option = document.createElement('div');
                option.className = 'filter-option';
                option.innerHTML = `
                    <input type="radio" name="purpose" id="purpose-${purpose.id}" value="${purpose.id}">
                    <label for="purpose-${purpose.id}">${purpose.text}</label>
                `;
                purposeFilter.appendChild(option);
            });
        }

        // Настраивает обработчики событий для интерфейса фильтров
        function setupEventListeners() {
            filterToggle.addEventListener('click', toggleFilters);
            document.addEventListener('click', closeFiltersOnClickOutside);
            gamesFilter.addEventListener('change', handleGameFilterChange);
            purposeFilter.addEventListener('change', handlePurposeFilterChange);
            clearFiltersButton.addEventListener('click', clearAllFilters);
            if (applyFiltersButton) {
                applyFiltersButton.addEventListener('click', () => {
                    // Закрываем окно фильтров
                    filterDropdownElement.classList.remove('active');
                    filterToggle.classList.remove('active');
                    // Вызываем applyFilters для обновления анкет
                    applyFilters();
                });
            } else {
                console.error('Кнопка apply-filters-button не найдена');
            }

            gameSearch.addEventListener('input', handleGameSearch);

            // Снятие выбора цели при повторном клике на метку
            purposeFilter.querySelectorAll('label').forEach(label => {
                label.addEventListener('click', (e) => {
                    const radio = document.getElementById(label.getAttribute('for'));
                    if (radio.checked) {
                        e.preventDefault();
                        radio.checked = false;
                        currentFilter.purpose = null;
                        updateSelectedFilters();
                        dispatchFilterChangeEvent();
                    }
                });
            });

            document.addEventListener('filterChanged', logFilters);
        }

        // Обрабатывает поиск игр по введенному тексту
        function handleGameSearch() {
            const searchText = gameSearch.value.toLowerCase();
            filteredGames = games.filter(game => game.toLowerCase().includes(searchText));
            currentPage = 1;
            renderGamesList(filteredGames, currentPage);
            renderPagination(filteredGames);
        }

        // Переключает видимость выпадающего меню фильтров
        function toggleFilters() {
            filterDropdownElement.classList.toggle('active');
            filterToggle.classList.toggle('active');
        }

        // Закрывает меню фильтров при клике вне контейнера
        function closeFiltersOnClickOutside(e) {
            if (!filterContainer.contains(e.target)) {
                filterDropdownElement.classList.remove('active');
                filterToggle.classList.remove('active');
            }
        }

        // Обрабатывает выбор/снятие игры в фильтре
        function handleGameFilterChange(e) {
            const game = e.target.value;
            if (e.target.checked) {
                currentFilter.games.push(game);
            } else {
                currentFilter.games = currentFilter.games.filter(g => g !== game);
            }
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        // Обрабатывает выбор цели в фильтре
        function handlePurposeFilterChange(e) {
            currentFilter.purpose = e.target.value;
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        // Сбрасывает все выбранные фильтры
        function clearAllFilters() {
            currentFilter.games = [];
            currentFilter.purpose = null;
            gamesFilter.querySelectorAll('input').forEach(input => input.checked = false);
            purposeFilter.querySelectorAll('input').forEach(input => input.checked = false);
            filteredGames = games;
            currentPage = 1;
            renderGamesList(filteredGames, currentPage);
            renderPagination(filteredGames);
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        function updateSelectedFilters() {
            selectedFiltersContainer.innerHTML = '';
            currentFilter.games.forEach(game => {
                const tag = createFilterTag(game, 'game', game);
                selectedFiltersContainer.appendChild(tag);
            });
            if (currentFilter.purpose) {
                const purposeIntId = Number(currentFilter.purpose);
                const purposeText = purposes.find(p => p.id === purposeIntId).text;
                const tag = createFilterTag(purposeText, 'purpose', currentFilter.purpose);
                selectedFiltersContainer.appendChild(tag);
            }
            setupRemoveFilterHandlers();
        }

        // Создает тег для выбранного фильтра с кнопкой удаления
        function createFilterTag(text, type, value) {
            const tag = document.createElement('div');
            tag.className = 'filter-tag';
            tag.innerHTML = `
                ${text}
                <span class="remove-filter" data-type="${type}" data-value="${value}">×</span>
            `;
            return tag;
        }

        // Настраивает обработчики для удаления фильтров по клику на крестик
        function setupRemoveFilterHandlers() {
            selectedFiltersContainer.querySelectorAll('.remove-filter').forEach(button => {
                button.addEventListener('click', (e) => {
                    e.stopPropagation();
                    const type = button.dataset.type;
                    const value = button.dataset.value;

                    if (type === 'game') {
                        currentFilter.games = currentFilter.games.filter(g => g !== value);
                        const checkbox = gamesFilter.querySelector(`input[value="${value}"]`);
                        if (checkbox) checkbox.checked = false;
                    } else if (type === 'purpose') {
                        currentFilter.purpose = null;
                        purposeFilter.querySelector(`input[value="${value}"]`).checked = false;
                    }

                    updateSelectedFilters();
                    dispatchFilterChangeEvent();
                });
            });
        }

        // Отправляет событие об изменении фильтров
        function dispatchFilterChangeEvent() {
            const event = new CustomEvent('filterChanged', {
                detail: { games: currentFilter.games, purpose: currentFilter.purpose },
            });
            document.dispatchEvent(event);
        }

        function logFilters() {
            console.log('Текущие фильтры:', currentFilter);
        }
    } catch (error) {
        console.error('Ошибка инициализации фильтров:', error);
    }
}

// Функция ожидания элемента в DOM
async function waitForElement(selector, timeout = 5000) {
    const start = Date.now();
    while (Date.now() - start < timeout) {
        const element = document.querySelector(selector);
        if (element) return element;
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    console.warn(`Элемент ${selector} не найден за ${timeout} мс`);
    return null;
}