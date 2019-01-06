export class TopicIndexSettings {
	public currentPage: number = 0;

	public constructor(init?: Partial<Window>) {
		Object.assign(this, init);
	}
}
