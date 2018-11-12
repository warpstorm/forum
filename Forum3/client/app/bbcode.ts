import { insertAtCaret, throwIfNull } from './helpers'

let bbCodes = {
	'bold': '[b]  [/b]',
	'italics': '[i]  [/i]',
	'url': '[url=]  [/url]',
	'quote': '[quote]\n\n\n[/quote]',
	'spoiler': '[spoiler]  [/spoiler]',
	'img': '[img]  [/img]',
	'underline': '[u]  [/u]',
	'strike': '[s]  [/s]',
	'color': '[color=#A335EE]  [/color]',
	'list': '[ul]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ul]',
	'numlist': '[ol]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ol]',
	'code': '[code]\n\n\n[/code]',
	'size': '[size=10]  [/size]'
};

export default function () {
	// expects document to be defined at the global scope.
	var bbCode = new BBCode(document);
	bbCode.addListeners();
}

export class BBCode {
	constructor(private htmlDocument: Document) {}

	addListeners(): void {
		this.addBBCodeListener();
		this.addSpoilerListener();
	}

	addBBCodeListener(): void {
		this.htmlDocument.querySelectorAll('.add-bbcode').forEach(element => {
			element.addEventListener('click', this.insertBBCode);
		});
	}

	addSpoilerListener(): void {
		this.htmlDocument.querySelectorAll('.bbc-spoiler').forEach(element => {
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
