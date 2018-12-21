import { App } from "../app";

import { Xhr } from "../services/xhr";
import { XhrOptions } from "../models/xhr-options";
import { postToPath, throwIfNull, hide, show } from "../helpers";

import * as SignalR from "@aspnet/signalr";

export class TopicDisplay {
	private thoughtSelectorMessageId: string = "";
	private assignedBoards: string[] = [];
	private topicHub!: SignalR.HubConnection;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');
	}

	init() {
		this.startConnection();
		this.addReplyListener();

		let incomingBoards: string[] = (<any>window).assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.addEventListener('click', this.eventShowReplyForm)
		});

		this.doc.querySelectorAll('.thought-button').forEach(element => {
			element.addEventListener('click', this.eventShowThoughtSelector);
		});

		this.doc.querySelectorAll('blockquote.reply').forEach(element => {
			element.addEventListener('click', this.eventShowFullReply);
		});

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.addEventListener('click', this.eventToggleBoard);
		});

		if (!(<any>window).showFavicons) {
			this.doc.querySelectorAll('.link-favicon').forEach(element => {
				hide(element);
			});
		}
	}

	startConnection = () => {
		this.topicHub = new SignalR.HubConnectionBuilder()
			.withUrl('http://localhost:31415/hub/topics')
			.build();

		this.topicHub
			.start()
			.then(() => console.log('Connection started'))
			.catch(err => console.log('Error while starting connection: ' + err))
	}

	addReplyListener = () => {
		this.topicHub.on('newreply', (data) => {
			console.log(data);
		});
	}

	eventShowReplyForm = (event: Event) => {
		this.doc.querySelectorAll('.reply-form').forEach(element => {
			hide(element);
		});

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.eventShowReplyForm);
			element.addEventListener('click', this.eventShowReplyForm);
		});

		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.eventHideReplyForm);
		});

		let target = <Element>event.currentTarget;
		target.removeEventListener('click', this.eventShowReplyForm);
		(<Element>target.closest('section')).querySelectorAll('.reply-form').forEach(element => { show(element); });
		target.addEventListener('click', this.eventHideReplyForm);
	}

	eventHideReplyForm = (event: Event) => {
		let target = <Element>event.currentTarget;
		target.removeEventListener('click', this.eventHideReplyForm);
		(<Element>target.closest('section')).querySelectorAll('.reply-form').forEach(element => { hide(element); });
		target.addEventListener('click', this.eventShowReplyForm);
	}

	eventShowThoughtSelector = (event: Event) => {
		event.preventDefault();
		let target = <HTMLElement>event.currentTarget;
		this.thoughtSelectorMessageId = target.getAttribute('message-id') || '';
		this.app.smileySelector.showSmileySelectorNearElement(target, this.eventAddThought);
	}

	eventShowFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.removeEventListener('click', this.eventShowFullReply);
		target.addEventListener('click', this.eventCloseFullReply);

		target.querySelectorAll('.reply-preview').forEach(element => { hide(element) });
		target.querySelectorAll('.reply-body').forEach(element => { show(element) });
	}

	eventCloseFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.querySelectorAll('.reply-body').forEach(element => { hide(element) });
		target.querySelectorAll('.reply-preview').forEach(element => { show(element) });

		target.removeEventListener('click', this.eventCloseFullReply);
		target.addEventListener('click', this.eventShowFullReply);
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

		if (!boardId) {
			return;
		}

		let boardFlag = this.doc.querySelector(`[board-flag="${boardId}"]`);

		if (!boardFlag) {
			return;
		}

		let assignedBoardIndex: number = this.assignedBoards.indexOf(boardId, 0);

		let imgSrc = boardFlag.getAttribute('src') || '';

		if (assignedBoardIndex > -1) {
			this.assignedBoards.splice(assignedBoardIndex, 1);
			imgSrc = imgSrc.replace('checked', 'unchecked');
		}
		else {
			this.assignedBoards.push(boardId);
			imgSrc = imgSrc.replace('unchecked', 'checked');
		}

		boardFlag.setAttribute('src', imgSrc);

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
