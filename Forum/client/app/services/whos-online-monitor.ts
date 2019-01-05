import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";
import { App } from "../app";
import { Xhr } from "./xhr";

import { XhrOptions } from "../models/xhr-options";

export class WhosOnlineMonitor {
	private recentRequest: boolean = false;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
	}

	init(): void {
		if (document.querySelector("[sidebar='whos-online']")) {
			if (this.app.hub) {
				this.bindHubActions();
			}

			this.bindChicletMonitor();
		}
	}

	bindHubActions = () => {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('whos-online', this.hubWhosOnline);
	}

	bindChicletMonitor = () => {
		document.querySelectorAll('.whos-online-chiclet').forEach(element => {
			let chicletTimeValue = element.getAttribute('time');

			if (chicletTimeValue) {
				let chicletTime = new Date(chicletTimeValue);
				
				// 5 minute expiration
				let expiration = new Date(chicletTime.getTime() + 5 * 60 * 1000); 

				let difference = expiration.getTime() - new Date().getTime();

				setTimeout(() => {
					element.classList.add('hidden');
				}, difference);
			}
		});
	}

	hubWhosOnline = () => {
		let self = this;

		if (!self.recentRequest) {
			self.recentRequest = true;

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

					this.bindChicletMonitor();
				})
				.catch(Xhr.logRejected);
						
			setTimeout(() => {
				self.recentRequest = false;
			}, 10000);
		}
	}
}
