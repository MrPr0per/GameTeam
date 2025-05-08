// Глобальный объект для хранения состояния
const stateHeader = {
    isAuthenticated: false,
    pendingUsers: [],
    notifications: [],
};

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
    await loadNotifications();
    updateNotificationBell();
    setupNotificationBellListener();
}

async function loadAndRenderUserName() {
    try {
        const response = await fetch('/data/profile');
        stateHeader.isAuthenticated = response.headers.get('X-Is-Authenticated') === 'true';
        if (response.status === 401) stateHeader.isAuthenticated = false;
        const usernameElement = document.getElementById('user-name-header');
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
                usernameElement.textContent = json['Username'];
            } else {
                usernameElement.textContent = 'Вход не выполнен';
            }
        } else {
            usernameElement.textContent = 'Вход не выполнен';
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
        document.getElementById('user-name-header').textContent = 'Вход не выполнен';
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

async function loadNotifications() {
    if (!stateHeader.isAuthenticated) {
        stateHeader.notifications = [];
        stateHeader.pendingUsers = [];
        return;
    }

    try {
        const selfAppsResponse = await fetch('/data/selfapplications', {
            method: 'GET',
            headers: {'Content-Type': 'application/json'},
        });
        let selfApplicationIds = [];
        if (selfAppsResponse.ok) {
            const selfAppsData = await selfAppsResponse.json();
            if (Array.isArray(selfAppsData)) {
                selfApplicationIds = selfAppsData.map(app => String(app.Id || app.id));
            } else if (selfAppsData && Array.isArray(selfAppsData.applications)) {
                selfApplicationIds = selfAppsData.applications.map(app => String(app.Id || app.id));
            } else {
                console.warn('Неожиданный формат данных /data/selfapplications');
            }
        } else {
            console.error('Ошибка при загрузке selfapplications:', selfAppsResponse.status);
        }

        const pendingResponse = await fetch('/team/pending', {
            method: 'GET',
            headers: {'Content-Type': 'application/json'},
        });
        let pendingNotifications = [];
        if (pendingResponse.ok) {
            const pendingData = await pendingResponse.json();
            if (pendingData && Array.isArray(pendingData.Requests)) {
                pendingNotifications = pendingData.Requests;
            } else if (Array.isArray(pendingData)) {
                pendingNotifications = pendingData;
            } else if (pendingData && Array.isArray(pendingData.Pending)) {
                pendingNotifications = pendingData.Pending;
            } else if (pendingData && Array.isArray(pendingData.pending)) {
                pendingNotifications = pendingData.pending;
            } else {
                console.warn('Неожиданный формат данных /team/pending');
            }
        } else {
            console.error('Ошибка при загрузке pending:', pendingResponse.status);
        }

        stateHeader.notifications = pendingNotifications
            .filter(notification => {
                const appId = String(notification.applicationId || notification.ApplicationId);
                const match = selfApplicationIds.includes(appId);
                return match;
            })
            .map(notification => ({
                userId: notification.userId || notification.UserId,
                applicationId: notification.applicationId || notification.ApplicationId,
            }));
        stateHeader.pendingUsers = stateHeader.notifications.map(notification => notification.userId);
    } catch (error) {
        console.error('Ошибка при загрузке уведомлений:', error);
        stateHeader.notifications = [];
        stateHeader.pendingUsers = [];
    }

    renderNotifications();
}

function renderNotifications() {
    const notificationsContent = document.querySelector('.notifications-content');
    const noNotifications = document.querySelector('.no-notifications');
    if (!notificationsContent) return;

    notificationsContent.innerHTML = '';
    notificationsContent.appendChild(noNotifications);

    if (stateHeader.notifications.length === 0) {
        noNotifications.textContent = 'Затишье перед бурей — новых заявок нет! 😴';
        noNotifications.style.display = 'block';
        return;
    }

    noNotifications.style.display = 'none';

    stateHeader.notifications.forEach(notification => {
        const notificationItem = document.createElement('div');
        notificationItem.className = 'notification-item';
        notificationItem.innerHTML = `
            <p class="notification-message">
                <span class="clickable-player">Игрок</span> готов стать частью твоей команды!
            </p>
            <div class="notification-actions">
                <button class="accept">Принять</button>
                <button class="deny">Отклонить</button> 
            </div>
        `;
        const playerSpan = notificationItem.querySelector('.clickable-player');
        playerSpan.dataset.userId = notification.userId;
        playerSpan.addEventListener('click', () => {
            // Здесь будет логика перехода на профиль игрока в будущем
        });

        const acceptButton = notificationItem.querySelector('.accept');
        const denyButton = notificationItem.querySelector('.deny');

        acceptButton.addEventListener('click', async () => {
            try {
                const response = await fetch(`/team/approve/${notification.userId}/${notification.applicationId}`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                });
                if (response.ok) {
                    showNotificationMessage('Выбор сделан!');
                    stateHeader.notifications = stateHeader.notifications.filter(
                        n => n.userId !== notification.userId || n.applicationId !== notification.applicationId,
                    );
                    renderNotifications();
                    updateNotificationBell();
                } else {
                    showNotificationMessage('Ошибка при подтверждении.', true);
                }
            } catch (error) {
                console.error('Ошибка при подтверждении:', error);
                showNotificationMessage('Ошибка при подтверждении.', true);
            }
        });

        denyButton.addEventListener('click', async () => {
            try {
                const response = await fetch(`/team/deny/${notification.userId}/${notification.applicationId}`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                });
                if (response.ok) {
                    showNotificationMessage('Выбор сделан!');
                    stateHeader.notifications = stateHeader.notifications.filter(
                        n => n.userId !== notification.userId || n.applicationId !== notification.applicationId,
                    );
                    renderNotifications();
                    updateNotificationBell();
                } else {
                    showNotificationMessage('Ошибка при отклонении.', true);
                }
            } catch (error) {
                console.error('Ошибка при отклонении:', error);
                showNotificationMessage('Ошибка при отклонении.', true);
            }
        });

        notificationsContent.appendChild(notificationItem);
    });
}

function showNotificationMessage(message, isError = false) {
    const notification = document.createElement('div');
    notification.className = `notification ${isError ? 'error' : 'success'}`;
    notification.textContent = message;
    document.body.appendChild(notification);
    setTimeout(() => notification.remove(), 3000);
}

function updateNotificationBell() {
    const bellIcon = document.querySelector('.header-notification-bell');
    if (bellIcon) {
        if (!stateHeader.isAuthenticated || stateHeader.notifications.length === 0) {
            bellIcon.style.display = stateHeader.isAuthenticated ? 'block' : 'none';
            bellIcon.src = '../img/bell.svg';
            bellIcon.classList.remove('active');
        } else {
            bellIcon.style.display = 'block';
        }
        bellIcon.src = '../img/bell-active.svg';
        bellIcon.classList.add('active');
    }
}

function setupNotificationBellListener() {
    const bellIcon = document.querySelector('.header-notification-bell');
    const notificationsPanel = document.querySelector('.notifications-panel');
    if (bellIcon && notificationsPanel) {
        bellIcon.addEventListener('click', () => {
            notificationsPanel.style.display = notificationsPanel.style.display === 'none' ? 'block' : 'none';
        });
        document.addEventListener('click', (event) => {
            if (!notificationsPanel.contains(event.target) && !bellIcon.contains(event.target)) {
                notificationsPanel.style.display = 'none';
            }
        });
    }
}

export {loadHeader};