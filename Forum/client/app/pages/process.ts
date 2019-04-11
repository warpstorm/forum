import { ProcessSettings } from "../models/page-settings/process-settings";
import { XhrOptions } from "../models/xhr-options";
import { HttpMethod } from "../definitions/http-method";
import { Xhr } from "../services/xhr";
import { show, queryify } from "../helpers";
import { XhrResult } from "../models/xhr-result";
import { XhrException } from "../models/xhr-exception";
import { ProcessStage } from "../models/page-settings/process-stage";

function getSettings(): ProcessSettings {
	let genericWindow = <any>window;

	return new ProcessSettings({
		stages: genericWindow.stages
	});
}

export class Process {
	private settings: ProcessSettings;

	constructor() {
		this.settings = getSettings();
	}

	init() {
		let startButton = <Element>document.querySelector('#start-button');
		startButton.addEventListener('click', this.eventStartButtonClick);

		let takeInput = <HTMLInputElement>document.querySelector('#take');
		takeInput.addEventListener('blur', this.eventUpdateTake);

		let stageInput = <HTMLInputElement>document.querySelector('#current-stage');
		stageInput.addEventListener('blur', this.eventUpdateStage);

		let totalStages = <Element>document.querySelector('#total-stages');
		totalStages.innerHTML = this.settings.stages.length.toString();
	}

	updateProgress(): void {
		let progressBar = <HTMLButtonElement>document.querySelector('#progress-bar');
		show(progressBar);

		let bar = <HTMLElement>document.querySelector('#completed-bar');
		let percent = 100 * (this.settings.currentPage + 1) / (this.settings.totalPages + 1);
		bar.style.width = `${percent}%`;

		percent = Math.round(percent);
		bar.innerHTML = `${percent}%`;
	}

	log(xhrResult: XhrResult): void {
		let logSuccessInput = <HTMLInputElement>document.querySelector('#log-success');

		if (xhrResult.status == 200 && !logSuccessInput.checked) {
			return;
		}

		let log = <Element>document.querySelector('#log');

		if (xhrResult) {
			let responseStatus = `${xhrResult.status} ${xhrResult.statusText}`;
			let responseException: XhrException | null = null;

			if (xhrResult.responseText) {
				try {
					responseException = new XhrException(JSON.parse(xhrResult.responseText));
				}
				catch { }
			}

			if (responseException) {
				let responseDescription = `${responseException.ClassName}: ${responseException.Message}\n${responseException.StackTraceString}`;
				this.addLogItem(log, responseStatus, responseDescription);
			}
			else {
				this.addLogItem(log, responseStatus, xhrResult.responseText);
			}
		}
		else {
			this.addLogItem(log, 'Result was null');
		}

		let logItem = log.firstChild;

		if (logItem) {
			logItem.addEventListener('click', (event: Event): void => {
				let eventTarget = <Element>event.currentTarget;
				show(eventTarget.querySelector('pre'));
			});
		}
	}

	addLogItem(logElement: Element, status: string = '', statusDescription: string = ''): void {
		let date = new Date();
		let hours = date.getHours() < 10 ? `0${date.getHours()}` : date.getHours();
		let mins = date.getMinutes() < 10 ? `0${date.getMinutes()}` : date.getMinutes();
		let secs = date.getSeconds() < 10 ? `0${date.getSeconds()}` : date.getSeconds();

		let time = `${hours}:${mins}:${secs}`;

		let logItemHtml = '<li class="small-pad-bottom font-small">';
		logItemHtml += `<p>${time} - ${status}</p>`;

		if (statusDescription) {
			logItemHtml += `<pre class="hidden">${statusDescription}</pre>`
		}

		logItemHtml += '</li>';

		logElement.innerHTML = logItemHtml + logElement.innerHTML;
	}

	async loadCurrentAction(): Promise<void> {
		let requestOptions = new XhrOptions({
			method: HttpMethod.Post,
			url: this.settings.currentAction
		});

		let xhrResult = await Xhr.request(requestOptions);

		if (xhrResult.status != 200) {
			this.log(xhrResult);
		}

		let parsedResult = JSON.parse(xhrResult.responseText) as ProcessStage;

		if (parsedResult) {
			this.settings.totalPages = parsedResult.totalPages;
			this.settings.totalRecords = parsedResult.totalRecords;

			let actionName = <Element>document.querySelector('#action-name');
			let actionNote = <Element>document.querySelector('#action-note');

			actionName.innerHTML = parsedResult.actionName;
			actionNote.innerHTML = parsedResult.actionNote;

			let takeInput = <HTMLInputElement>document.querySelector('#take');
			let totalPages = <Element>document.querySelector('#total-pages');
			let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');

			if (takeInput.value) {
				this.settings.take = parseInt(takeInput.value);
			}

			if (this.settings.take == 0) {
				this.settings.take = parsedResult.take;
				takeInput.value = this.settings.take.toString();
			}

			totalPages.innerHTML = (this.settings.totalPages + 1).toString();

			if (currentPageInput.value) {
				this.settings.currentPage = parseInt(currentPageInput.value) - 1;
			}

			if (this.settings.currentPage == 0) {
				currentPageInput.value = "1";
			}
		}
	}

