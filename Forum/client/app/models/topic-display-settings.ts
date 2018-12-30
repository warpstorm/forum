export class TopicDisplaySettings {
	public assignedBoards: string[] = [];
	public messages: number[] = [];
	public currentPage: number = 0;
	public latest: number = 0;
	public pageActions: string = '';
	public showFavicons: boolean = false;
	public sideloading: boolean = false;
	public togglePath: string = '';
	public topicId: number = 0;
	public totalPages: number = 0;

	public constructor(init?: Partial<Window>) {
		Object.assign(this, init);
	}
}
