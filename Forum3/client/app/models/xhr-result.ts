export class XhrResult {
	status: number;
	statusText: string;
	response: any = null;
	responseText: string;

	public constructor(init?: Partial<XhrResult>) {
		Object.assign(this, init);
	}
}
