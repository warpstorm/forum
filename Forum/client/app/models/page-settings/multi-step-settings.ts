export class MultiStepSettings {
	public page: number = 0;
	public totalPages: number = 0;
	public take: number = 0;
	public nextAction: string = '';

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
