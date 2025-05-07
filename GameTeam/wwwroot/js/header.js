// Глобальный объект для хранения состояния
const stateHeader = {
    isAuthenticated: false
};

let hasNotifications = true;

async function loadHeader() {
    const response = await fetch('../pages/Header.html');
    const headerHtml = await response.text();
    const layout = document.querySelector('.layout') || document.getElementById('header-placeholder');
    if (layout) {
        layout.insertAdjacentHTML('beforeend', headerHtml);
    } else {
        console.error('Контейнер для header не найден');
        return;
    }

    // Проверяем, находимся ли на странице анкет (для фильтров)
    const isQuestionnairesPage = window.location.pathname.includes('/questionnaires');
    if (isQuestionnairesPage) {
        const filterContainer = document.createElement('div');
        filterContainer.className = 'filter-container';
        filterContainer.innerHTML = `
            <div class="filter-toggle">Фильтры
                <svg class="chevron" viewBox="0 0 24 24">
                    <path d="M7 10l5 5 5-5z" />
                </svg>
            </div>
            <div class="selected-filters"></div>
        `;
        const header = document.querySelector('header');
        if (header && header.querySelector('.auth-wrapper')) {
            header.insertBefore(filterContainer, header.querySelector('.auth-wrapper'));
        }
    }

    // Загружаем имя пользователя и статус аутентификации
    await loadAndRenderUserName();
    // Обновляем колокольчик после получения статуса аутентификации
    updateNotificationBell();
}

async function loadAndRenderUserName() {
    try {
        const response = await fetch('/data/profile');
        stateHeader.isAuthenticated = response.headers.get('X-Is-Authenticated') === 'true';
        const userNameElements = document.querySelectorAll('.user-name');
        const profileElements = document.querySelectorAll('.auth-status.user-name');

        // Обновляем видимость ссылок "Мои анкеты" и "Мои команды"
        const myQuestionnairesLink = document.querySelector('.my-q');
        const myTeamsLink = document.querySelector('.my-teams');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = stateHeader.isAuthenticated ? 'flex' : 'none';
        }
        if (myTeamsLink) {
            myTeamsLink.style.display = stateHeader.isAuthenticated ? 'flex' : 'none';
        }

        // Обновляем ссылку профиля
        if (!stateHeader.isAuthenticated) {
            profileElements.forEach(el => el.href = '../pages/Register.html');
        } else {
            profileElements.forEach(el => el.href = '../pages/Profile.html');
        }

        if (response.ok) {
            const json = await response.json();
            if (stateHeader.isAuthenticated && json && json['Username']) {
                userNameElements.forEach(el => el.textContent = json['Username']);
            } else {
                userNameElements.forEach(el => el.textContent = 'Вход не выполнен');
            }
        } else {
            userNameElements.forEach(el => el.textContent = 'Вход не выполнен');
            if (myQuestionnairesLink) {
                myQuestionnairesLink.style.display = 'none';
            }
            if (myTeamsLink) {
                myTeamsLink.style.display = 'none';
            }
            throw new Error('Profile load error');
        }
    } catch (err) {
        console.error('Ошибка при получении имени пользователя:', err);
        document.querySelectorAll('.user-name').forEach(el => el.textContent = 'Вход не выполнен');
        const myQuestionnairesLink = document.querySelector('.my-q');
        const myTeamsLink = document.querySelector('.my-teams');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = 'none';
        }
        if (myTeamsLink) {
            myTeamsLink.style.display = 'none';
        }
    }
}

function updateNotificationBell() {
    const bellIcon = document.querySelector('.header-notification-bell');
    if (bellIcon) {
        if (!stateHeader.isAuthenticated) {
            bellIcon.style.display = 'none';
        } else {
            // Показываем и обновляем колокольчик, если вход выполнен
            bellIcon.style.display = 'block';
            if (hasNotifications) {
                bellIcon.src = '../img/bell-active.svg';
                bellIcon.classList.add('active');
            } else {
                bellIcon.src = '../img/bell.svg';
                bellIcon.classList.remove('active');
            }
        }
    }
}

export { loadHeader };