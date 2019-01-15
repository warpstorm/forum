export class OnlineUser {
	public id: string = '';
	public time: string = '';
	public isOnline: boolean = false;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
