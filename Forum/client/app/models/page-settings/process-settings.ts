export class ProcessSettings {
	public stages: string[] = [];

	public currentAction: string = '';
	public currentPage: number = 0;
	public currentStage: number = 0;

	public take: number = 0;
	public totalPages: number = 0;
	public totalRecords: number = 0;

	public lastRecordId: number = -1;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
