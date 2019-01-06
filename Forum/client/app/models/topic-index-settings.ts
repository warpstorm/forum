export class TopicIndexSettings {
	public boardId: number = 0;
	public currentPage: number = 0;
	public totalPages: number = 0;
	public unreadFilter: number = 0;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
