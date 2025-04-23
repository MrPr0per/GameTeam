export const buttonsActivator = {
	originalTexts: new Map(),

	setPending(button) {
		if (button.id) {
			this.originalTexts.set(button.id, button.innerText);
			button.innerText = 'Загрузка...';
		}
		button.disabled = true;
	},

	resetPending(button) {
		if (button.id && this.originalTexts.has(button.id)) {
			button.innerText = this.originalTexts.get(button.id);
			this.originalTexts.delete(button.id);
		}
		button.disabled = false;
	},
};