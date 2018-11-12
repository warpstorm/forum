import { XhrOptions } from "../models/xhr-options";
import { Xhr } from "../services/xhr";
import { HttpMethod } from "../definitions/http-method";
import { Navigation } from "../navigation";

// expects `document` to be defined at the global scope.
export default function () {
	let topicIndex = new TopicIndex(document);
	topicIndex.setupPage();
}

export class TopicIndex {
	constructor(private html: Document) { }

	setupPage(): void {
		if ((<any>window).unreadFilter == 0) {
			this.html.querySelector("#load-more-topics").show();
			this.html.querySelector("#load-more-topics").off('click', this.eventLoadMoreTopics);
			this.html.querySelector("#load-more-topics").on('click', this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		let self = this;

		let originalText = self.html.querySelector("#load-more-topics").textContent;

		self.html.querySelector("#load-more-topics").textContent = "Loading...";

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
					new Navigation(self.html).addListenerClickableLinkParent();
				}
				else {
					self.html.querySelector("#topic-list").insertAdjacentElement('beforeend', element);
				}
			});

			if ((<any>window).moreTopics)
				self.html.querySelector("#load-more-topics").textContent = originalText;
			else
				self.html.querySelector("#load-more-topics").hide();
		});
	}
}
