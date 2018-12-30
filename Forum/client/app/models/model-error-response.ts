export class ModelErrorResponse {
	propertyName: string = '';
	errorMessage: string = '';

	public constructor(init?: Partial<ModelErrorResponse>) {
		Object.assign(this, init);
	}
}
