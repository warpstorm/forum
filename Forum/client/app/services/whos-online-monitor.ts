import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";
import { App } from "../app";
import { Xhr } from "./xhr";

import { XhrOptions } from "../models/xhr-options";

export class WhosOnlineMonitor {
	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
	}

	init(): void {
		if (this.app.hub) {
			this.bindHubActions();
		}
	}

	bindHubActions = () => {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('whos-online', this.hubWhosOnline);
	}

	hubWhosOnline = () => {
		console.log("whos online event");

		let self = this;

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: '/Home/WhosOnline',
			responseType: 'document'
		});

		Xhr.request(requestOptions)
			.then((xhrResult) => {
				let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
				let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
				let resultBodyElements = resultBody.childNodes;
				let targetElement = <Element>self.doc.querySelector(`div[sidebar="whos-online"]`);

				resultBodyElements.forEach(node => {
					let element = node as Element;

					if (element && element.tagName && element.tagName.toLowerCase() == 'div') {
						targetElement.after(element);
						targetElement.remove();
					}
				});
			})
			.catch(Xhr.logRejected);
	}
}
