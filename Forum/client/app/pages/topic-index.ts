import { throwIfNull } from '../helpers';
import { HttpMethod } from '../definitions/http-method';
import { App } from '../app';
import { Xhr } from '../services/xhr';

import { XhrOptions } from '../models/xhr-options';
import { TopicIndexSettings } from '../models/topic-index-settings';

function getSettings(): TopicIndexSettings {
	let genericWindow = <any>window;

	return new TopicIndexSettings({
		boardId: genericWindow.boardId,
		currentPage: genericWindow.currentPage,
		totalPages: genericWindow.totalPages,
		unreadFilter: genericWindow.unreadFilter
	});
}

export class TopicIndex {
	private settings: TopicIndexSettings;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');

		this.settings = getSettings();

		// Ensures the first load also has the settings state.
		window.history.replaceState(this.settings, this.doc.title, window.location.href);
		window.onpopstate = this.eventPopState;
	}

	init(): void {
		if (this.app.hub) {
			this.bindHubActions();
		}

		this.bindPageButtons();
	}

	bindPageButtons() {
		this.doc.querySelectorAll('.page a').forEach(element => {
			element.removeEventListener('click', this.eventPageClick);
			element.addEventListener('click', this.eventPageClick);
		});
	}

	bindHubActions() {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('new-reply', this.hubNewReply);
	}

	async loadPage(pageId: number, pushState: boolean = true) {
		let list = <Element>this.doc.querySelector('#topic-list');
		list.classList.add('faded');

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/IndexPartial/${this.settings.boardId}/${pageId}?unread=${this.settings.unreadFilter}`,
			responseType: 'document'
		});

		let xhrResult = await Xhr.request(requestOptions);

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
					list.after(element);
					list.remove();
				}
				else if (element.tagName.toLowerCase() == 'footer') {
					let targetElement = <Element>this.doc.querySelector('footer');
					targetElement.after(element);
					targetElement.remove();
				}
			}
		});

		this.settings = getSettings();

		if (pushState) {
			window.history.pushState(this.settings, this.doc.title, `/Topics/Index/${this.settings.boardId}/${this.settings.currentPage}?unread=${this.settings.unreadFilter}`);
		}

		this.app.navigation.setupPageNavigators();
		this.app.navigation.addListenerClickableLinkParent();
	}

	hubNewReply = () => {
		if (this.settings.currentPage == 1) {
			this.loadPage(1);
		}
	}

	eventPageClick = (event: Event) => {
		let eventTarget = event.currentTarget as HTMLAnchorElement;

		if (!eventTarget) {
			return;
		}

		event.preventDefault();

		let pageId = Number(eventTarget.getAttribute('data-page-id'));
		this.loadPage(pageId);
	}

	eventPopState = (event: PopStateEvent) => {
		var settings = event.state as TopicIndexSettings;

		if (settings) {
			this.settings = settings;
			this.loadPage(this.settings.currentPage, false);
		}
	}
}
