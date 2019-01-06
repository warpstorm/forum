import { HubConnectionState } from "@aspnet/signalr";
import { throwIfNull, hide, show, queryify, warning } from "../helpers";
import { App } from "../app";
import { HttpMethod } from "../definitions/http-method";
import { Xhr } from "../services/xhr";

import { HubMessage } from "../models/hub-message";
import { ModelErrorResponse } from "../models/model-error-response";
import { TopicDisplaySettings } from "../models/topic-display-settings";
import { TokenRequestResponse } from "../models/token-request-response";
import { XhrOptions } from "../models/xhr-options";
import { XhrResult } from "../models/xhr-result";

import { TopicDisplayPartialSettings } from "../models/topic-display-partial-settings";

export class TopicDisplay {
	private assignedBoards: string[] = [];
	private settings: TopicDisplaySettings;
	private submitting: boolean = false;
	private thoughtSelectorMessageId: string = "";
	private thoughtTarget?: HTMLElement = undefined;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');

		this.settings = new TopicDisplaySettings(window);
	}

	init(): void {
		if (this.app.hub) {
			this.bindHubActions();
		}

		let incomingBoards: string[] = this.settings.assignedBoards;

		if (incomingBoards && incomingBoards.length > 0) {
			this.assignedBoards = incomingBoards;
		}

		this.bindMessageEventListeners();

		this.doc.querySelectorAll('.bookmark-button').forEach(element => {
			element.addEventListener('mouseenter', this.eventToggleBookmarkImage);
			element.addEventListener('mouseleave', this.eventToggleBookmarkImage);
		});

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.addEventListener('click', this.eventToggleBoard);
		});

		this.doc.querySelectorAll('.message-form .save-button').forEach(element => {
			element.addEventListener('click', this.eventSaveMessage);
		});

		this.hideFavIcons();
	}

	bindHubActions = () => {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('new-reply', this.hubNewReply);
		this.app.hub.on('updated-message', this.hubUpdatedMessage);
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

				let partialSettings = new TopicDisplayPartialSettings(window);

				self.settings.latest = partialSettings.latest;
				let firstMessageId = partialSettings.firstMessageId;
				let newMessages = partialSettings.newMessages;

				for (let i = 0; i < newMessages.length; i++) {
					self.settings.messages.push(newMessages[i]);
				}

				self.bindMessageEventListeners();

				var time = new Date();
				var passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

				warning(`<a href='#message${firstMessageId}'>New messages were posted <time datetime='${time}'>${passedTime}</time>.</a>`);

				window.location.hash = `message${firstMessageId}`;
			})
			.catch(Xhr.logRejected)
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

	hubNewReply = (data: HubMessage) => {
		if (data.topicId == this.settings.topicId && this.settings.currentPage == this.settings.totalPages) {
			this.getLatestReplies();
		}
	}

	hubUpdatedMessage = (data: HubMessage) => {
		let self = this;

		if (data.topicId == self.settings.topicId
			&& self.settings.messages.indexOf(data.messageId) >= 0) {

			let requestOptions = new XhrOptions({
				method: HttpMethod.Get,
				url: `/Topics/DisplayOne/${data.messageId}`,
				responseType: 'document'
			});

			Xhr.request(requestOptions)
				.then((xhrResult) => {
					let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
					let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
					let resultBodyElements = resultBody.childNodes;
					let targetArticle = <Element>self.doc.querySelector(`article[message="${data.messageId}"]`);

					resultBodyElements.forEach(node => {
						let element = node as Element;

						if (element && element.tagName && element.tagName.toLowerCase() == 'article') {
							targetArticle.after(element);
							targetArticle.remove();
						}
					});

					self.bindMessageEventListeners();

					var time = new Date();
					var passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

					warning(`<a href='#message${data.messageId}'>A message was updated <time datetime='${time}'>${passedTime}</time>.</a>`);
				})
				.catch(Xhr.logRejected);
		}
	}

	eventSaveMessage = (event: Event): void => {
		let self = this;

		// make sure the user has chosen to enable the hub connection.
		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
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

		if (!bodyElement || bodyElement.value == '') {
			return;
		}

		form.classList.add('faded');
		saveButton.setAttribute('disabled', 'disabled');
		bodyElement.setAttribute('disabled', 'disabled');

		let requestBodyValues = {
			id: idElement ? idElement.value : '',
			body: bodyElement ? bodyElement.value : '',
			sideload: true
		};

		self.submitting = true;

		let submitRequestOptions = new XhrOptions({
			method: HttpMethod.Post,
			url: form.action,
			body: queryify(requestBodyValues)
		});

		submitRequestOptions.headers['Content-Type'] = 'application/x-www-form-urlencoded;charset=UTF-8';
		submitRequestOptions.headers['RequestVerificationToken'] = self.getToken(form);

		Xhr.request(submitRequestOptions)
			.then((xhrResult) => {
				let modelErrors: ModelErrorResponse[] = JSON.parse(xhrResult.responseText);

				for (let i = 0; i < modelErrors.length; i++) {
					let modelErrorField = form.querySelector(`[data-valmsg-for="${modelErrors[i].propertyName}"]`);

					if (modelErrorField) {
						modelErrorField.textContent = modelErrors[i].errorMessage;
					}
				}

				if (modelErrors.length == 0) {
					new Promise(() => {
						self.doc.querySelectorAll('.reply-button').forEach(element => {
							element.removeEventListener('click', self.eventHideReplyForm);
							element.removeEventListener('click', self.eventShowReplyForm);
							element.addEventListener('click', self.eventShowReplyForm);
						});

						self.doc.querySelectorAll('.reply-form').forEach(element => { hide(element); });
					});
				}
			})
			.catch(Xhr.logRejected)
			.then(() => {
				form.classList.remove('faded');

				if (bodyElement) {
					bodyElement.value = '';
					bodyElement.removeAttribute('disabled');
				}

				saveButton.removeAttribute('disabled');
				self.submitting = false;
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
		this.thoughtTarget = <HTMLElement>target.closest('article');
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
		if (this.thoughtTarget) {
			this.thoughtTarget.classList.add('faded');
		}

		let smileyImg = <HTMLElement>event.currentTarget;
		let smileyId = smileyImg.getAttribute('smiley-id');

		// Only send an XHR if we anticipate the thought will be returned via the hub.
		if (this.app.hub && this.app.hub.state == HubConnectionState.Connected) {
			let requestOptions = new XhrOptions({
				method: HttpMethod.Get,
				url: `/Messages/AddThought/${this.thoughtSelectorMessageId}?smiley=${smileyId}`
			});

			Xhr.request(requestOptions).catch(Xhr.logRejected);

			this.app.smileySelector.eventCloseSmileySelector();
		}
		else {
			window.location.href = `/Messages/AddThought/${this.thoughtSelectorMessageId}?smiley=${smileyId}`;
		}
	}

	eventToggleBookmarkImage = (event: Event): void => {
		var bookmarkImageSpan = <HTMLElement>event.currentTarget;
		var bookmarkImage = bookmarkImageSpan.querySelector('img');

		if (bookmarkImage) {
			var status = this.settings.bookmarked ? 'on' : 'off';

			if (bookmarkImage.src.includes('hover')) {
				bookmarkImage.src = bookmarkImage.src.replace('hover', status);
			}
			else {
				bookmarkImage.src = bookmarkImage.src.replace(status, 'hover');
			}
		}
	}
}
