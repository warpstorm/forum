import { App } from "../app";
import { postToPath, throwIfNull, hide, show, queryify } from "../helpers";

import { Xhr } from "../services/xhr";
import { XhrOptions } from "../models/xhr-options";
import { NewReply } from "../models/new-reply";
import { TopicDisplaySettings } from "../models/topic-display-settings";

import * as SignalR from "@aspnet/signalr";
import { HttpMethod } from "../definitions/http-method";
import { TokenRequestResponse } from "../models/token-request-response";
import { XhrResult } from "../models/xhr-result";

export class TopicDisplay {
	private hub?: SignalR.HubConnection = undefined;
	private settings: TopicDisplaySettings;
	private thoughtSelectorMessageId: string = "";
	private assignedBoards: string[] = [];
	private submitting: boolean = false;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');

		this.settings = new TopicDisplaySettings(window);
	}

	init(): void {
		if (this.settings.sideloading) {
			this.establishHubConnection();
		}

		let incomingBoards: string[] = this.settings.assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		this.bindMessageEventListeners();

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.addEventListener('click', this.eventToggleBoard);
		});

		this.doc.querySelectorAll('.message-form .save-button').forEach(element => {
			element.addEventListener('click', this.eventSaveMessage);
		});

		this.hideFavIcons();
	}

	establishHubConnection = () => {
		this.hub = new SignalR.HubConnectionBuilder().withUrl('/hub').build();
		this.hub.start()
			.then(this.bindHubActions)
			.catch(err => console.log('Error while starting connection: ' + err));

		console.log('Hub connection established.');
	}

	bindHubActions = () => {
		if (!this.hub) {
			throw new Error('Hub not defined.');
		}

		this.hub.on('newreply', this.hubNewReply);
	}

	bindMessageEventListeners(): void {
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

	hideFavIcons(): void {
		if (!this.settings.showFavicons) {
			this.doc.querySelectorAll('.link-favicon').forEach(element => {
				hide(element);
			});
		}
	}

	getLatestReplies(): void {
		console.log('Getting latest replies');

		let self = this;

		show(self.doc.querySelector('#loading-message'));

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/DisplayPartial/${self.settings.topicId}?latest=${self.settings.latest}`,
			responseType: 'document'
		});

		Xhr.request(requestOptions)
			.then((xhrResult) => {
				let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
				let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
				let resultBodyElements = resultBody.childNodes;
				let messageList = <Element>self.doc.querySelector('#message-list');

				resultBodyElements.forEach(node => {
					let element = node as Element;

					if (element && element.tagName) {
						if (element.tagName.toLowerCase() == 'script') {
							eval(element.textContent || '');
						}
						else {
							messageList.insertAdjacentElement('beforeend', element);
						}
					}
				});

				self.settings.latest = (<any>window).latest;
				let firstMessageId = (<any>window).firstMessageId;
				let newMessageCount = (<any>window).newMessageCount;

				self.bindMessageEventListeners();

				window.location.hash = `message${firstMessageId}`;

				console.log(`Xhr received ${newMessageCount} new messages.`);
			})
			.catch((reason) => {
				console.log(`Xhr was rejected: ${reason}`);
			})
			.then(() => {
				hide(self.doc.querySelector('#loading-message'));
			});
	}

	getToken(form: HTMLFormElement): string {
		let tokenElement = form.querySelector('[name=__RequestVerificationToken]') as HTMLInputElement;
		let returnToken = tokenElement ? tokenElement.value : '';

		let tokenRequestOptions = new XhrOptions({
			url: '/Home/Token'
		});

		Xhr.request(tokenRequestOptions)
			.then((xhrResult: XhrResult) => {
				let tokenRequestResponse: TokenRequestResponse = JSON.parse(xhrResult.responseText);
				tokenElement.value = tokenRequestResponse.token;
			})
			.catch(Xhr.logRejected);

		return returnToken;
	}

	hubNewReply = (data: NewReply) => {
		let self = this;

		if (data.topicId == self.settings.topicId
		&& self.settings.currentPage == self.settings.totalPages) {
			this.getLatestReplies();
		}
	}

	eventSaveMessage = (event: Event): void => {
		let self = this;

		// make sure the user has chosen to enable the hub connection.
		if (!self.hub) {
			return;
		}

		event.preventDefault();

		if (self.submitting) {
			return;
		}

		let saveButton = <HTMLElement>event.currentTarget;
		let form = <HTMLFormElement>saveButton.closest('form');
		let idElement = form.querySelector('[name=Id]') as HTMLInputElement;
		let bodyElement = form.querySelector('[name=body]') as HTMLTextAreaElement;

		saveButton.setAttribute('disabled', 'disabled');
		bodyElement.setAttribute('disabled', 'disabled');

		let requestBodyValues = {
			id: idElement ? idElement.value : '',
			body: bodyElement ? bodyElement.value : ''
		};

		if (requestBodyValues.body == '') {
			return;
		}

		self.submitting = true;

		let submitRequestOptions = new XhrOptions({
			method: HttpMethod.Post,
			url: form.action,
			body: queryify(requestBodyValues)
		});

		submitRequestOptions.headers['Content-Type'] = 'application/x-www-form-urlencoded;charset=UTF-8';
		submitRequestOptions.headers['RequestVerificationToken'] = self.getToken(form);

		show(self.doc.querySelector('#loading-message'));

		Xhr.request(submitRequestOptions)
			.then(() => {
				if (bodyElement) {
					bodyElement.value = '';
					bodyElement.removeAttribute('disabled');
				}

				new Promise(() => {
					self.doc.querySelectorAll('.reply-button').forEach(element => {
						element.removeEventListener('click', self.eventHideReplyForm);
						element.removeEventListener('click', self.eventShowReplyForm);
						element.addEventListener('click', self.eventShowReplyForm);
					});

					self.doc.querySelectorAll('.reply-form').forEach(element => { hide(element); });

					saveButton.removeAttribute('disabled');

					self.submitting = false;
				});
			})
			.catch(Xhr.logRejected);
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

		let requestOptions = new XhrOptions({
			url: `${(<any>window).togglePath}&BoardId=${boardId}`
		});

		Xhr.request(requestOptions)
			.then(() => {
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
