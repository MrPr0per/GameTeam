export const buttonsActivator = {
    originalTexts: new WeakMap(),

    /**
     * блокирует кнопку и меняет текст на "Загрузка..." (сохраняя оригинальный текст)
     * @param {HTMLButtonElement} button - Кнопка, которую нужно перевести в состояние загрузки
     */
    setPending(button) {

        this.originalTexts.set(button, button.innerText);
        button.innerText = 'Загрузка...';
        button.disabled = true;
    },

    /**
     * разблокирует кнопку и восстанавливает оригинальный текст
     * @param {HTMLButtonElement} button - Кнопка, которую нужно вернуть в обычное состояние
     */
    resetPending(button) {
        if (this.originalTexts.has(button)) {
            button.innerText = this.originalTexts.get(button);
            this.originalTexts.delete(button);
        }
        button.disabled = false;
    },
};