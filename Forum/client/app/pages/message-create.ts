export class MessageCreate {
	private toggleLock: boolean = false;
	private assignedBoards: string[] = [];

	init() {
		let incomingBoards: string[] = (<any>window).assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		document.querySelectorAll('[toggle-board]').forEach(element => {
			element.addEventListener('click', this.eventToggleBoard);
		});
	}

	eventToggleBoard = (event: Event): void => {
		event.stopPropagation();

		if (this.toggleLock) {
			return;
		}

		this.toggleLock = true;

		let target = <Element>event.currentTarget

		let boardId = <string>target.getAttribute('board-id');
		let boardFlagElement = <Element>document.querySelector(`[board-flag="${boardId}"]`);
		let imgSrc = <string>boardFlagElement.getAttribute('src');

		let assignedBoardIndex: number = this.assignedBoards.indexOf(boardId, 0);
		let checkbox = <HTMLInputElement>document.querySelector(`input[name="Selected_${boardId}"]`);

		if (assignedBoardIndex > -1) {
			this.assignedBoards.splice(assignedBoardIndex, 1);
			imgSrc = imgSrc.replace('checked', 'unchecked');
			checkbox.value = "False";
		}
		else {
			this.assignedBoards.push(boardId);
			imgSrc = imgSrc.replace('unchecked', 'checked');
			checkbox.value = "True";
		}

		boardFlagElement.setAttribute('src', imgSrc);

		this.toggleLock = false;
	};
}
