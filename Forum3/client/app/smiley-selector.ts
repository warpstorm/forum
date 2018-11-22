import { insertAtCaret } from './helpers';

export class SmileySelector {
	private win: Window;
	private body: HTMLBodyElement;
	private smileySelector: HTMLElement;
	private smileySelectorImageHandler: (event: Event) => void;
	private smileySelectorTargetTextArea: HTMLTextAreaElement;

	constructor(private doc: Document) {
		this.win = doc.defaultView;
		this.body = doc.getElementsByTagName('body')[0];

		let selectorElement = doc.querySelector('#smiley-selector') as HTMLElement;

		if (!selectorElement) {
			return;
		}

		this.smileySelector = selectorElement;
	}

	// Used in message forms to insert smileys into textareas.
	init(): void {
		let self = this;

		self.doc.querySelectorAll('.add-smiley').forEach(element => {
			element.on('click', (event: Event): void => {
				let target = <HTMLElement>event.currentTarget;

				self.smileySelectorTargetTextArea = target.closest('form').querySelector('textarea');
				self.showSmileySelectorNearElement(target, self.eventInsertSmileyCode);
			});
		});
	}

	showSmileySelectorNearElement(target: HTMLElement, imageHandler: (event: Event) => void): void {
		event.stopPropagation();

		let self = this;
		self.eventCloseSmileySelector();
		self.smileySelectorImageHandler = imageHandler;

		self.smileySelector.querySelectorAll('img').forEach(element => {
			element.on('click', self.smileySelectorImageHandler);
		});

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
		}, 50);
	}

	eventCloseSmileySelector = (): void => {
		let self = this;

		self.smileySelector.style.top = '0';
		self.smileySelector.style.left = '0';
		self.smileySelector.hide();

		self.body.off('click', self.eventCloseSmileySelector);
		self.smileySelector.off('click', self.eventStopPropagation);

		if (self.smileySelectorImageHandler) {
			self.smileySelector.querySelectorAll('img').forEach(element => {
				element.off('click', self.smileySelectorImageHandler);
			});

			self.smileySelectorImageHandler = null;
		}
	}

	eventInsertSmileyCode = (event: Event): void => {
		let self = this;

		let eventTarget = <Element>event.currentTarget
		let smileyCode = eventTarget.getAttribute('code');
			   
		if (self.smileySelectorTargetTextArea.textContent !== '') {
			smileyCode = ` ${smileyCode} `;
		}

		insertAtCaret(self.smileySelectorTargetTextArea, smileyCode);

		self.eventCloseSmileySelector();
	}

	private eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}
