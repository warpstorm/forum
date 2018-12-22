import { App } from "../app";
import { postToPath, throwIfNull, hide, show, queryify } from "../helpers";

import { Xhr } from "../services/xhr";
import { XhrOptions } from "../models/xhr-options";
import { NewReply } from "../models/new-reply";
import { TopicDisplayWindow } from "../models/topic-display-window";

import * as SignalR from "@aspnet/signalr";
import { HttpMethod } from "../definitions/http-method";
import { ResponseToken } from "../models/response-token";

export class TopicDisplay {
	private hub!: SignalR.HubConnection;
	private topicWindow: TopicDisplayWindow;
	private thoughtSelectorMessageId: string = "";
	private assignedBoards: string[] = [];
	private submitting: boolean = false;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');

		this.topicWindow = new TopicDisplayWindow(window);
	}

	init() {
		this.establishHubConnection();

		let incomingBoards: string[] = this.topicWindow.assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		this.bindMessageEventListeners();

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.addEventListener('click', this.eventToggleBoard);
		});

		this.hideFavIcons();
	}

	establishHubConnection = () => {
		this.hub = new SignalR.HubConnectionBuilder().withUrl('/hub').build();
		this.hub.start()
			.then(this.finalizeHubConnection)
			.catch(err => console.log('Error while starting connection: ' + err));
	}

	finalizeHubConnection = () => {
		console.log('Hub connection established');

		this.hub.on('newreply', this.hubNewReply);

		this.doc.querySelectorAll('.message-form .save-button').forEach(element => {
			element.addEventListener('click', this.eventSaveMessage);
		});
	}

	bindMessageEventListeners() {
		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.eventShowReplyForm)
			element.addEventListener('click', this.eventShowReplyForm)
		});

		this.doc.querySelectorAll('.thought-button').forEach(element => {
			element.removeEventListener('click', this.eventShowThoughtSelector);
			element.addEventListener('click', this.eventShowThoughtSelector);
		});

		this.doc.querySelectorAll('blockquote.reply').forEach(element => {
			element.removeEventListener('click', this.eventShowFullReply);
			element.addEventListener('click', this.eventShowFullReply);
		});
	}

	hideFavIcons() {
		if (!this.topicWindow.showFavicons) {
			this.doc.querySelectorAll('.link-favicon').forEach(element => {
				hide(element);
			});
		}
	}

	hubNewReply = (data: NewReply) => {
		if (data.topicId == this.topicWindow.topicId) {
			let request = Xhr.request(new XhrOptions({
				method: HttpMethod.Get,
				url: `/Topics/MessagePartial/${data.messageId}`,
				responseType: 'document'
			}));

			request.then((xhrResult) => {
				let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
				let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
				let resultBodyElements = resultBody.childNodes;

				resultBodyElements.forEach(node => {
					let element = <Element>node;

					if (element.tagName.toLowerCase() == 'script') {
						eval(element.textContent || '');
					}
					else {
						let messageList = <Element>this.doc.querySelector('#message-list');
						messageList.insertAdjacentElement('beforeend', element);
					}
				});

				this.bindMessageEventListeners();

				window.location.hash = `message${data.messageId}`;
			});
		}
	}

	eventSaveMessage = (event: Event) => {
		event.preventDefault();

		if (this.submitting) {
			return;
		}

		this.submitting = true;

		let target = <HTMLElement>event.currentTarget;
		target.innerHTML = target.textContent + ' <img src="/images/loadingDots.gif" style="vertical-align: middle" />';
		target.setAttribute('disabled', 'disabled');

		let form = <HTMLFormElement>target.closest('form');

		let tokenElement = form.querySelector('[name=__RequestVerificationToken]') as HTMLInputElement;
		let idElement = form.querySelector('[name=Id]') as HTMLInputElement;
		let bodyElement = form.querySelector('[name=body]') as HTMLTextAreaElement;

		let formBody = {
			id: idElement ? idElement.value : '',
			body: bodyElement ? bodyElement.value : ''
		};

		let submitRequestOptions = new XhrOptions({
			method: HttpMethod.Post,
			url: form.action,
			body: queryify(formBody)
		});

		submitRequestOptions.headers['Content-Type'] = 'application/x-www-form-urlencoded;charset=UTF-8';
		submitRequestOptions.headers['RequestVerificationToken'] = tokenElement ? tokenElement.value : '';

		let submitRequest = Xhr.request(submitRequestOptions);

		submitRequest.then(() => {
			let tokenRequest = Xhr.request(new XhrOptions({
				url: '/Home/Token'
			}));

			if (bodyElement) {
				bodyElement.value = '';
			}

			this.doc.querySelectorAll('.reply-button').forEach(element => {
				element.removeEventListener('click', this.eventHideReplyForm);
				element.removeEventListener('click', this.eventShowReplyForm);
				element.addEventListener('click', this.eventShowReplyForm);
			});

			this.doc.querySelectorAll('.reply-form').forEach(element => { hide(element); });

			tokenRequest.then((xhrResult) => {
				var responseToken: ResponseToken = JSON.parse(xhrResult.responseText);
				tokenElement.value = responseToken.token;
				target.removeAttribute('disabled');
				target.innerHTML = target.textContent || "";

				this.submitting = false;
			});
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
