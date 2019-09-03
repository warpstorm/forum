import { HubConnectionState } from "@aspnet/signalr";
import { throwIfNull, hide, show, queryify, warning, clear } from "../helpers";
import { App } from "../app";
import { Xhr } from "../services/xhr";
import { HttpMethod } from "../definitions/http-method";
import { HubMessage } from "../models/hub-message";
import { TokenRequestResponse } from "../models/token-request-response";
import { XhrOptions } from "../models/xhr-options";
import { TopicDisplaySettings } from "../models/page-settings/topic-display-settings";
import { TopicDisplayPartialSettings } from "../models/page-settings/topic-display-partial-settings";

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
	private body: HTMLBodyElement;

	constructor(private app: App) {
		throwIfNull(app, 'app');
		throwIfNull(app.smileySelector, 'app.smileySelector');

		this.settings = getSettings();

		// Ensures the first load also has the settings state.
		window.history.replaceState(this.settings, document.title, window.location.href);
		window.onpopstate = this.eventPopState;

		if (this.app.hub) {
			this.app.hub.on('new-reply', this.hubNewReply);
			this.app.hub.on('updated-message', this.hubUpdatedMessage);
			this.app.hub.on('deleted-message', this.hubDeletedMessage);
			this.app.hub.on('deleted-topic', this.hubDeletedTopic);
		}

		this.body = document.getElementsByTagName('body')[0];
	}

	init(): void {
		this.bindPageButtons();
		this.bindGlobalButtonHandlers();
		this.bindMessageButtonHandlers();
		this.hideFavIcons();

		this.app.navigation.setupPageNavigators();
		this.app.navigation.addListenerClickableLinkParent();
	}

	async loadPage(page: number, pushState: boolean = true) {
		let mainElement = <Element>document.querySelector('main');
		mainElement.classList.add('faded');

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/Display/${this.settings.topicId}/${page}`
		});

		await Xhr.loadView(requestOptions, document);

		window.scrollTo(0, 0);
		mainElement.classList.remove('faded');

		this.settings = getSettings();

		if (pushState) {
			window.history.pushState(this.settings, document.title, `/Topics/Display/${this.settings.topicId}/${this.settings.currentPage}`);
		}

		let topicReplyForm = <Element>document.querySelector('.topic-reply-form');

		if (this.settings.currentPage == this.settings.totalPages) {
			show(topicReplyForm);
		}
		else {
			hide(topicReplyForm);
		}

		this.init();
	}

	bindPageButtons() {
		document.querySelectorAll('.page a').forEach(element => {
			element.removeEventListener('click', this.eventPageClick);
			element.addEventListener('click', this.eventPageClick);
		});
	}

	bindMessageButtonHandlers(): void {
		document.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.eventShowReplyForm)
			element.addEventListener('click', this.eventShowReplyForm)
		});

		document.querySelectorAll('.thought-button').forEach(element => {
			element.removeEventListener('click', this.eventShowThoughtSelector);
			element.addEventListener('click', this.eventShowThoughtSelector);

			element.querySelectorAll('[data-component="smiley-image"]').forEach(imgElement => {
				imgElement.removeEventListener('click', this.eventSaveThought);
				imgElement.addEventListener('click', this.eventSaveThought);
			});
		});

		document.querySelectorAll('.edit-button').forEach(element => {
			element.removeEventListener('click', this.eventShowEditForm);
			element.addEventListener('click', this.eventShowEditForm);
		});

		document.querySelectorAll('.delete-button').forEach(element => {
			element.removeEventListener('click', this.eventDeleteMessage);
			element.addEventListener('click', this.eventDeleteMessage);
		});

		document.querySelectorAll('blockquote.reply').forEach(element => {
			element.removeEventListener('click', this.eventShowFullReply);
			element.addEventListener('click', this.eventShowFullReply);
		});
	}

	bindGlobalButtonHandlers(): void {
		document.querySelectorAll('.bookmark-button').forEach(element => {
			element.removeEventListener('mouseenter', this.eventToggleBookmarkImage);
			element.addEventListener('mouseenter', this.eventToggleBookmarkImage);
			element.removeEventListener('mouseleave', this.eventToggleBookmarkImage);
			element.addEventListener('mouseleave', this.eventToggleBookmarkImage);
		});

		document.querySelectorAll('[toggle-board]').forEach(element => {
			element.removeEventListener('click', this.eventToggleBoard);
			element.addEventListener('click', this.eventToggleBoard);
		});

		document.querySelectorAll('#topic-reply .save-button').forEach(element => {
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

		show(document.querySelector('#loading-message'));

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/DisplayPartial/${self.settings.topicId}?latest=${self.settings.latest}`,
			responseType: 'document'
		});

		let xhrResult = await Xhr.request(requestOptions);

		let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
		let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
		let resultBodyElements = resultBody.childNodes;
		let messageList = <Element>document.querySelector('#message-list');

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

		let time = new Date();
		let passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

		hide(document.querySelector('#loading-message'));

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
		let topicIdElement = form.querySelector('[name=TopicId]') as HTMLInputElement;
		let disableMergingElement = form.querySelector('[name="ReplyForm.DisableMerging"]') as HTMLInputElement;

		let requestBodyValues = {
			id: idElement ? idElement.value : '',
			topicId: topicIdElement ? topicIdElement.value : '',
			body: bodyElement ? bodyElement.value : '',
			disableMerging: disableMergingElement ? disableMergingElement.checked : false
		};

		self.submitting = true;

		let submitRequestOptions = new XhrOptions({
			method: HttpMethod.Post,
			url: form.action,
			body: queryify(requestBodyValues)
		});

		submitRequestOptions.headers['RequestVerificationToken'] = await self.getToken(form);

		let xhrResult = await Xhr.request(submitRequestOptions);

		if (xhrResult.status == 200) {
			success();
		}
		else {
			try {
				let modelErrors = JSON.parse(xhrResult.responseText);

				for (let i = 0; i < modelErrors.length; i++) {
					let modelErrorField = form.querySelector(`[data-valmsg-for="${modelErrors[i].propertyName}"]`);

					if (modelErrorField) {
						modelErrorField.textContent = modelErrors[i].errorMessage;
					}
				}
			}
			catch {
				console.log(xhrResult.responseText);
			}
		}

		bodyElement.removeAttribute('disabled');
		disableMergingElement.checked = false;

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
		document.querySelectorAll('.reply-button').forEach(element => {
			element.removeEventListener('click', this.resetMessageReplyForms);
			element.removeEventListener('click', this.eventShowReplyForm);
			element.addEventListener('click', this.eventShowReplyForm);
		});

		document.querySelectorAll('.reply-form').forEach(element => {
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
				url: `/Messages/Display/${data.messageId}`,
				responseType: 'document'
			});

			let xhrResult = await Xhr.request(requestOptions);

			let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
			let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
			let resultBodyElements = resultBody.childNodes;
			let targetArticle = <Element>document.querySelector(`article[message="${data.messageId}"]`);

			resultBodyElements.forEach(node => {
				let element = node as Element;

				if (element && element.tagName && element.tagName.toLowerCase() == 'article') {
					targetArticle.after(element);
					targetArticle.remove();
				}
			});

			self.bindMessageButtonHandlers();
			self.app.navigation.showScriptFunctionality();

			let time = new Date();
			let passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

			warning(`<a href='#message${data.messageId}'>A message was updated <time datetime='${time}'>${passedTime}</time>.</a>`);
		}
	}

	hubDeletedMessage = async (data: HubMessage): Promise<void> => {
		let self = this;

		if (data.topicId == self.settings.topicId && self.settings.messages.indexOf(data.messageId) >= 0) {
			let targetArticle = <HTMLElement>document.querySelector(`article[message="${data.messageId}"]`);
			let userAvatar = <HTMLElement>targetArticle.querySelector('.user-avatar');
			let messageContents = <HTMLElement>targetArticle.querySelector('.message-contents');

			userAvatar.remove();
			messageContents.classList.add('faded');
			messageContents.innerHTML = '<p class="font-small subdued-text">This message was removed.</p>';

			document.querySelectorAll(`[reply="${data.messageId}"]`).forEach(element => {
				element.innerHTML = '<p class="font-small subdued-text">This message was removed.</p>';
			});

			let time = new Date();
			let passedTime = this.app.passedTimeMonitor.convertToPassedTime(time);

			warning(`<a href='#message${data.messageId}'>A message was removed <time datetime='${time}'>${passedTime}</time>.</a>`);
		}
	}

	hubDeletedTopic = async (data: HubMessage): Promise<void> => {
		if (data.topicId == this.settings.topicId) {
			let targetElement = <HTMLElement>document.querySelector('main');
			targetElement.innerHTML = '<div class="content-box pad"><p class="font-small subdued-text">This topic was removed.</p></div>';
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

		let workingDots = document.querySelector(`#working-${messageId}`);
		show(workingDots);

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Messages/XhrEdit/${messageId}`
		});

		await Xhr.loadView(requestOptions, document);

		hide(workingDots);

		self.app.navigation.showScriptFunctionality();
		self.app.bbCode.init();
		self.app.smileySelector.init();
		self.app.reactionSelector.init();

		let saveButton = document.querySelector(`#edit-message-${messageId} .save-button`);

		if (saveButton) {
			saveButton.addEventListener('click', self.eventSaveEditForm);
		}

		let cancelButton = document.querySelector(`#edit-message-${messageId} .cancel-button`);

		if (cancelButton) {
			cancelButton.addEventListener('click', (event: Event) => {
				event.preventDefault();

				button.removeEventListener('click', self.eventHideEditForm);
				button.addEventListener('click', self.eventShowEditForm);

				let form = document.querySelector(`#edit-message-${messageId}`) as HTMLElement;
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
		let form = document.querySelector(`#edit-message-${messageId}`) as HTMLElement;

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

		let workingDots = document.querySelector(`#working-${messageId}`);
		show(workingDots);

		let onSuccess = () => {
			let form = document.querySelector(`#edit-message-${messageId}`) as HTMLElement;
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

		let workingDots = document.querySelector(`#working-${messageId}`);
		show(workingDots);

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Messages/XhrReply/${messageId}`
		});

		await Xhr.loadView(requestOptions, document);

		hide(workingDots);

		self.app.navigation.showScriptFunctionality();
		self.app.bbCode.init();
		self.app.smileySelector.init();
		self.app.reactionSelector.init();

		let saveButton = document.querySelector(`#message-reply-${messageId} .save-button`);

		if (saveButton) {
			saveButton.addEventListener('click', self.eventSaveReplyForm);
		}

		let cancelButton = document.querySelector(`#message-reply-${messageId} .cancel-button`);

		if (cancelButton) {
			cancelButton.addEventListener('click', (event: Event) => {
				event.preventDefault();

				button.removeEventListener('click', self.eventHideReplyForm);
				button.addEventListener('click', self.eventShowReplyForm);

				let form = document.querySelector(`#message-reply-${messageId}`) as HTMLElement;
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
		let form = document.querySelector(`#message-reply-${messageId}`) as HTMLElement;

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

		let workingDots = document.querySelector(`#working-${messageId}`);
		show(workingDots);

		let onSuccess = () => {
			let form = document.querySelector(`#message-reply-${messageId}`) as HTMLElement;
			clear(form);
			hide(form);

			document.querySelectorAll('.reply-button').forEach(element => {
				element.removeEventListener('click', self.resetMessageReplyForms);
				element.removeEventListener('click', self.eventShowReplyForm);
				element.addEventListener('click', self.eventShowReplyForm);
			});

			document.querySelectorAll('.reply-form').forEach(element => {
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
		event.stopPropagation();

		let target = <HTMLElement>event.currentTarget;

		let self = this;
		self.thoughtTarget = <HTMLElement>target.closest('article');
		self.thoughtSelectorMessageId = target.getAttribute('message-id') || '';

		let smileySelector = target.querySelector('[data-component="smiley-selector"]');
		show(smileySelector);

		setTimeout(function () {
			self.body.addEventListener('click', self.eventHideThoughtSelector);
		}, 50);
	}

	eventHideThoughtSelector = (event: Event) => {
		document.querySelectorAll('[data-component="smiley-selector"]').forEach(element => {
			hide(element);
		});

		let self = this;
		self.body.removeEventListener('click', self.eventHideThoughtSelector);
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
		}
		else {
			window.location.href = `/Messages/AddThought/${this.thoughtSelectorMessageId}?smiley=${smileyId}`;
		}
	}

	eventDeleteMessage = async (event: Event): Promise<void> => {
		// Only send an XHR if we anticipate the update will be posted via the hub.
		if (this.app.hub && this.app.hub.state == HubConnectionState.Connected) {
			event.preventDefault();

			let targetButton = <HTMLAnchorElement>event.currentTarget;
			let targetMessage = <HTMLElement>targetButton.closest('article');

			targetMessage.classList.add('faded');

			let requestOptions = new XhrOptions({
				method: HttpMethod.Get,
				url: targetButton.href
			});

			Xhr.request(requestOptions);
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

		let target = <HTMLElement>event.currentTarget;
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

		let boardFlag = <HTMLInputElement>document.querySelector(`[board-flag="${boardId}"]`);

		if (!boardFlag) {
			return;
		}

		boardFlag.checked = boardFlag.checked ? false : true;

		let requestOptions = new XhrOptions({
			url: `${(<any>window).togglePath}&BoardId=${boardId}`
		});

		await Xhr.request(requestOptions);

		target.removeAttribute('toggling');
	}

	eventToggleBookmarkImage = (event: Event): void => {
		let bookmarkImageSpan = <HTMLElement>event.currentTarget;
		let bookmarkImage = bookmarkImageSpan.querySelector('img');

		if (bookmarkImage) {
			let status = this.settings.bookmarked ? 'on' : 'off';

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

		let page = Number(eventTarget.getAttribute('data-page'));
		this.loadPage(page);
	}

	eventPopState = (event: PopStateEvent) => {
		let settings = event.state as TopicDisplaySettings;

		if (settings) {
			this.settings = settings;
			this.loadPage(this.settings.currentPage, false);
		}
	}
}
