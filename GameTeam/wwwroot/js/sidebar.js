// Глобальный объект для хранения состояния
const state = {
    isAuthenticated: false
};

async function updateSidebarAuthState() {
    try {
        const response = await fetch('/data/profile');
        state.isAuthenticated = response.headers.get('X-Is-Authenticated') === 'true';

        // Находим ссылку "Мои анкеты"
        const myQuestionnairesLink = document.querySelector('a[href="/My_questionnaires"]');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = state.isAuthenticated ? 'flex' : 'none';
        }
    } catch (err) {
        console.error('Ошибка при проверке статуса аутентификации:', err);
        // В случае ошибки скрываем ссылку "Мои анкеты"
        const myQuestionnairesLink = document.querySelector('a[href="/My_questionnaires"]');
        if (myQuestionnairesLink) {
            myQuestionnairesLink.style.display = 'none';
        }
    }
}

// Выполняем проверку сразу после загрузки скрипта
updateSidebarAuthState();