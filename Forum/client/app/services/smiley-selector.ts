import { insertAtCaret, show, hide } from '../helpers';

export class SmileySelector {
	private body: HTMLBodyElement;

	constructor(private doc: Document) {
		this.body = doc.getElementsByTagName('body')[0];
	}

	// Used in message forms to insert smileys into textareas.
	init(): void {
		this.doc.querySelectorAll('.add-smiley').forEach(element => {
			element.removeEventListener('click', this.eventShowSelector);
			element.addEventListener('click', this.eventShowSelector);

			element.querySelectorAll('[data-component="smiley-image"]').forEach(imgElement => {
				imgElement.removeEventListener('click', this.eventInsertSmileyCode);
				imgElement.addEventListener('click', this.eventInsertSmileyCode);
			});
		});
	}

	eventShowSelector = (event: Event): void => {
		event.stopPropagation();

		let self = this;
		let target = <HTMLElement>event.currentTarget;
		let selectorElement = target.querySelector('[data-component="smiley-selector"]');
		show(selectorElement);

		setTimeout(function () {
			self.body.addEventListener('click', self.eventCloseSelector);
		}, 50);
	}

	eventCloseSelector = (): void => {
		document.querySelectorAll('[data-component="smiley-selector"]').forEach(element => {
			hide(element);
		});

		let self = this;
		self.body.removeEventListener('click', self.eventCloseSelector);
	}

	eventInsertSmileyCode = (event: Event): void => {
		event.stopPropagation();

		let self = this;

		let eventTarget = <Element>event.currentTarget
		let smileyCode = eventTarget.getAttribute('code') || '';

		let form = <HTMLFormElement>eventTarget.closest('form');
		let targetTextArea = <HTMLTextAreaElement>form.querySelector('textarea');

		if (targetTextArea.value !== '') {
			smileyCode = ` ${smileyCode} `;
		}

		insertAtCaret(targetTextArea, smileyCode);

		self.eventCloseSelector();
	}
}
