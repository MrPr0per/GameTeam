document.addEventListener('DOMContentLoaded', function () {
    const questionnairesContainer = document.querySelector('.questionnaires-container');
    const modalTemplate = document.getElementById('modal-template');

    // Тестим
    const questionnaires = [
        {
            title: "Анкетка",
            description: "Описание ".repeat(70),
            games: ["Кря", "Кря", "Кря"],
            purpose: "Играть по кайфу",
            availability: "с 18:00 до 22:00 хз как выводить",
            contacts: "КонтурТолк: Имя Фамилия"
        },
        {
            title: "Анкетка Еще Одна",
            description: "Описание ".repeat(10),
            games: ["Кря", "Кря", "Кря"],
            purpose: "Играть по кайфу",
            availability: "с 18:00 до 22:00 хз как выводить",
            contacts: "Тг: @....."
        }


    ];

    questionnaires.forEach(q => {
        const questionnaireDiv = document.createElement('div');
        questionnaireDiv.className = 'questionnaire';

        // Тут данные для краткого описания анкеты
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

    function openModal(questionnaire) {
        const modalOverlay = modalTemplate.content.cloneNode(true).firstElementChild;
        const modalContent = modalOverlay.querySelector('.modal-content');
        const title = modalContent.querySelector('h2');
        const description = modalContent.querySelector('.modal-description');
        const gamesList = modalContent.querySelector('.modal-games');
        const purpose = modalContent.querySelector('.modal-purpose');
        const availability = modalContent.querySelector('.modal-availability'); 
        const contacts = modalContent.querySelector('.modal-contacts');
        const closeButton = modalContent.querySelector('.close-button');


        // Тут данные для полной анкеты
        title.textContent = questionnaire.title;
        description.textContent = questionnaire.description;
        gamesList.innerHTML = questionnaire.games.map(game => `<li>${game}</li>`).join('');
        purpose.textContent = questionnaire.purpose;
        availability.textContent = questionnaire.availability;
        contacts.textContent =  questionnaire.contacts;



        closeButton.addEventListener('click', () => closeModal(modalOverlay));

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
});