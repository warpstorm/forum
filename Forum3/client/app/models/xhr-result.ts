export class XhrResult {
	status: number;
	statusText: string;
	data: string;

	public constructor(init?: Partial<XhrResult>) {
		Object.assign(this, init);
	}
}
