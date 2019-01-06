import { throwIfNull } from '../helpers';
import { HttpMethod } from '../definitions/http-method';
import { App } from '../app';
import { Navigation } from '../services/navigation';
import { Xhr } from '../services/xhr';

import { XhrOptions } from '../models/xhr-options';
import { TopicIndexSettings } from '../models/topic-index-settings';

export class TopicIndex {
	private settings: TopicIndexSettings;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		this.settings = new TopicIndexSettings({
			boardId: (<any>window).boardId,
			currentPage: (<any>window).currentPage,
			totalPages: (<any>window).totalPages,
			unreadFilter: (<any>window).unreadFilter
		});
	}

	init(): void {
		let self = this;

		if (self.app.hub) {
			self.bindHubActions();
		}

		self.bindPageButtons();

		window.onpopstate = function (event: PopStateEvent) {
			var settings = event.state as TopicIndexSettings;

			if (settings) {
				self.settings = settings;
				self.loadTopicsPage(self.settings.boardId, self.settings.currentPage, self.settings.unreadFilter, false);
			}
		};
	}

	bindPageButtons = (pushState: boolean = true) => {
		let self = this;

		if (pushState) {
			window.history.pushState(self.settings, self.doc.title, `/Topics/Index/${self.settings.boardId}/${self.settings.currentPage}?unread=${self.settings.unreadFilter}`);
		}

		self.doc.querySelectorAll('.page a').forEach(element => {
			element.removeEventListener('click', self.eventPageClick);
			element.addEventListener('click', self.eventPageClick);
		});
	}

	bindHubActions = () => {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('new-reply', this.hubNewReply);
	}

	loadTopicsPage = (boardId: number, pageId: number, unread: number, pushState: boolean = true) => {
		let self = this;

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/IndexPartial/${boardId}/${pageId}?unread=${unread}`,
			responseType: 'document'
		});

		Xhr.request(requestOptions)
			.then((xhrResult) => {
				let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
				let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
				let resultBodyElements = resultBody.childNodes;

				resultBodyElements.forEach(node => {
					let element = node as Element;

					if (element && element.tagName) {
						if (element.tagName.toLowerCase() == 'script') {
							eval(element.textContent || '');
						}
						else if (element.tagName.toLowerCase() == 'section') {
							let targetElement = <Element>self.doc.querySelector('#topic-list');
							targetElement.after(element);
							targetElement.remove();
						}
						else if (element.tagName.toLowerCase() == 'footer') {
							let targetElement = <Element>self.doc.querySelector('footer');
							targetElement.after(element);
							targetElement.remove();
						}
					}
				});

				self.settings = new TopicIndexSettings({
					boardId: (<any>window).boardId,
					currentPage: (<any>window).currentPage,
					totalPages: (<any>window).totalPages,
					unreadFilter: (<any>window).unreadFilter
				});

				self.bindPageButtons(pushState);
				self.app.navigation.setupPageNavigators();
				self.app.navigation.addListenerClickableLinkParent();
			})
			.catch(Xhr.logRejected);
	}

	hubNewReply = () => {
		if (this.settings.currentPage == 1) {
			this.loadTopicsPage(this.settings.boardId, 1, this.settings.unreadFilter);
		}
	}

	eventPageClick = (event: Event) => {
		let eventTarget = event.currentTarget as HTMLAnchorElement;

		if (!eventTarget) {
			return;
		}

		event.preventDefault();

		let self = this;
		let pageId = Number(eventTarget.getAttribute('data-page-id'));

		self.loadTopicsPage(self.settings.boardId, pageId, self.settings.unreadFilter);
	}
}
