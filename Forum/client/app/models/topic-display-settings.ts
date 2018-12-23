export class TopicDisplaySettings {
	public sideloading: boolean = false;
	public topicId: number = 0;
	public pageActions: string = '';
	public currentPage: number = 0;
	public totalPages: number = 0;
	public showFavicons: boolean = false;
	public togglePath: string = '';
	public assignedBoards: string[] = [];

	public constructor(init?: Partial<Window>) {
		Object.assign(this, init);
	}
}
