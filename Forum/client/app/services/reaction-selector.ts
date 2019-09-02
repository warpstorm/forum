import { insertAtCaret, show, hide } from '../helpers';
import { ReactionSelectorSettings } from '../models/page-settings/reaction-selector-settings';

function getSettings(): ReactionSelectorSettings {
	let genericWindow = <any>window;

	return new ReactionSelectorSettings({
		imgurName: genericWindow.imgurName
	});
}

export class ReactionSelector {
	private body: HTMLBodyElement;
	private settings: ReactionSelectorSettings;

	constructor(private doc: Document) {
		this.body = doc.getElementsByTagName('body')[0];
		this.settings = getSettings();
	}

	// Used in message forms to insert smileys into textareas.
	init(): void {
		this.doc.querySelectorAll('.add-reaction').forEach(element => {
			element.removeEventListener('click', this.eventShowSelector);
			element.addEventListener('click', this.eventShowSelector);

			element.querySelectorAll('[data-component="reaction-image"]').forEach(imgElement => {
				imgElement.removeEventListener('click', this.eventInsertReaction);
				imgElement.addEventListener('click', this.eventInsertReaction);
			});
		});
	}

	eventShowSelector = (event: Event): void => {
		event.stopPropagation();

		let self = this;
		let target = <HTMLElement>event.currentTarget;
		let selectorElement = target.querySelector('[data-component="reaction-selector"]');
		show(selectorElement);

		if (self.settings.imgurName) {

		}

		setTimeout(function () {
			self.body.addEventListener('click', self.eventCloseSelector);
		}, 50);
	}

	eventCloseSelector = (): void => {
		document.querySelectorAll('[data-component="reaction-selector"]').forEach(element => {
			hide(element);
		});

		let self = this;
		self.body.removeEventListener('click', self.eventCloseSelector);
	}

	eventInsertReaction = (event: Event): void => {
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
