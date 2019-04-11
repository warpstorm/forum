export class ProcessStage {
	public actionName: string = '';
	public actionNote: string = '';
	public take: number = 0;
	public totalSteps: number = 0;
	public totalRecords: number = 0;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
