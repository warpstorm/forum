import { insertAtCaret, show, hide } from '../helpers';

export class SmileySelector {
	private body: HTMLBodyElement;

	constructor(private doc: Document) {
		this.body = doc.getElementsByTagName('body')[0];
	}

	// Used in message forms to insert smileys into textareas.
	init(): void {
		this.doc.querySelectorAll('.add-smiley').forEach(element => {
			element.removeEventListener('click', this.eventShowSmileySelector);
			element.addEventListener('click', this.eventShowSmileySelector);

			element.querySelectorAll('[data-component="smiley-image"]').forEach(imgElement => {
				imgElement.removeEventListener('click', this.eventInsertSmileyCode);
				imgElement.addEventListener('click', this.eventInsertSmileyCode);
			});
		});
	}

	eventShowSmileySelector = (event: Event): void => {
		event.stopPropagation();

		let self = this;
		let target = <HTMLElement>event.currentTarget;
		let smileySelector = target.querySelector('[data-component="smiley-selector"]');
		show(smileySelector);

		setTimeout(function () {
			self.body.addEventListener('click', self.eventCloseSmileySelector);
		}, 50);
	}

	eventCloseSmileySelector = (): void => {
		document.querySelectorAll('[data-component="smiley-selector"]').forEach(element => {
			hide(element);
		});

		let self = this;
		self.body.removeEventListener('click', self.eventCloseSmileySelector);
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

		self.eventCloseSmileySelector();
	}
}
