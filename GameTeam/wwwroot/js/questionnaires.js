document.addEventListener('DOMContentLoaded', function () {
    const questionnairesContainer = document.querySelector('.questionnaires-container');
    const modalTemplate = document.getElementById('modal-template');
    const loadMoreButton = document.getElementById('load-more-button');
    const loadMoreContainer = document.querySelector('.load-more-container');

    let offset = 0;
    const limit = 5;
    let loading = false;
    let endReached = false;

    loadQuestionnaires();

    loadMoreButton.addEventListener('click', function () {
        if (loading || endReached) return;
        loadQuestionnaires();
    });

    function loadQuestionnaires() {
        loading = true;

        if (loadMoreButton) {
            loadMoreButton.textContent = 'Загрузка...';
            loadMoreButton.disabled = true;
        }

        fetch(`https://localhost:7179/data/applications/${offset}/${offset + limit}`)
            .then(response => response.json())
            .then(data => {
                if (!Array.isArray(data) || data.length === 0) {
                    endReached = true;
                    if (loadMoreButton) {
                        loadMoreButton.remove();
                    }
                    return;
                }

                const questionnaires = data.map(item => ({
                    title: item.Title,
                    description: item.Description,
                    games: item.Games.map(g => g.Name),
                    purpose: getPurposeText(item.PurposeId),
                    availability: formatAvailabilities(item.Availabilities),
                    contacts: item.Contacts
                }));

                questionnaires.forEach(q => {
                    const questionnaireDiv = document.createElement('div');
                    questionnaireDiv.className = 'questionnaire';

                    const title = document.createElement('h2');
                    title.textContent = q.title;

                    const description = document.createElement('p');
                    description.textContent = q.description;

                    const button = document.createElement('button');
                    button.className = 'filled-button';
                    button.textContent = 'Подробнее';
                    button.addEventListener('click', () => openModal(q));

                    questionnaireDiv.appendChild(title);
                    questionnaireDiv.appendChild(description);
                    questionnaireDiv.appendChild(button);

                    questionnairesContainer.appendChild(questionnaireDiv);
                });

                offset += data.length;

                if (data.length < limit && loadMoreButton) {
                    loadMoreButton.remove();
                    endReached = true;
                }
            })
            .catch(error => {
                console.error("Ошибка при загрузке анкет:", error);
            })
            .finally(() => {
                loading = false;
                if (!endReached && loadMoreButton) {
                    loadMoreButton.textContent = 'Загрузить ещё';
                    loadMoreButton.disabled = false;
                }
            });
    }

    function openModal(questionnaire) {
        const modalOverlay = modalTemplate.content.cloneNode(true).firstElementChild;
        const modalContent = modalOverlay.querySelector('.modal-content');

        modalContent.querySelector('h2').textContent = questionnaire.title;
        modalContent.querySelector('.modal-description').textContent = questionnaire.description;
        modalContent.querySelector('.modal-games').innerHTML = questionnaire.games.map(g => `<li>${g}</li>`).join('');
        modalContent.querySelector('.modal-purpose').textContent = questionnaire.purpose;
        modalContent.querySelector('.modal-availability').innerHTML = questionnaire.availability;
        modalContent.querySelector('.modal-contacts').textContent = questionnaire.contacts;

        modalContent.querySelector('.close-button').addEventListener('click', () => closeModal(modalOverlay));
        modalOverlay.addEventListener('click', (e) => {
            if (e.target === modalOverlay) {
                closeModal(modalOverlay);
            }
        });

        document.body.appendChild(modalOverlay);
    }

    function closeModal(modalOverlay) {
        modalOverlay.remove();
    }

    function getPurposeText(id) {
        const purposes = {
            1: "Поиск команды",
            2: "Поиск игроков",
            3: "Совместная игра"
        };
        return purposes[id] || "Неизвестная цель";
    }

    function formatAvailabilities(availabilities) {
        const days = ["Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота"];

        if (!availabilities || availabilities.length === 0) return "Не указано";

        return availabilities.map(a => {
            const dayName = days[a.DayOfWeek] || "Неизвестный день";
            const startHour = String(a.StartTime.Hour).padStart(2, '0');
            const startMinute = String(a.StartTime.Minute).padStart(2, '0');
            const endHour = String(a.EndTime.Hour).padStart(2, '0');
            const endMinute = String(a.EndTime.Minute).padStart(2, '0');

            return `${dayName}: ${startHour}:${startMinute} – ${endHour}:${endMinute}`;
        }).join('<br>');
    }
});
