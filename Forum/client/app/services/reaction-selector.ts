import { insertAtCaret, show, hide, toggle } from '../helpers';
import { ReactionSelectorSettings } from '../models/page-settings/reaction-selector-settings';

function getSettings(): ReactionSelectorSettings {
	let genericWindow = <any>window;

	return new ReactionSelectorSettings({
		imgurName: genericWindow.imgurName,
		reactionImages: genericWindow.reactionImages
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
			element.removeEventListener('click', this.eventToggleSelector);
			element.addEventListener('click', this.eventToggleSelector);
		});
	}

	eventToggleSelector = (event: Event): void => {
		event.stopPropagation();

		let self = this;
		let target = <HTMLElement>event.currentTarget;
		let selectorElement = <HTMLElement>target.querySelector('[data-component="reaction-selector"]');
		let imageList = <HTMLElement>selectorElement.querySelector('[data-component="reaction-image-list"]');

		if (imageList.getAttribute('data-loaded') == '0') {
			self.settings.reactionImages.forEach(image => {
				imageList.innerHTML += `<div class='reaction-image' data-id="${image.id}"><video autoplay loop muted><source src='${image.path}' type='video/mp4' /></video></div>`;
			});

			imageList.setAttribute('data-loaded', '1');

			imageList.querySelectorAll('.reaction-image').forEach(imgElement => {
				imgElement.removeEventListener('click', this.eventInsertReaction);
				imgElement.addEventListener('click', this.eventInsertReaction);
			});
		}

		toggle(selectorElement);

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
		let result = eventTarget.getAttribute('data-id') || '';

		let form = <HTMLFormElement>eventTarget.closest('form');
		let targetTextArea = <HTMLTextAreaElement>form.querySelector('textarea');

		result = `[reaction]https://i.imgur.com/${result}.gifv[/reaction]`;

		if (targetTextArea.value !== '') {
			result = ` ${result} `;
		}

		insertAtCaret(targetTextArea, result);

		self.eventCloseSelector();
	}
}
