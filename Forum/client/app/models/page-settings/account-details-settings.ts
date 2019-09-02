export class AccountDetailsSettings {
	public imgurClientId: string = '';

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
