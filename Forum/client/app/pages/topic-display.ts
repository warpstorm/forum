import { HubConnectionState } from "@aspnet/signalr";
import { throwIfNull, hide, show, queryify, warning, clear } from "../helpers";
import { App } from "../app";
import { Xhr } from "../services/xhr";
import { HttpMethod } from "../definitions/http-method";

import { HubMessage } from "../models/hub-message";
import { ModelErrorResponse } from "../models/model-error-response";
import { TopicDisplaySettings } from "../models/topic-display-settings";
import { TokenRequestResponse } from "../models/token-request-response";
import { XhrOptions } from "../models/xhr-options";

import { TopicDisplayPartialSettings } from "../models/topic-display-partial-settings";

function getSettings(): TopicDisplaySettings {
	let genericWindow = <any>window;

	return new TopicDisplaySettings({
		assignedBoards: genericWindow.assignedBoards,
		bookmarked: genericWindow.bookmarked,
		currentPage: genericWindow.currentPage,
		latest: genericWindow.latest,
		messages: genericWindow.messages,
		pageActions: genericWindow.pageActions,
		showFavicons: genericWindow.showFavicons,
		togglePath: genericWindow.togglePath,
		topicId: genericWindow.topicId,
		totalPages: genericWindow.totalPages
	});
}

export class TopicDisplay {
	private settings: TopicDisplaySettings;
	private submitting: boolean = false;
	private thoughtSelectorMessageId: string = "";
	private thoughtTarget?: HTMLElement = undefined;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');

		this.settings = getSettings();

		// Ensures the first load also has the settings state.
		window.history.replaceState(this.settings, this.doc.title, window.location.href);
		window.onpopstate = this.eventPopState;

