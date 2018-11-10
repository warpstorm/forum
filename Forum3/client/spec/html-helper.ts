import { throwIfNull } from '../scripts/helpers';
import { JSDOM } from 'jsdom';
import * as fs from 'fs';

export class HtmlHelper {
	private jsdom: JSDOM;

	constructor() { }

	loadEmptyDocument() {
		let htmlSource = '<!doctype html><html><body></body></html>';
		this.jsdom = new JSDOM(htmlSource);
	}

	loadDocumentFromPath(path: string) {
		let htmlSource = fs.readFileSync(path, 'utf8');
		this.jsdom = new JSDOM(htmlSource);
	}

	get(selector: string): Element {
		return this.jsdom.window.document.querySelector(selector);
	}

	getAll(selector: string): NodeListOf<Element> {
		return this.jsdom.window.document.querySelectorAll(selector);
	}

	window(): Window {
		if (!this.jsdom) {
			this.loadEmptyDocument();
		}

		return this.jsdom.window;
	}

	element(elementMarkup: string): Element {
		throwIfNull(elementMarkup, 'elementMarkup');

		let element = this.window().document.createElement(elementMarkup);

		let body = this.window().document.getElementsByTagName('body')[0];
		body.appendChild(element);

		return element;
	}

	event(eventType: string): Event {
		throwIfNull(eventType, 'eventType');

		let event = this.window().document.createEvent('Event');
		event.initEvent(eventType);
		return event;
	}

	click(element: Element) {
		throwIfNull(element, 'element');
		element.dispatchEvent(this.event('click'));
	}

	mouseEnter(element: Element) {
		throwIfNull(element, 'element');
		element.dispatchEvent(this.event('mouseenter'));
	}

	mouseLeave(element: Element) {
		throwIfNull(element, 'element');
		element.dispatchEvent(this.event('mouseleave'));
	}
}