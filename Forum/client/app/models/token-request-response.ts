export class TokenRequestResponse {
	token: string = '';

	public constructor(init?: Partial<TokenRequestResponse>) {
		Object.assign(this, init);
	}
}