	eventUpdateTake = (event: Event): void => {
		let takeInput = <HTMLInputElement>event.currentTarget;
		let pageInput = <HTMLInputElement>document.querySelector('#current-page');
		let totalPages = <Element>document.querySelector('#total-pages');

		if (!takeInput.value) {
			return;
		}

		let skip = this.settings.take * this.settings.currentPage;

		this.settings.take = parseInt(takeInput.value);
		this.settings.currentPage = skip > 0 ? Math.floor(skip / this.settings.take) : 0;
		this.settings.totalPages = Math.ceil(this.settings.totalRecords / this.settings.take);

		pageInput.value = (this.settings.currentPage + 1).toString();
		totalPages.innerHTML = (this.settings.totalPages + 1).toString();
	}

	eventUpdateStage = (event: Event): void => {
		let stageInput = <HTMLInputElement>event.currentTarget;
		let pageInput = <HTMLInputElement>document.querySelector('#current-page');
		let takeInput = <HTMLInputElement>document.querySelector('#take');
		let totalPages = <Element>document.querySelector('#total-pages');

		if (!stageInput.value) {
			return;
		}

		this.settings.currentStage = parseInt(stageInput.value) - 1;
		this.settings.take = 0;
		this.settings.currentPage = 0;
		this.settings.totalPages = 0;

		takeInput.value = "";
		pageInput.value = "";
		totalPages.innerHTML = "";
	}

	eventStartButtonClick = async (event: Event): Promise<void> => {
		let togglePauseOnError = <HTMLInputElement>document.querySelector('#pause-on-error');
		let togglePauseAfterNext = <HTMLInputElement>document.querySelector('#pause-after-next');
		let currentStageInput = <HTMLInputElement>document.querySelector('#current-stage');

		if (currentStageInput.value) {
			this.settings.currentStage = parseInt(currentStageInput.value) - 1;
		}

		for (var i = this.settings.currentStage; i < this.settings.stages.length; i++) {
			this.start(i);

			this.settings.currentAction = this.settings.stages[i];

			if (this.settings.totalPages == 0) {
				await this.loadCurrentAction();
			}

			while (this.settings.currentPage <= this.settings.totalPages) {
				let requestOptions = new XhrOptions({
					method: HttpMethod.Post,
					url: this.settings.currentAction,
					body: queryify({
						currentPage: this.settings.currentPage,
						take: this.settings.take,
						lastRecordId: this.settings.lastRecordId
					}),
					timeout: 120000
				});

				let xhrResult = await Xhr.request(requestOptions);

				this.log(xhrResult);

				if (xhrResult.status != 200 && togglePauseOnError.checked) {
					this.stop();
					return;
				}

				if (xhrResult.responseText) {
					this.settings.lastRecordId = parseInt(xhrResult.responseText);

					if (!this.settings.lastRecordId) {
						this.settings.lastRecordId = -1;
					}
				}

				this.updateProgress();
				this.settings.currentPage++;

				let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');
				currentPageInput.value = this.settings.currentPage.toString();

				if (togglePauseAfterNext.checked) {
					togglePauseAfterNext.checked = false;
					this.stop();
					return;
				}
			}

			this.stop();
		}
	}

	start(stage: number) {
		let startButton = <HTMLInputElement>document.querySelector('#start-button');
		let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');
		let currentStageInput = <HTMLInputElement>document.querySelector('#current-stage');
		let takeInput = <HTMLInputElement>document.querySelector('#take');

		if (takeInput.value) {
			this.settings.take = parseInt(takeInput.value);
		}

		if (currentPageInput.value) {
			this.settings.currentPage = parseInt(currentPageInput.value) - 1;
		}

		currentStageInput.value = (stage + 1).toString();

		startButton.disabled = true;
		currentPageInput.disabled = true;
		currentStageInput.disabled = true;
		takeInput.disabled = true;
	}

	stop() {
		let startButton = <HTMLInputElement>document.querySelector('#start-button');
		let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');
		let currentStageInput = <HTMLInputElement>document.querySelector('#current-stage');
		let totalPages = <Element>document.querySelector('#total-pages');
		let takeInput = <HTMLInputElement>document.querySelector('#take');

		if (this.settings.currentPage > this.settings.totalPages) {
			this.settings.lastRecordId = 0;
			this.settings.currentPage = 0;
			this.settings.totalPages = 0;
			this.settings.take = 0;

			currentPageInput.value = "";
			totalPages.innerHTML = "";
			takeInput.value = "";
		}

		startButton.disabled = false;
		currentPageInput.disabled = false;
		currentStageInput.disabled = false;
		takeInput.disabled = false;
	}
}