import { XhrOptions } from "../models/xhr-options";
import { Xhr } from "../services/xhr";
import { HttpMethod } from "../definitions/http-method";
import navigation, { Navigation } from "../navigation";

// expects `window` and `document` to be defined at the global scope.
export default function () {
	let topicIndex = new TopicIndex(document);
	topicIndex.setupPage();
}

export class TopicIndex {
	constructor(private htmlDocument: Document) { }

	setupPage(): void {
		if ((<any>window).unreadFilter == 0) {
			this.htmlDocument.querySelector("#load-more-topics").show();
			this.htmlDocument.querySelector("#load-more-topics").off('click', this.eventLoadMoreTopics);
			this.htmlDocument.querySelector("#load-more-topics").on('click', this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		let self = this;

		let originalText = self.htmlDocument.querySelector("#load-more-topics").textContent;

		self.htmlDocument.querySelector("#load-more-topics").textContent = "Loading...";

		let request = Xhr.request(new XhrOptions({
			method: HttpMethod.Get,
			url: `/topics/indexmore/${(<any>window).boardId}/?page=${(<any>window).page + 1}`,
			responseType: 'document'
		}));

		request.then((xhrResult) => {
			let resultDocument = (<Document>xhrResult.response).documentElement.querySelector('body').childNodes;

			resultDocument.forEach(node => {
				let element = <Element>node;

				if (element.tagName.toLowerCase() == 'script') {
					eval(element.textContent);
					new Navigation(self.htmlDocument).addListenerClickableLinkParent();
				}
				else {
					self.htmlDocument.querySelector("#topic-list").insertAdjacentElement('beforeend', element);
				}
			});

			if ((<any>window).moreTopics)
				self.htmlDocument.querySelector("#load-more-topics").textContent = originalText;
			else
				self.htmlDocument.querySelector("#load-more-topics").hide();
		});
	}
}
