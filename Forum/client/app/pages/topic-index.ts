import { App } from '../app';
import { Navigation } from '../navigation';

import { Xhr } from '../services/xhr';
import { XhrOptions } from '../models/xhr-options';
import { HttpMethod } from '../definitions/http-method';
import { throwIfNull, show, hide } from '../helpers';

export class TopicIndex {
	private moreTopicsButton: HTMLElement | null;

	constructor(private doc: Document, private app: App | null = null) {
		throwIfNull(doc, 'doc');
		this.moreTopicsButton = doc.querySelector('#load-more-topics');
	}

	init(): void {
		if ((<any>this.doc.defaultView).unreadFilter == 0 && this.moreTopicsButton) {
			show(this.moreTopicsButton);
			this.moreTopicsButton.removeEventListener('click', this.eventLoadMoreTopics);
			this.moreTopicsButton.addEventListener('click', this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		let self = this;

		let originalText = '';

		if (this.moreTopicsButton) {
			originalText = this.moreTopicsButton.textContent || '';
			this.moreTopicsButton.textContent = 'Loading...';
		}

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/topics/${(<any>window).moreAction}/${(<any>window).boardId}/?page=${(<any>window).page + 1}`,
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
							new Navigation(self.doc).addListenerClickableLinkParent();
						}
						else {
							let topicList = <Element>self.doc.querySelector('#topic-list');
							topicList.insertAdjacentElement('beforeend', element);
						}
					}
				});

				if (this.moreTopicsButton) {
					if ((<any>window).moreTopics) {
						this.moreTopicsButton.textContent = originalText;
					}
					else {
						hide(this.moreTopicsButton);
					}
				}
			})
			.catch(Xhr.logRejected);
	}
}
