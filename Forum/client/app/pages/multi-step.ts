import { MultiStepSettings } from "../models/page-settings/multi-step-settings";
import { XhrOptions } from "../models/xhr-options";
import { HttpMethod } from "../definitions/http-method";
import { Xhr } from "../services/xhr";
import { show, queryify } from "../helpers";
import { XhrResult } from "../models/xhr-result";
import { XhrException } from "../models/xhr-exception";
import { Step } from "../models/page-settings/step";

function getSettings(): MultiStepSettings {
	let genericWindow = <any>window;

	return new MultiStepSettings({
		steps: genericWindow.steps
	});
}

export class MultiStep {
	private settings: MultiStepSettings;

	constructor() {
		this.settings = getSettings();
	}

	init() {
		let startButton = <Element>document.querySelector('#start-button');
		startButton.addEventListener('click', this.eventStartButtonClick);

		let takeInput = <HTMLInputElement>document.querySelector('#take');
		takeInput.addEventListener('blur', this.eventUpdateTake);

		let totalSteps = <Element>document.querySelector('#total-steps');
		totalSteps.innerHTML = this.settings.steps.length.toString();

		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		currentStepInput.value = "1";
	}

	updateProgress(): void {
		let progressBar = <HTMLButtonElement>document.querySelector('#progress-bar');
		show(progressBar);

		let bar = <HTMLElement>document.querySelector('#completed-bar');
		let percent = 100 * this.settings.currentPage / this.settings.totalPages;
		bar.style.width = `${percent}%`;

		percent = Math.round(percent);
		bar.innerHTML = `${percent}%`;
	}

	log(xhrResult: XhrResult): void {
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

		let parsedResult = JSON.parse(xhrResult.responseText) as Step;

		if (parsedResult) {
			this.settings.take = parsedResult.take;
			this.settings.totalPages = parsedResult.totalPages;
			this.settings.totalRecords = parsedResult.totalRecords;
			this.settings.actionName = parsedResult.actionName;
			this.settings.actionNote = parsedResult.actionNote;
			this.settings.currentPage = 0;

			let takeInput = <HTMLInputElement>document.querySelector('#take');
			takeInput.value = this.settings.take.toString();

			let totalPages = <Element>document.querySelector('#total-pages');
			totalPages.innerHTML = (this.settings.totalPages + 1).toString();

			let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');
			currentPageInput.value = "1";
		}
	}

	eventUpdateTake = (event: Event): void => {
		let takeInput = <HTMLInputElement>event.currentTarget;
		let pageInput = <HTMLInputElement>document.querySelector('#current-page');
		let totalPages = <Element>document.querySelector('#total-pages');

		let skip = this.settings.take * this.settings.currentPage;

		this.settings.take = parseInt(takeInput.value);
		this.settings.currentPage = skip > 0 ? Math.floor(skip / this.settings.take) : 0;
		this.settings.totalPages = Math.ceil(this.settings.totalRecords / this.settings.take);

		pageInput.value = (this.settings.currentPage + 1).toString();
		totalPages.innerHTML = (this.settings.totalPages + 1).toString();
	}

	eventStartButtonClick = async (event: Event): Promise<void> => {
		let startButton = <HTMLButtonElement>event.currentTarget;
		let currentPageInput = <HTMLInputElement>document.querySelector('#current-page');
		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		let takeInput = <HTMLInputElement>document.querySelector('#take');
		let togglePauseOnError = <HTMLInputElement>document.querySelector('#pause-on-error');
		let togglePauseAfterNext = <HTMLInputElement>document.querySelector('#pause-after-next');

		startButton.disabled = true;
		currentPageInput.disabled = true;
		currentStepInput.disabled = true;
		takeInput.disabled = true;

		this.settings.take = parseInt(takeInput.value);
		this.settings.currentPage = parseInt(currentPageInput.value) - 1;
		this.settings.currentStep = parseInt(currentStepInput.value) - 1;

		for (var i = this.settings.currentStep; i < this.settings.steps.length; i++) {
			currentStepInput.value = (i + 1).toString();

			this.settings.currentAction = this.settings.steps[i];

			if (this.settings.totalPages == 0) {
				await this.loadCurrentAction();
			}

			while (this.settings.currentPage <= this.settings.totalPages) {
				let requestOptions = new XhrOptions({
					method: HttpMethod.Post,
					url: this.settings.currentAction,
					body: queryify({
						currentPage: this.settings.currentPage,
						take: takeInput.value
					})
				});

				let xhrResult = await Xhr.request(requestOptions);

				this.log(xhrResult);

				this.updateProgress();
				this.settings.currentPage++;
				currentPageInput.value = this.settings.currentPage.toString();

				if (xhrResult.status != 200 && togglePauseOnError.checked) {
					break;
				}

				if (togglePauseAfterNext.checked) {
					togglePauseAfterNext.checked = false;
					break;
				}
			}

			if (this.settings.currentPage > this.settings.totalPages) {
				this.settings.totalPages = 0;
			}
		}

		startButton.disabled = false;
		currentPageInput.disabled = false;
		currentStepInput.disabled = false;
	}
}