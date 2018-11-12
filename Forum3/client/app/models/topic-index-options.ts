export class TopicIndexOptions {
	boardId: number;
	page: number;
	unreadFilter: number;
	moreTopics: boolean;

	public constructor(init?: Partial<TopicIndexOptions>) {
		Object.assign(this, init);
	}
}
