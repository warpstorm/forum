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
	var bbCode = new BBCode();

	// expects document to be defined at the global scope.
	bbCode.addListeners(document);
}

export class BBCode {
	addListeners(htmlDocument: Document): void {
		throwIfNull(htmlDocument, 'htmlDocument');

		this.addBBCodeListener(htmlDocument);
		this.addSpoilerListener(htmlDocument);
	}

	addBBCodeListener(htmlDocument: Document): void {
		throwIfNull(htmlDocument, 'htmlDocument');

		let elements = htmlDocument.getElementsByClassName('add-bbcode');

		for (let i = 0; i < elements.length; i++) {
			elements[i].addEventListener('click', this.insertBBCode);
		}
	}

	addSpoilerListener(htmlDocument: Document): void {
		throwIfNull(htmlDocument, 'htmlDocument');

		let elements = htmlDocument.getElementsByClassName('bbc-spoiler');

		for (let i = 0; i < elements.length; i++) {
			elements[i].addEventListener('click', this.showSpoiler);
		}
	}

	insertBBCode(event: Event): void {
		throwIfNull(event, 'event');

		event.preventDefault();

		let target = <HTMLElement>event.currentTarget;
		let targetCode = target.getAttribute('bbcode');	

		let form = target.closest('form');

		if (!form) {
			throw new Error('Element is not defined');
		}

		let targetTextArea = form.getElementsByTagName('textarea')[0];

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
