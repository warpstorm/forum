export class ResponseToken {
	token: string = '';

	public constructor(init?: Partial<ResponseToken>) {
		Object.assign(this, init);
	}
}