export class TopicDisplaySettings {
	public assignedBoards: string[] = [];
	public bookmarked: boolean = false;
	public currentPage: number = 0;
	public latest: number = 0;
	public messages: number[] = [];
	public pageActions: string = '';
	public showFavicons: boolean = false;
	public togglePath: string = '';
	public topicId: number = 0;
	public totalPages: number = 0;

	public constructor(init?: Partial<Window>) {
		Object.assign(this, init);
	}
}
