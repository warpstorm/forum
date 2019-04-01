export class XhrException {
	ClassName: string = "";
	Message: string = "";
	StackTraceString: string = "";

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
