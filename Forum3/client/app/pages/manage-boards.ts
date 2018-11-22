import { App } from "../app";

import { postToPath, throwIfNull } from "../helpers";

export class ManageBoards {
	private mergeFromBoardId: string;
	private mergeFromCategoryId: string;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
	}

	init() {
		this.doc.querySelectorAll('.merge-board').forEach(element => {
			element.on('click', this.eventMergeBoard);
		});

		this.doc.querySelectorAll('.merge-category').forEach(element => {
			element.on('click', this.eventMergeCategory);
		});
	}
	
	eventMergeBoard = (event: Event): void => {
		let target = <Element>event.currentTarget

		if (!this.mergeFromBoardId || this.mergeFromBoardId == "") {
			this.mergeFromBoardId = target.getAttribute('board-id');

			target.closest('.table-row').querySelectorAll('.table-cell').forEach(element => {
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

		if (!this.mergeFromCategoryId || this.mergeFromCategoryId == "") {
			this.mergeFromCategoryId = target.getAttribute('category-id');

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