		if (this.app.hub) {
			this.app.hub.on('new-reply', this.hubNewReply);
			this.app.hub.on('updated-message', this.hubUpdatedMessage);
		}
	}

	init(): void {
		this.bindPageButtons();
		this.bindGlobalButtonHandlers();
		this.bindMessageButtonHandlers();
		this.hideFavIcons();

		this.app.navigation.setupPageNavigators();
		this.app.navigation.addListenerClickableLinkParent();
	}

	async loadPage(pageId: number, pushState: boolean = true) {
		let mainElement = <Element>this.doc.querySelector('main');
		mainElement.classList.add('faded');

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/Display/${this.settings.topicId}/${pageId}`
		});

		await Xhr.requestPartialView(requestOptions, this.doc);

		window.scrollTo(0, 0);
		mainElement.classList.remove('faded');

		this.settings = getSettings();

		if (pushState) {
			window.history.pushState(this.settings, this.doc.title, `/Topics/Display/${this.settings.topicId}/${this.settings.currentPage}`);
		}

		let topicReplyForm = <Element>this.doc.querySelector('.topic-reply-form');

		if (this.settings.currentPage == this.settings.totalPages) {
			show(topicReplyForm);
		}
		else {
			hide(topicReplyForm);
		}

		this.init();
	}

	bindPageButtons() {
		this.doc.querySelectorAll('.page a').forEach(element => {
			element.removeEventListener('click', this.eventPageClick);
			element.addEventListener('click', this.eventPageClick);
		});
	}

	bindMessageButtonHandlers(): void {
		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.eventShowReplyForm)
			element.addEventListener('click', this.eventShowReplyForm)
		});

		this.doc.querySelectorAll('.thought-button').forEach(element => {
			element.removeEventListener('click', this.eventShowThoughtSelector);
			element.addEventListener('click', this.eventShowThoughtSelector);
		});

		this.doc.querySelectorAll('.edit-button').forEach(element => {
			element.removeEventListener('click', this.eventShowEditForm);
			element.addEventListener('click', this.eventShowEditForm);
		});

		this.doc.querySelectorAll('blockquote.reply').forEach(element => {
			element.removeEventListener('click', this.eventShowFullReply);
			element.addEventListener('click', this.eventShowFullReply);
		});
	}

	bindGlobalButtonHandlers(): void {
		this.doc.querySelectorAll('.bookmark-button').forEach(element => {
			element.removeEventListener('mouseenter', this.eventToggleBookmarkImage);
			element.addEventListener('mouseenter', this.eventToggleBookmarkImage);
			element.removeEventListener('mouseleave', this.eventToggleBookmarkImage);
			element.addEventListener('mouseleave', this.eventToggleBookmarkImage);
		});

		this.doc.querySelectorAll('[toggle-board]').forEach(element => {
			element.removeEventListener('click', this.eventToggleBoard);
			element.addEventListener('click', this.eventToggleBoard);
		});

		this.doc.querySelectorAll('#topic-reply .save-button').forEach(element => {
			element.removeEventListener('click', this.eventSaveTopicReplyForm);
			element.addEventListener('click', this.eventSaveTopicReplyForm);
		});
	}

	hideFavIcons(): void {
		if (!this.settings.showFavicons) {
			hide('.link-favicon');
		}
	}

	async getLatestReplies(): Promise<void> {
		let self = this;

		show(self.doc.querySelector('#loading-message'));

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/DisplayPartial/${self.settings.topicId}?latest=${self.settings.latest}`,
			responseType: 'document'
		});

		let xhrResult = await Xhr.request(requestOptions);

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

		self.bindMessageButtonHandlers();

		var time = new Date();
		var passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

		hide(self.doc.querySelector('#loading-message'));

		warning(`<a href='#message${firstMessageId}'>New messages were posted <time datetime='${time}'>${passedTime}</time>.</a>`);
		window.location.hash = `message${firstMessageId}`;
	}

	async saveMessage(form: HTMLFormElement, success: () => void): Promise<void> {
		throwIfNull(form, 'form');

		let self = this;

		if (self.submitting) {
			return;
		}
		
		let bodyElement = form.querySelector('[name=body]') as HTMLTextAreaElement;

		if (!bodyElement || bodyElement.value == '') {
			return;
		}

		bodyElement.setAttribute('disabled', 'disabled');

		form.classList.add('faded');

		form.querySelectorAll('.button').forEach(element => {
			element.setAttribute('disabled', 'disabled');
		});

		let idElement = form.querySelector('[name=Id]') as HTMLInputElement;

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
		submitRequestOptions.headers['RequestVerificationToken'] = await self.getToken(form);

		let xhrResult = await Xhr.request(submitRequestOptions);

		let modelErrors: ModelErrorResponse[] = JSON.parse(xhrResult.responseText);

		if (modelErrors.length == 0) {
			success();
		}
		else {
			for (let i = 0; i < modelErrors.length; i++) {
				let modelErrorField = form.querySelector(`[data-valmsg-for="${modelErrors[i].propertyName}"]`);

				if (modelErrorField) {
					modelErrorField.textContent = modelErrors[i].errorMessage;
				}
			}
		}

		bodyElement.removeAttribute('disabled');

		form.querySelectorAll('.button').forEach(element => {
			element.removeAttribute('disabled');
		});

		form.classList.remove('faded');

		self.submitting = false;
	}

	async getToken(form: HTMLFormElement): Promise<string> {
		let tokenElement = form.querySelector('[name=__RequestVerificationToken]') as HTMLInputElement;
		let returnToken = tokenElement ? tokenElement.value : '';

		let tokenRequestOptions = new XhrOptions({
			url: '/Home/Token'
		});

		let xhrResult = await Xhr.request(tokenRequestOptions);

		let tokenRequestResponse: TokenRequestResponse = JSON.parse(xhrResult.responseText);
		tokenElement.value = tokenRequestResponse.token;

		return returnToken;
	}

	resetMessageReplyForms(): void {
		this.doc.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.resetMessageReplyForms);
			element.removeEventListener('click', this.eventShowReplyForm);
			element.addEventListener('click', this.eventShowReplyForm);
		});

		this.doc.querySelectorAll('.reply-form').forEach(element => {
			hide(element);
		});
	}

	hubNewReply = async (data: HubMessage): Promise<void> => {
		if (data.topicId == this.settings.topicId && this.settings.currentPage == this.settings.totalPages) {
			await this.getLatestReplies();
		}
	}

	hubUpdatedMessage = async (data: HubMessage): Promise<void> => {
		let self = this;

		if (data.topicId == self.settings.topicId
			&& self.settings.messages.indexOf(data.messageId) >= 0) {

			let requestOptions = new XhrOptions({
				method: HttpMethod.Get,
				url: `/Topics/DisplayOne/${data.messageId}`,
				responseType: 'document'
			});

			let xhrResult = await Xhr.request(requestOptions);

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

			self.bindMessageButtonHandlers();

			var time = new Date();
			var passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

			warning(`<a href='#message${data.messageId}'>A message was updated <time datetime='${time}'>${passedTime}</time>.</a>`);
		}
	}

	eventShowEditForm = async (event: Event): Promise<void> => {
		let self = this;

		// make sure the user has chosen to enable the hub connection.
		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
			return;
		}

		event.preventDefault();

		let button = <Element>event.currentTarget;

		button.removeEventListener('click', self.eventShowEditForm);
		button.addEventListener('click', self.eventHideEditForm);

		let messageId = button.getAttribute('message-id');

		let workingDots = self.doc.querySelector(`#working-${messageId}`);
		show(workingDots);

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Messages/EditPartial/${messageId}`
		});

		await Xhr.requestPartialView(requestOptions, self.doc);

		hide(workingDots);

		let saveButton = self.doc.querySelector(`#edit-message-${messageId} .save-button`);

		if (saveButton) {
			saveButton.addEventListener('click', self.eventSaveEditForm);
		}

		let cancelButton = self.doc.querySelector(`#edit-message-${messageId} .cancel-button`);

		if (cancelButton) {
			cancelButton.addEventListener('click', (event: Event) => {
				event.preventDefault();

				button.removeEventListener('click', self.eventHideEditForm);
				button.addEventListener('click', self.eventShowEditForm);

				let form = self.doc.querySelector(`#edit-message-${messageId}`) as HTMLElement;
				clear(form);
				hide(form);
			});
		}
	}

	eventHideEditForm = (event: Event) => {
		event.preventDefault();

		let button = <Element>event.currentTarget;

		button.removeEventListener('click', this.eventHideEditForm);
		button.addEventListener('click', this.eventShowEditForm);

		let messageId = button.getAttribute('message-id');
		let form = this.doc.querySelector(`#edit-message-${messageId}`) as HTMLElement;

		clear(form);
		hide(form);
	}

	eventSaveEditForm = async (event: Event): Promise<void> => {
		let self = this;

		// make sure the user has chosen to enable the hub connection.
		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
			return;
		}

		event.preventDefault();

		let button = <HTMLElement>event.currentTarget;
		let form = <HTMLFormElement>button.closest('form');
		let messageId = button.getAttribute('message-id');

		let workingDots = self.doc.querySelector(`#working-${messageId}`);
		show(workingDots);

		let onSuccess = () => {
			let form = this.doc.querySelector(`#edit-message-${messageId}`) as HTMLElement;
			clear(form);
			hide(form);

			hide(workingDots);
		};

		await self.saveMessage(form, onSuccess);
	}

	eventShowReplyForm = async (event: Event): Promise<void> => {
		let self = this;

		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
			return;
		}

		event.preventDefault();

		let button = <Element>event.currentTarget;

		button.removeEventListener('click', self.eventShowReplyForm);
		button.addEventListener('click', self.eventHideReplyForm);

		let messageId = button.getAttribute('message-id');

		let workingDots = self.doc.querySelector(`#working-${messageId}`);
		show(workingDots);

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Messages/ReplyPartial/${messageId}`
		});

		await Xhr.requestPartialView(requestOptions, self.doc);

		hide(workingDots);

		let saveButton = self.doc.querySelector(`#message-reply-${messageId} .save-button`);

		if (saveButton) {
			saveButton.addEventListener('click', self.eventSaveReplyForm);
		}

		let cancelButton = self.doc.querySelector(`#message-reply-${messageId} .cancel-button`);

		if (cancelButton) {
			cancelButton.addEventListener('click', (event: Event) => {
				event.preventDefault();

				button.removeEventListener('click', self.eventHideReplyForm);
				button.addEventListener('click', self.eventShowReplyForm);

				let form = self.doc.querySelector(`#message-reply-${messageId}`) as HTMLElement;
				clear(form);
				hide(form);
			});
		}
	}

	eventHideReplyForm = (event: Event) => {
		event.preventDefault();

		let button = <Element>event.currentTarget;

		button.removeEventListener('click', this.eventHideReplyForm);
		button.addEventListener('click', this.eventShowReplyForm);

		let messageId = button.getAttribute('message-id');
		let form = this.doc.querySelector(`#message-reply-${messageId}`) as HTMLElement;

		clear(form);
		hide(form);
	}

	eventSaveReplyForm = async (event: Event): Promise<void> => {
		let self = this;

		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
			return;
		}

		event.preventDefault();

		let button = <HTMLElement>event.currentTarget;
		let form = <HTMLFormElement>button.closest('form');
		let bodyElement = form.querySelector('[name=body]') as HTMLTextAreaElement;
		let messageId = button.getAttribute('message-id');

		let workingDots = self.doc.querySelector(`#working-${messageId}`);
		show(workingDots);

		let onSuccess = () => {
			let form = this.doc.querySelector(`#message-reply-${messageId}`) as HTMLElement;
			clear(form);
			hide(form);

			self.doc.querySelectorAll('.reply-button').forEach(element => {
				element.removeEventListener('click', self.resetMessageReplyForms);
				element.removeEventListener('click', self.eventShowReplyForm);
				element.addEventListener('click', self.eventShowReplyForm);
			});

			self.doc.querySelectorAll('.reply-form').forEach(element => {
				hide(element);
			});

			if (bodyElement) {
				bodyElement.value = '';
			}

			hide(workingDots);
		};

		await self.saveMessage(form, onSuccess);
	}

	eventSaveTopicReplyForm = async (event: Event): Promise<void> => {
		let self = this;

		if (!self.app.hub || self.app.hub.state == HubConnectionState.Disconnected) {
			return;
		}

		event.preventDefault();

		let button = <HTMLElement>event.currentTarget;
		let form = <HTMLFormElement>button.closest('form');

		let onSuccess = () => {
			let bodyElement = form.querySelector('[name=body]') as HTMLTextAreaElement;

			if (bodyElement) {
				bodyElement.value = '';
			}
		};

		await self.saveMessage(form, onSuccess);
	}

	eventShowThoughtSelector = (event: Event) => {
		event.preventDefault();
		let target = <HTMLElement>event.currentTarget;
		this.thoughtTarget = <HTMLElement>target.closest('article');
		this.thoughtSelectorMessageId = target.getAttribute('message-id') || '';
		this.app.smileySelector.showSmileySelectorNearElement(target, this.eventSaveThought);
	}

	eventSaveThought = (event: Event): void => {
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

			Xhr.request(requestOptions);

			this.app.smileySelector.eventCloseSmileySelector();
		}
		else {
			window.location.href = `/Messages/AddThought/${this.thoughtSelectorMessageId}?smiley=${smileyId}`;
		}
	}

	eventShowFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.removeEventListener('click', this.eventShowFullReply);
		target.addEventListener('click', this.eventHideFullReply);

		target.querySelectorAll('.reply-preview').forEach(element => { hide(element) });
		target.querySelectorAll('.reply-body').forEach(element => { show(element) });
	}

	eventHideFullReply = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.querySelectorAll('.reply-body').forEach(element => { hide(element) });
		target.querySelectorAll('.reply-preview').forEach(element => { show(element) });

		target.removeEventListener('click', this.eventHideFullReply);
		target.addEventListener('click', this.eventShowFullReply);
	}

	eventToggleBoard = async (event: Event) => {
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

		let assignedBoardIndex: number = this.settings.assignedBoards.indexOf(boardId, 0);

		let imgSrc = boardFlag.getAttribute('src') || '';

		if (assignedBoardIndex > -1) {
			this.settings.assignedBoards.splice(assignedBoardIndex, 1);
			imgSrc = imgSrc.replace('checked', 'unchecked');
		}
		else {
			this.settings.assignedBoards.push(boardId);
			imgSrc = imgSrc.replace('unchecked', 'checked');
		}

		boardFlag.setAttribute('src', imgSrc);

		let requestOptions = new XhrOptions({
			url: `${(<any>window).togglePath}&BoardId=${boardId}`
		});

		await Xhr.request(requestOptions);

		target.removeAttribute('toggling');
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

	eventPageClick = (event: Event) => {
		let eventTarget = event.currentTarget as HTMLAnchorElement;

		if (!eventTarget) {
			return;
		}

		event.preventDefault();

		let pageId = Number(eventTarget.getAttribute('data-page-id'));
		this.loadPage(pageId);
	}

	eventPopState = (event: PopStateEvent) => {
		var settings = event.state as TopicDisplaySettings;

		if (settings) {
			this.settings = settings;
			this.loadPage(this.settings.currentPage, false);
		}
	}
}
