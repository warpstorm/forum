import { postToPath, isEmpty } from "../helpers";

export class ManageBoards {
	private mergeFromBoardId: string = "";
	private mergeFromCategoryId: string = "";

	init() {
		document.querySelectorAll('.merge-board').forEach(element => {
			element.addEventListener('click', this.eventMergeBoard);
		});

		document.querySelectorAll('.merge-category').forEach(element => {
			element.addEventListener('click', this.eventMergeCategory);
		});
	}
	
	eventMergeBoard = (event: Event): void => {
		let target = <Element>event.currentTarget
		
		if (isEmpty(this.mergeFromBoardId)) {
			this.mergeFromBoardId = target.getAttribute('board-id') || "";

			(<Element>target.closest('.table-row')).querySelectorAll('.table-cell').forEach(element => {
				(<HTMLElement>element).style.backgroundColor = "#ACD";
			});
		} else {
			let mergeToBoardId = target.getAttribute('board-id');

			if (mergeToBoardId == this.mergeFromBoardId) {
				return;
			}

			postToPath("/Boards/MergeBoard", {
				"FromId": this.mergeFromBoardId,
				"ToId": mergeToBoardId
			});
		}
	};

	eventMergeCategory = (event: Event): void => {
		let target = <Element>event.currentTarget

		if (isEmpty(this.mergeFromCategoryId)) {
			this.mergeFromCategoryId = target.getAttribute('category-id') || '';

			(<HTMLElement>target.closest('h3')).style.backgroundColor = "#ACD";
		} else {
			let mergeToCategoryId = target.getAttribute('category-id');

			if (mergeToCategoryId == this.mergeFromCategoryId) {
				return;
			}

			postToPath("/Boards/MergeCategory", {
				"FromId": this.mergeFromCategoryId,
				"ToId": mergeToCategoryId
			});
		}
	};
}
