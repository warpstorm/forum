import { insertAtCaret, throwIfNull } from '../helpers'

let bbCodes: { [key: string]: string } = {
	'bold': '[b]  [/b]',
	'italics': '[i]  [/i]',
	'url': '[url=]  [/url]',
	'quote': '[quote]\n\n\n[/quote]',
	'spoiler': '[spoiler]  [/spoiler]',
	'img': '[img]  [/img]',
	'reaction': '[reaction]  [/reaction]',
	'underline': '[u]  [/u]',
	'strike': '[s]  [/s]',
	'color': '[color=#A335EE]  [/color]',
	'list': '[ul]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ul]',
	'numlist': '[ol]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ol]',
	'code': '[code]\n\n\n[/code]',
	'size': '[size=10]  [/size]'
};

export class BBCode {
	constructor(private doc: Document) {}

	init(): void {
		this.addBBCodeListener();
		this.addSpoilerListener();
	}

	addBBCodeListener(): void {
		this.doc.querySelectorAll('.add-bbcode').forEach(element => {
			element.removeEventListener('click', this.insertBBCode);
			element.addEventListener('click', this.insertBBCode);
		});
	}

	addSpoilerListener(): void {
		this.doc.querySelectorAll('.bbc-spoiler').forEach(element => {
			element.removeEventListener('click', this.showSpoiler);
			element.addEventListener('click', this.showSpoiler);
		});
	}

	insertBBCode(event: Event): void {
		throwIfNull(event, 'event');

		event.preventDefault();

		let target = <HTMLElement>event.currentTarget;

		if (!target) {
			throw new Error('Event target not found');
		}

		let targetCode = target.getAttribute('bbcode');	

		if (!targetCode) {
			throw new Error('Target bbcode not found');
		}

		let form = target.closest('form');

		if (!form) {
			throw new Error('Form element not found');
		}

		let targetTextArea = form.querySelector('textarea');

		if (!targetTextArea) {
			throw new Error('Textarea element not found');
		}

		insertAtCaret(targetTextArea, bbCodes[targetCode]);
	}

	showSpoiler(event: Event): void {
		throwIfNull(event, 'event');

		// in case they click a link in a spoiler when revealing the spoiler.
		event.preventDefault();

		// in case they click a spoiler that is in a link
		event.stopPropagation();

		let target = <HTMLElement>event.target;

		throwIfNull(target, 'target');

		if (target.classList.contains('bbc-spoiler-hover')) {
			target.classList.remove('bbc-spoiler-hover');
		}
		else {
			target.classList.add('bbc-spoiler-hover');
		}
	}
}
