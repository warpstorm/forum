export class XhrResult {
	status: number = -1;
	statusText: string = "";
	response: any = null;
	responseText: string = "";

	public constructor(init?: Partial<XhrResult>) {
		Object.assign(this, init);
	}
}
