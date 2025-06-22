import {showError} from './errors.js';

// Глобальный объект для хранения состояния
const stateHeader = {
    isAuthenticated: false,
    pendingUsers: [],
    notifications: [],
    hasNewNotifications: false,
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

    // Ждём, пока <header> появится в DOM
    await waitForElement('header', 5000);

    updateHeaderTitle();

    // Проверяем, находимся ли на странице анкет (для фильтров)
    const isQuestionnairesPage = window.location.pathname.toLowerCase().includes('/questionnaires');
    console.log('isQuestionnairesPage:', isQuestionnairesPage, 'pathname:', window.location.pathname);
    if (isQuestionnairesPage) {
        const header = document.querySelector('header');
        if (!header) {
            console.error('Элемент <header> не найден');
            return;
        }

        // Ждём, пока .auth-wrapper появится
        await waitForElement('.auth-wrapper', 5000, header);

        const authWrapper = header.querySelector('.auth-wrapper');
        if (!authWrapper) {
            console.error('Элемент .auth-wrapper не найден в <header>');
            return;
        }

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
        header.insertBefore(filterContainer, authWrapper);
        console.log('Filter container добавлен:', document.querySelector('.filter-container'));
    }

    // Загружаем имя пользователя и статус аутентификации
    await loadAndRenderUserName();
    await loadNotifications();
    updateNotificationBell();
    setupNotificationBellListener();
}

function updateHeaderTitle() {
    const headerTitle = document.querySelector('.header-title');
    const path = window.location.pathname.toLowerCase();

    const titleMap = {
        '/questionnaires': 'Поиск анкет',
        '/my_questionnaires': 'Моя анкета',
        '/pending_requests': 'Мои заявки',
        '/my_teams': 'Мои команды',
    };

    if (headerTitle && titleMap[path]) {
        headerTitle.textContent = titleMap[path];
    }
}

