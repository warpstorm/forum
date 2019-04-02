export class Step {
	public actionName: string = '';
	public actionNote: string = '';
	public take: number = 0;
	public totalPages: number = 0;
	public totalRecords: number = 0;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
