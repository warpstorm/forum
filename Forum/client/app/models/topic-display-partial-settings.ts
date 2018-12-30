export class TopicDisplayPartialSettings {
	public latest: number = 0;
	public firstMessageId: number = 0;
	public newMessages: number[] = [];

	public constructor(init?: Partial<Window>) {
		Object.assign(this, init);
	}
}
