export class MultiStepSettings {
	public steps: string[] = [];

	public currentAction: string = '';
	public currentPage: number = 0;
	public currentStep: number = 0;

	public take: number = 0;
	public totalPages: number = 0;
	public totalRecords: number = 0;

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
