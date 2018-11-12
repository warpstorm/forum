import { TopicIndexOptions } from "./models/topic-index-options";
import { XhrOptions } from "./models/xhr-options";
import { Xhr } from "./services/xhr";
import { HttpMethod } from "./definitions/http-method";

// expects `window` and `document` to be defined at the global scope.
export default function () {
	let global = (<any>window);

	let options = new TopicIndexOptions({
		boardId: global.boardId,
		page: global.page,
		unreadFilter: global.unreadFilter,
		moreTopics: global.moreTopics
	});

	let topicIndex = new TopicIndex(document, options);
	topicIndex.setupPage();
}

export class TopicIndex {
	constructor(private htmlDocument: Document, private options: TopicIndexOptions) { }

	setupPage(): void {
		if (this.options.unreadFilter == 0) {
			this.htmlDocument.querySelector("#load-more-topics").show();
			this.htmlDocument.querySelector("#load-more-topics").onClick(this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		var originalText = this.htmlDocument.querySelector("#load-more-topics").textContent;

		this.htmlDocument.querySelector("#load-more-topics").textContent = "Loading...";

		let request = Xhr.request(new XhrOptions({
			method: HttpMethod.Get,
			url: `/topics/indexmore/${this.options.boardId}/?page=${this.options.page + 1}`
		}));

		request.then((xhrResult) => {
			this.htmlDocument.querySelector("#topic-list").append(xhrResult.data);

			if (this.options.moreTopics)
				this.htmlDocument.querySelector("#load-more-topics").textContent = originalText;
			else
				this.htmlDocument.querySelector("#load-more-topics").hide();
		});
	}
}
