import { throwIfNull } from '../scripts/helpers';
import { JSDOM } from 'jsdom';

export class HtmlHelper {
	private jsdomDocument: Document;

	document(): Document {
		if (!this.jsdomDocument) {
			let jsdom = new JSDOM('<!doctype html><html><body></body></html>');
			this.jsdomDocument = jsdom.window.document;
		}

		return this.jsdomDocument;
	}

	element(elementMarkup: string): HTMLElement {
		throwIfNull(elementMarkup, 'elementMarkup');

		let element = this.jsdomDocument.createElement(elementMarkup);

		let body = this.jsdomDocument.getElementsByTagName('body')[0];
		body.appendChild(element);

		return element;
	}

	event(eventType: string): Event {
		throwIfNull(eventType, 'eventType');

		let event = this.jsdomDocument.createEvent('Event');
		event.initEvent(eventType);
		return event;
	}
}