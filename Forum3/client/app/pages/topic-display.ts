import { App } from "../app";

import { Xhr } from "../services/xhr";
import { XhrOptions } from "../models/xhr-options";
import { postToPath, throwIfNull } from "../helpers";

export class TopicDisplay {
	private thoughtSelectorMessageId: string;
	private assignedBoards: string[];

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');
	}

	init() {
		let incomingBoards: string[] = (<any>window).assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.on('click', this.eventShowReplyForm)
		});

		this.doc.querySelectorAll('.thought-button').forEach(element => {
			element.on('click', this.eventShowThoughtSelector);
		});

		this.doc.querySelectorAll('blockquote.reply').forEach(element => {
			element.on('click', this.eventShowFullReply);
		});

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.on('click', this.eventToggleBoard);
		});

		if (!(<any>window).showFavicons) {
			this.doc.querySelectorAll('.link-favicon').forEach(element => {
				element.hide();
			});
		}
	}

	eventShowReplyForm = (event: Event) => {
		let target = <Element>event.currentTarget;

		this.doc.querySelectorAll('.reply-form').forEach(element => {
			element.hide();
		});

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.off('click', this.eventShowReplyForm);
			element.on('click', this.eventShowReplyForm);
		});

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.off('click', this.eventHideReplyForm);
		});

		target.off('click', this.eventShowReplyForm);
		target.closest('section').querySelectorAll('.reply-form').forEach(element => { element.show(); });
		target.on('click', this.eventHideReplyForm);
	}

	eventHideReplyForm = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.off('click', this.eventHideReplyForm);
		target.closest('section').querySelectorAll('.reply-form').forEach(element => { element.hide(); });
		target.on('click', this.eventShowReplyForm);
	}

	eventShowThoughtSelector = (event: Event) => {
		event.preventDefault();
		let target = <HTMLElement>event.currentTarget;
		this.thoughtSelectorMessageId = target.getAttribute('message-id');

		this.app.smileySelector.showSmileySelectorNearElement(target, this.eventAddThought);
	}

	eventShowFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.off('click', this.eventCloseFullReply);
		target.on('click', this.eventCloseFullReply);

		target.querySelectorAll('.reply-preview').forEach(element => { element.hide() });
		target.querySelectorAll('.reply-body').forEach(element => { element.show() });
	}

	eventCloseFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.querySelectorAll('.reply-body').forEach(element => { element.hide() });
		target.querySelectorAll('.reply-preview').forEach(element => { element.show() });

		target.off('click', this.eventShowFullReply);
		target.on('click', this.eventShowFullReply);
	}

	eventToggleBoard = (event: Event) => {
		event.stopPropagation();

		let target = <Element>event.currentTarget;
		let toggling = target.getAttribute('toggling');

		if (toggling) {
			return;
		}

		target.setAttribute('toggling', 'true');

		if ((<any>window).assignedBoards === undefined || (<any>window).togglePath === undefined) {
			return;
		}

		let boardId = target.getAttribute('board-id');
		let assignedBoardIndex: number = this.assignedBoards.indexOf(boardId, 0);

		let imgSrc = this.doc.querySelector(`[board-flag="${boardId}"]`).getAttribute('src');

		if (assignedBoardIndex > -1) {
			this.assignedBoards.splice(assignedBoardIndex, 1);
			imgSrc = imgSrc.replace('checked', 'unchecked');
		}
		else {
			this.assignedBoards.push(boardId);
			imgSrc = imgSrc.replace('unchecked', 'checked');
		}

		this.doc.querySelector(`[board-flag="${boardId}"]`).setAttribute('src', imgSrc);

		let request = Xhr.request(new XhrOptions({
			url: `${(<any>window).togglePath}&BoardId=${boardId}`			
		}));

		request.then(() => {
			target.removeAttribute('toggling');
		});
	}

	eventAddThought = (event: Event): void => {
		let smileyImg = <HTMLElement>event.currentTarget;
		let smileyId = smileyImg.getAttribute('smiley-id');

		postToPath('/Messages/AddThought', {
			'MessageId': this.thoughtSelectorMessageId,
			'SmileyId': smileyId
		});
	}
}
