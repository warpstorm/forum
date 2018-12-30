export class HubMessage {
	topicId: number = 0;
	messageId: number = 0;

	public constructor(init?: Partial<HubMessage>) {
		Object.assign(this, init);
	}
}
