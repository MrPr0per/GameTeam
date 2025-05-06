let hasNotifications = false; 

function loadAndRenderUserName() {
    fetch('/data/profile')
        .then(r => {
            if (r.ok) {
                return r.json();
            } else if (r.status === 401) {
                window.location.href = '/register';
            } else {
                console.error('Ошибка при загрузке профиля', r);
            }
        })
        .then(json => {
            if (json !== undefined) {
                const name = json['Username'];
                document.querySelectorAll('.user-name').forEach(el => el.textContent = name);
            }
        })
        .catch(err => {
            console.error('Ошибка при получении имени пользователя:', err);
        });
}

function updateNotificationBell() {
    const bellIcon = document.querySelector('.header-notification-bell');
    if (bellIcon) {
        if (hasNotifications) {
            bellIcon.src = '../img/bell-active.svg';
            bellIcon.classList.add('active');
        } else {
            bellIcon.src = '../img/bell.svg';
            bellIcon.classList.remove('active');
        }
    }
}

// Выполняем функции сразу после загрузки скрипта
loadAndRenderUserName();
updateNotificationBell();