// Функция ожидания элемента в DOM
async function waitForElement(selector, timeout = 5000, parent = document) {
    const start = Date.now();
    while (Date.now() - start < timeout) {
        const element = parent.querySelector(selector);
        if (element) return element;
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    console.warn(`Элемент ${selector} не найден за ${timeout} мс`);
    return null;
}

async function loadAndRenderUserName() {
    try {
        const response = await fetch('/data/profile');
        stateHeader.isAuthenticated = response.headers.get('X-Is-Authenticated') === 'true';
        if (response.status === 401) stateHeader.isAuthenticated = false;
        const usernameElement = document.getElementById('user-name-header');
        const profileElements = document.querySelectorAll('.auth-status.user-name');
        const profileImage = document.querySelector('.header-profile-image');

        if (profileImage) {
            profileImage.style.backgroundImage = stateHeader.isAuthenticated
                ? `url('../img/default-profile.jpg')`
                : `url('../img/unauthenticated-profile.jpg')`;
        } else {
            console.warn('Элемент .header-profile-image не найден');
        }

        // Обновляем видимость ссылок "Моя анкета", "Мои заявки" и "Мои команды"
        const myQuestionnairesLink = document.querySelector('.my-q');
        const pendingRequestsLink = document.querySelector('.pending-requests');
        const myTeamsLink = document.querySelector('.my-teams');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = stateHeader.isAuthenticated ? 'flex' : 'none';
        }
        if (pendingRequestsLink) {
            pendingRequestsLink.style.display = stateHeader.isAuthenticated ? 'flex' : 'none';
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
            if (pendingRequestsLink) {
                pendingRequestsLink.style.display = 'none';
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
        const pendingRequestsLink = document.querySelector('.pending-requests');
        const myTeamsLink = document.querySelector('.my-teams');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = 'none';
        }
        if (pendingRequestsLink) {
            pendingRequestsLink.style.display = 'none';
        }
        if (myTeamsLink) {
            myTeamsLink.style.display = 'none';
        }
        const profileImage = document.querySelector('.header-profile-image');
        if (profileImage) {
            profileImage.style.backgroundImage = `url('../img/unauthenticated-profile.jpg')`;
        }
    }
}

async function loadNotifications() {
    if (!stateHeader.isAuthenticated) {
        stateHeader.notifications = [];
        stateHeader.pendingUsers = [];
        stateHeader.hasNewNotifications = false;
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
                console.warn('Неожиданный формат данных /data/selfapplications:', selfAppsData);
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
            stateHeader.hasNewNotifications = pendingData.HasNew === true;

            if (pendingData && Array.isArray(pendingData.Requests)) {
                pendingNotifications = pendingData.Requests;
            } else if (Array.isArray(pendingData)) {
                pendingNotifications = pendingData;
            } else if (pendingData && Array.isArray(pendingData.Pending)) {
                pendingNotifications = pendingData.Pending;
            } else if (pendingData && Array.isArray(pendingData.pending)) {
                pendingNotifications = pendingData.pending;
            } else {
                console.warn('Неожиданный формат данных /team/pending:', pendingData);
            }
        } else {
            console.error('Ошибка при загрузке pending:', pendingResponse.status);
        }

        const filteredNotifications = pendingNotifications
            .filter(notification => {
                const appId = String(notification.applicationId || notification.ApplicationId || notification.applicationId);
                const match = selfApplicationIds.includes(appId);
                return match;
            })
            .map(notification => ({
                userId: notification.userId || notification.UserId,
                applicationId: notification.applicationId || notification.ApplicationId,
            }));

        // Получение юзернеймов для всех уникальных user IDs
        const uniqueUserIds = [...new Set(filteredNotifications.map(n => n.userId))];
        const usernameRequests = uniqueUserIds.map(async userId => {
            try {
                const response = await fetch(`/data/getusernamebyid/${userId}`);
                if (!response.ok) return {userId, username: null};
                const data = await response.json();
                return {userId, username: data.username || null};
            } catch (e) {
                return {userId, username: null};
            }
        });

        // Ожидание всех запросов и создание маппинга
        const usernameResults = await Promise.all(usernameRequests);
        const usernameMap = usernameResults.reduce((acc, curr) => {
            acc[curr.userId] = curr.username;
            return acc;
        }, {});

        // Обновление состояния с юзернеймами
        stateHeader.notifications = filteredNotifications.map(n => ({
            ...n,
            username: usernameMap[n.userId] || null,
        }));

        stateHeader.pendingUsers = filteredNotifications.map(n => n.userId);

    } catch (error) {
        console.error('Ошибка при загрузке уведомлений:', error);
        stateHeader.notifications = [];
        stateHeader.pendingUsers = [];
        stateHeader.hasNewNotifications = false;
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
        if (!notification.username) {
            showError(`Ошибка при получении юзернейма пользователя с ID ${notification.userId}`);
            return;
        }

        const notificationItem = document.createElement('div');
        notificationItem.className = 'notification-item';
        notificationItem.innerHTML = `
            <p class="notification-message">
                <a class="clickable-player">Игрок</a> готов стать частью твоей команды!
            </p>
            <div class="notification-actions">
                <button class="accept">Принять</button>
                <button class="deny">Отклонить</button> 
            </div>
        `;

        // Безопасная вставка данных
        const playerLink = notificationItem.querySelector('.clickable-player');
        playerLink.href = `/profile/${encodeURIComponent(notification.username)}`;
        playerLink.textContent = notification.username;

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
                    // Повторно загружаем уведомления, чтобы обновить HasNew
                    await loadNotifications();
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
                    await loadNotifications();
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
        bellIcon.style.display = stateHeader.isAuthenticated ? 'block' : 'none';

        if (stateHeader.hasNewNotifications) {
            bellIcon.src = '../img/bell-active.svg';
            bellIcon.classList.add('active');
        } else {
            bellIcon.src = '../img/bell.svg';
            bellIcon.classList.remove('active');
        }
    } else {
        console.warn('Элемент .header-notification-bell не найден');
    }
}

function setupNotificationBellListener() {
    const bellIcon = document.querySelector('.header-notification-bell');
    const notificationsPanel = document.querySelector('.notifications-panel');
    if (bellIcon && notificationsPanel) {
        bellIcon.addEventListener('click', async () => {
            const isOpening = notificationsPanel.style.display === 'none' || notificationsPanel.style.display === '';
            notificationsPanel.style.display = isOpening ? 'block' : 'none';

            if (isOpening) {
                try {
                    const response = await fetch('/team/read', {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                    });
                    if (response.ok) {
                        await loadNotifications();
                        updateNotificationBell();
                    } else {
                        console.error('Ошибка при выполнении запроса /team/read:', response.status);
                    }
                } catch (error) {
                    console.error('Ошибка при отправке запроса /team/read:', error);
                }
            }
        });
        document.addEventListener('click', (event) => {
            if (!notificationsPanel.contains(event.target) && !bellIcon.contains(event.target)) {
                notificationsPanel.style.display = 'none';
            }
        });
    } else {
        console.warn('Не найдены элементы для настройки колокольчика: bellIcon=', !!bellIcon, 'notificationsPanel=', !!notificationsPanel);
    }
}

export {loadHeader, showNotificationMessage};

