const debugMode = true;
const toastVisibleDuration = 3000; // [ms]

// отображает плашку с ошибкой в правом нижнем углу
export function showError(message, ...debugInfo) {
    if (debugMode) {
        console.log(message, ':', debugInfo);
    }
    showServerErrorToast(message);
}

function showServerErrorToast(message) {
    const container = getOrCreateToastContainer();
    addToast(container, message);
}

function getOrCreateToastContainer() {
    const toastsContainerId = 'toast-container';
    let container = document.getElementById(toastsContainerId);
    if (!container) {
        // создаем контейнер
        container = document.createElement('div');
        container.id = toastsContainerId;
        document.body.appendChild(container);
    }
    // подгружаем стили
    const href = '../css/toast.css';
    if (!document.querySelector(`link[href="${href}"]`)) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        document.head.appendChild(link);
    }
    return container;
}

function addToast(container, message) {
    const toast = createToast(message);
    addToContainer(container, toast);
    startAppearanceAnimation(toast);
}

function createToast(message) {
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.innerHTML = `
        <span class="toast-icon">❌</span>
        <span class="toast-message">${message}</span>
        <button class="toast-close">&times;</button>
    `;

    // Закрытие по таймеру
    const timeout = setTimeout(() => closeToast(), toastVisibleDuration);

    // Закрытие вручную
    toast.querySelector('.toast-close').addEventListener('click', () => {
        clearTimeout(timeout);
        closeToast();
    });

    function closeToast() {
        // меняем классы для анимации
        toast.classList.remove('show');
        toast.classList.add('hide');

        // после того как анимация исчезновения кончится - удаляем
        toast.addEventListener('transitionend', () => {
            toast.remove();
        });
    }

    return toast;

}

function addToContainer(container, toast) {
    container.appendChild(toast);
}

function startAppearanceAnimation(toast) {
    requestAnimationFrame(() => {
        toast.classList.add('show');
    });
}