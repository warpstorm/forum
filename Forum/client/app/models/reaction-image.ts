export class ReactionImage {
	id: string = '';
	path: string = '';

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}