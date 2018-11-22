import { App } from '../app';
import { Navigation } from '../navigation';

import { Xhr } from '../services/xhr';
import { XhrOptions } from '../models/xhr-options';
import { HttpMethod } from '../definitions/http-method';
import { throwIfNull } from '../helpers';

export class TopicIndex {
	private moreTopicsButton: HTMLElement;

	constructor(private doc: Document, private app: App = null) {
		throwIfNull(doc, 'doc');
		this.moreTopicsButton = doc.querySelector('#load-more-topics');
	}

	init(): void {
		if ((<any>this.doc.defaultView).unreadFilter == 0) {
			this.moreTopicsButton.show();
			this.moreTopicsButton.off('click', this.eventLoadMoreTopics);
			this.moreTopicsButton.on('click', this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		let self = this;

		let originalText = this.moreTopicsButton.textContent;

		this.moreTopicsButton.textContent = 'Loading...';

		let request = Xhr.request(new XhrOptions({
			method: HttpMethod.Get,
			url: `/topics/${(<any>window).moreAction}/${(<any>window).boardId}/?page=${(<any>window).page + 1}`,
			responseType: 'document'
		}));

		request.then((xhrResult) => {
			let resultDocument = (<Document>xhrResult.response).documentElement.querySelector('body').childNodes;

			resultDocument.forEach(node => {
				let element = <Element>node;

				if (element.tagName.toLowerCase() == 'script') {
					eval(element.textContent);
					new Navigation(self.doc).addListenerClickableLinkParent();
				}
				else {
					self.doc.querySelector('#topic-list').insertAdjacentElement('beforeend', element);
				}
			});

			if ((<any>window).moreTopics)
				this.moreTopicsButton.textContent = originalText;
			else
				this.moreTopicsButton.hide();
		});
	}
}
