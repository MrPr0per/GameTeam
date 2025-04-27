document.addEventListener('DOMContentLoaded', async function () {
    try {
        // Загрузка шаблона фильтров из Filters.html
        const filterTemplate = await loadFilterTemplate();


        const filterContainer = document.querySelector('.filter-container');
        const selectedFiltersContainer = document.querySelector('.selected-filters');
        let selectedGames = []; // Массив выбранных игр
        let selectedPurpose = null; // ID выбранной цели

        // TODO: Заменить статические данные на загрузку с API
        const games = [
            "КВН",
            "Шашки",
            "Сумо",
            "Супер-корова",
            "Танчики",
            "Башенки",
            "Нарды",
            "Counter-Strike 2",
            "Dota 2",
            "Дурак онлайн",
            "Counter strike 2",
            "Rainbow six siege",
            "asdfasdfasdg",
            "aaaaaaa",
            "Русская рулетка",
            "Valorant",
            "Триатлон",
            "agasdg",
            "Королевская битва",
            "clash royale",
            "салочки",
            "Counter strike",
            "counter strike",
            "dota 2",
            "52$ В ДЕНЬ",
            "123123 123",
            "skylab24.net",
            "h",
            "efg",
            "eg",
            "jn",
            "Войнушки"
        ];

        // TODO: Заменить статические данные на загрузку с API
        const purposes = [
            { id: 1, text: 'Цель 1' },
            { id: 2, text: 'Цель 2' },
            { id: 3, text: 'Цель 3' },
            { id: 4, text: 'Цель 4' },
            { id: 5, text: 'Цель 5' },
            { id: 6, text: 'Цель 6' },
            { id: 7, text: 'Цель 7' },
            { id: 8, text: 'Цель 8' }
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
        const applyFiltersButton = filterDropdownElement.querySelector('.apply-filters-button');
        const paginationContainer = gamesFilter.querySelector('.pagination');
        const loadingOverlay = gamesFilter.querySelector('.loading-overlay');

        const gamesPerPage = 15; // Количество игр на странице
        let currentPage = 1; // Текущая страница
        let filteredGames = games; // Отфильтрованный список игр

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
                const isChecked = selectedGames.includes(game) ? 'checked' : '';
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
                        showLoading();
                        setTimeout(() => {
                            currentPage--;
                            renderGamesList(filteredGames, currentPage);
                            renderPagination(filteredGames);
                            hideLoading();
                        }, 500);
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
                        showLoading();
                        setTimeout(() => {
                            currentPage++;
                            renderGamesList(filteredGames, currentPage);
                            renderPagination(filteredGames);
                            hideLoading();
                        }, 500);
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
            applyFiltersButton.addEventListener('click', applyFilters);

           
            let timeout;
            gameSearch.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(handleGameSearch, 300);
            });

            // Снятие выбора цели при повторном клике на метку
            purposeFilter.querySelectorAll('label').forEach(label => {
                label.addEventListener('click', (e) => {
                    const radio = document.getElementById(label.getAttribute('for'));
                    if (radio.checked) {
                        e.preventDefault();
                        radio.checked = false;
                        selectedPurpose = null;
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
            showLoading();
            setTimeout(() => {
                renderGamesList(filteredGames, currentPage);
                renderPagination(filteredGames);
                hideLoading();
            }, 500);
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
                selectedGames.push(game);
            } else {
                selectedGames = selectedGames.filter(g => g !== game);
            }
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        // Обрабатывает выбор цели в фильтре
        function handlePurposeFilterChange(e) {
            selectedPurpose = e.target.value;
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        // Сбрасывает все выбранные фильтры
        function clearAllFilters() {
            selectedGames = [];
            selectedPurpose = null;
            gamesFilter.querySelectorAll('input').forEach(input => input.checked = false);
            purposeFilter.querySelectorAll('input').forEach(input => input.checked = false);
            filteredGames = games;
            currentPage = 1;
            showLoading();
            setTimeout(() => {
                renderGamesList(filteredGames, currentPage);
                renderPagination(filteredGames);
                hideLoading();
            }, 500);
            updateSelectedFilters();
            dispatchFilterChangeEvent();
        }

        // Применяет фильтры и скрывает меню
        function applyFilters() {
            filterDropdownElement.classList.remove('active');
            filterToggle.classList.remove('active');
        }

        // Показывает "загрузка.."
        function showLoading() {
            loadingOverlay.classList.add('active');
        }

        // Скрывает "загрузка.."
        function hideLoading() {
            loadingOverlay.classList.remove('active');
        }

        // Обновляет отображение выбранных фильтров 
        function updateSelectedFilters() {
            selectedFiltersContainer.innerHTML = '';
            selectedGames.forEach(game => {
                const tag = createFilterTag(game, 'game', game);
                selectedFiltersContainer.appendChild(tag);
            });
            if (selectedPurpose) {
                const purposeText = purposes.find(p => p.id == selectedPurpose).text;
                const tag = createFilterTag(purposeText, 'purpose', selectedPurpose);
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
                        selectedGames = selectedGames.filter(g => g !== value);
                        const checkbox = gamesFilter.querySelector(`input[value="${value}"]`);
                        if (checkbox) checkbox.checked = false;
                    } else if (type === 'purpose') {
                        selectedPurpose = null;
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
                detail: { games: selectedGames, purpose: selectedPurpose }
            });
            document.dispatchEvent(event);
        }

        // тестим тестим в консоли
        function logFilters() {
            console.log('Текущие фильтры:', { games: selectedGames, purpose: selectedPurpose });
        }

        // TODO: Добавить отправку выбранных фильтров на сервер


    } catch (error) {
        console.error('Ошибка инициализации фильтров:', error);
    }
});