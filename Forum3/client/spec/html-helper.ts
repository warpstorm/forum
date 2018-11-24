import { throwIfNull } from '../app/helpers';
import { JSDOM } from 'jsdom';
import * as fs from 'fs';

export class HtmlHelper {
	private jsdom: JSDOM | null = null;

	constructor() { }

	loadEmptyDocument() {
		let htmlSource = '<!doctype html><html><body></body></html>';
		this.jsdom = new JSDOM(htmlSource);
	}

	loadDocumentFromPath(path: string) {
		let htmlSource = fs.readFileSync(path, 'utf8');
		this.jsdom = new JSDOM(htmlSource);
	}

	get(selector: string): Element | null {
		if (!this.jsdom) {
			this.loadEmptyDocument();
		}

		return this.jsdom ? this.jsdom.window.document.querySelector(selector) : null;
	}

	getAll(selector: string): NodeListOf<Element> | null {
		if (!this.jsdom) {
			this.loadEmptyDocument();
		}

		return this.jsdom ? this.jsdom.window.document.querySelectorAll(selector) : null;
	}

	window(): Window | null {
		if (!this.jsdom) {
			this.loadEmptyDocument();
		}

		return this.jsdom ? this.jsdom.window : null;
	}

	element(elementMarkup: string): Element | null {
		throwIfNull(elementMarkup, 'elementMarkup');

		let window = this.window();

		if (!window) {
			return null;
		}

		let element = window.document.createElement(elementMarkup);

		let body = window.document.getElementsByTagName('body')[0];
		body.appendChild(element);

		return element;
	}

	click(element: Element) {
		this.dispatchEvent(element, 'click');
	}

	mouseEnter(element: Element) {
		this.dispatchEvent(element, 'mouseenter');
	}

	mouseLeave(element: Element) {
		this.dispatchEvent(element, 'mouseleave');
	}

	private dispatchEvent(element: Element | null, eventType: EventType) {
		if (!element) {
			return;
		}

		let event = this.event(eventType);

		if (event) {
			element.dispatchEvent(event);
		}
	}

	private event(eventType: EventType): Event | null {
		throwIfNull(eventType, 'eventType');

		let window = this.window();

		if (!window) {
			return null;
		}

		let event = window.document.createEvent('Event');
		event.initEvent(eventType);
		return event;
	}
}