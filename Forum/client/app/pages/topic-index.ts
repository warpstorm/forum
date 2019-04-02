import { throwIfNull } from '../helpers';
import { HttpMethod } from '../definitions/http-method';
import { App } from '../app';
import { Xhr } from '../services/xhr';

import { XhrOptions } from '../models/xhr-options';
import { TopicIndexSettings } from '../models/page-settings/topic-index-settings';

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

	constructor(private app: App) {
		throwIfNull(app, 'app');

		this.settings = getSettings();

		// Ensures the first load also has the settings state.
		window.history.replaceState(this.settings, document.title, window.location.href);
		window.onpopstate = this.eventPopState;

		if (this.app.hub) {
			this.app.hub.on('new-reply', this.hubNewReply);
		}
	}

	init(): void {
		this.bindPageButtons();

		this.app.navigation.setupPageNavigators();
		this.app.navigation.addListenerClickableLinkParent();
	}

	bindPageButtons() {
		document.querySelectorAll('.page a').forEach(element => {
			element.removeEventListener('click', this.eventPageClick);
			element.addEventListener('click', this.eventPageClick);
		});
	}

	async loadPage(page: number, pushState: boolean = true) {
		let mainElement = <Element>document.querySelector('main');
		mainElement.classList.add('faded');

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/Index/${this.settings.boardId}/${page}?unread=${this.settings.unreadFilter}`,
		});

		await Xhr.requestPartialView(requestOptions, document);

		window.scrollTo(0, 0);
		mainElement.classList.remove('faded');

		this.settings = getSettings();

		if (pushState) {
			window.history.pushState(this.settings, document.title, `/Topics/Index/${this.settings.boardId}/${this.settings.currentPage}?unread=${this.settings.unreadFilter}`);
		}

		this.init();
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

		let page = Number(eventTarget.getAttribute('data-page'));
		this.loadPage(page);
	}

	eventPopState = (event: PopStateEvent) => {
		let settings = event.state as TopicIndexSettings;

		if (settings) {
			this.settings = settings;
			this.loadPage(this.settings.currentPage, false);
		}
	}
}
