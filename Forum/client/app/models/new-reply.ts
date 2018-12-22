export class NewReply {
	topicId: number = 0;
	messageId: number = 0;

	public constructor(init?: Partial<NewReply>) {
		Object.assign(this, init);
	}
}
