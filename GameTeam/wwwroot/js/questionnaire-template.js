export function createQuestionnaire(data) {
    // Загружаем HTML-шаблон
    return fetch('../pages/Questionnaire-template.html')
        .then(response => response.text())
        .then(template => { 
            // Создаём DOM-элемент из шаблона
            const parser = new DOMParser();
            const doc = parser.parseFromString(template, 'text/html');
            const questionnaire = doc.querySelector('.questionnaire');

            // Заполняем данными
            questionnaire.querySelector('.questionnaire-title').textContent = data.title;
            questionnaire.querySelector('.questionnaire-description').textContent = data.description;

            // Игры
            const gamesList = questionnaire.querySelector('.games-list');
            data.games.forEach(game => {
                const li = document.createElement('li');
                li.className = 'game-item';
                li.innerHTML = `<span class="game-name">${game}</span>`;
                gamesList.appendChild(li);
            });

            // Цель
            questionnaire.querySelector('.questionnaire-purpose').textContent = data.purpose;

            // Время
            questionnaire.querySelector('.availability').innerHTML = data.availability;

            return questionnaire;
        });
}