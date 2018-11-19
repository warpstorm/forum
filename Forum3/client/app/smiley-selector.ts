import { insertAtCaret, throwIfNull } from './helpers';

export class SmileySelector {
	private win: Window;
	private body: HTMLBodyElement;
	private smileySelector: HTMLElement;
	private smileySelectorImageHandler: (event: Event) => void;

	constructor(private doc: Document) {
		this.win = doc.defaultView;
		this.body = doc.getElementsByTagName('body')[0];

		let selectorElement = doc.querySelector('#smiley-selector') as HTMLElement;
		throwIfNull(selectorElement, 'selectorElement');

		this.smileySelector = selectorElement;
	}

	// Used in message forms to insert smileys into textareas.
	init(): void {
		let self = this;

		self.doc.querySelectorAll('.add-smiley').forEach(element => {
			element.on('click', (event: Event): void => {
				this.showSmileySelectorNearElement(<HTMLElement>event.currentTarget, this.eventInsertSmileyCode);
			});
		});
	}

	showSmileySelectorNearElement(target: HTMLElement, imageHandler: (event: Event) => void): void {
		let self = this;

		self.eventCloseSmileySelector();
		self.smileySelectorImageHandler = imageHandler;

		var rect = target.getBoundingClientRect();
		var targetTop = rect.top + self.win.pageYOffset - self.doc.documentElement.clientTop;
		var targetLeft = rect.left + self.win.pageXOffset - self.doc.documentElement.clientLeft;

		self.smileySelector.show();
		self.smileySelector.on('click', self.eventStopPropagation);

		let selectorTopOffset = targetTop + rect.height;
		self.smileySelector.style.top = selectorTopOffset + (selectorTopOffset == 0 ? '' : 'px');

		let selectorLeftOffset = targetLeft;
		var screenFalloff = targetLeft + self.smileySelector.clientWidth + 20 - self.win.innerWidth;

		if (screenFalloff > 0) {
			selectorLeftOffset -= screenFalloff;
		}

		self.smileySelector.style.left = selectorLeftOffset + (selectorLeftOffset == 0 ? '' : 'px');

		setTimeout(function () {
			self.body.on('click', self.eventCloseSmileySelector);
		}, 20);
	}

	eventCloseSmileySelector = (): void => {
		let self = this;

		self.smileySelector.style.top = '0';
		self.smileySelector.style.left = '0';
		self.smileySelector.hide();

		setTimeout(function () {
			self.body.off('click', self.eventCloseSmileySelector);
			self.smileySelector.off('click', self.eventStopPropagation);
			self.smileySelector.querySelectorAll('img').forEach(element => {
				element.off('click', self.smileySelectorImageHandler);
			});
		}, 50);
	}

	eventInsertSmileyCode = (event: Event): void => {
		let eventTarget = <Element>event.currentTarget
		let smileyCode = eventTarget.getAttribute('code');
		let targetTextArea = eventTarget.closest('form').querySelector('textarea');

		if (targetTextArea.textContent !== '') {
			smileyCode = ` ${smileyCode} `;
		}

		insertAtCaret(targetTextArea, smileyCode);

		this.eventCloseSmileySelector();
	}

	private eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}
