import { TopicIndexOptions } from "../models/topic-index-options";
import { throwIfNull } from "./helpers";

export default function () {
	if ((<any>window).pageActions == "topicIndex") {
		let options = new TopicIndexOptions({
			boardId: (<any>window).boardId,
			page: (<any>window).page,
			unreadFilter: (<any>window).unreadFilter,
			moreTopics: (<any>window).moreTopics
		});

		// expects document to be defined at the global scope.
		let topicIndex = new TopicIndex(document, options);
		topicIndex.setupPage();
	}
}

export class TopicIndex {
	constructor(private htmlDocument: Document, private options: TopicIndexOptions) { }

	setupPage(): void {
		if (this.options.unreadFilter == 0) {
			this.htmlDocument.querySelector("#load-more-topics").classList.remove("hidden");
			this.htmlDocument.querySelector("#load-more-topics").addEventListener("click", this.eventLoadMoreTopics);
		}
	}

	eventLoadMoreTopics = () => {
		var originalText = this.htmlDocument.querySelector("#load-more-topics").textContent;

		this.htmlDocument.querySelector("#load-more-topics").textContent = "Loading...";

		$.ajax({
			dataType: "html",
			url: "/topics/indexmore/" + this.options.boardId + "/?page=" + (this.options.page + 1),
			success: function (data) {
				this.htmlDocument.querySelector("#topic-list").append(data);

				if (this.options.moreTopics)
					this.htmlDocument.querySelector("#load-more-topics").text(originalText);
				else
					this.htmlDocument.querySelector("#load-more-topics").hide();
			}
		});
	}
}
