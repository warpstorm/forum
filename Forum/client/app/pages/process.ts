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
		let percent = 100 * (this.settings.currentStep + 1) / (this.settings.totalSteps + 1);
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
			this.settings.totalSteps = parsedResult.totalSteps;
			this.settings.totalRecords = parsedResult.totalRecords;

			let actionName = <Element>document.querySelector('#action-name');
			let actionNote = <Element>document.querySelector('#action-note');

			actionName.innerHTML = parsedResult.actionName;
			actionNote.innerHTML = parsedResult.actionNote;

			let takeInput = <HTMLInputElement>document.querySelector('#take');
			let totalSteps = <Element>document.querySelector('#total-steps');
			let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');

			if (takeInput.value) {
				this.settings.take = parseInt(takeInput.value);
			}

			if (this.settings.take == 0) {
				this.settings.take = parsedResult.take;
				takeInput.value = this.settings.take.toString();
			}

			totalSteps.innerHTML = (this.settings.totalSteps + 1).toString();

			if (currentStepInput.value) {
				this.settings.currentStep = parseInt(currentStepInput.value) - 1;
			}

			if (this.settings.currentStep == 0) {
				currentStepInput.value = "1";
			}
		}
	}

	eventUpdateTake = (event: Event): void => {
		let takeInput = <HTMLInputElement>event.currentTarget;
		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		let totalSteps = <Element>document.querySelector('#total-steps');

		if (!takeInput.value) {
			return;
		}

		let skip = this.settings.take * this.settings.currentStep;

		this.settings.take = parseInt(takeInput.value);
		this.settings.currentStep = skip > 0 ? Math.floor(skip / this.settings.take) : 0;
		this.settings.totalSteps = Math.ceil(this.settings.totalRecords / this.settings.take);

		currentStepInput.value = (this.settings.currentStep + 1).toString();
		totalSteps.innerHTML = (this.settings.totalSteps + 1).toString();
	}

	eventUpdateStage = (event: Event): void => {
		let stageInput = <HTMLInputElement>event.currentTarget;
		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		let takeInput = <HTMLInputElement>document.querySelector('#take');
		let totalSteps = <Element>document.querySelector('#total-steps');

		if (!stageInput.value) {
			return;
		}

		this.settings.currentStage = parseInt(stageInput.value) - 1;
		this.settings.take = 0;
		this.settings.currentStep = 0;
		this.settings.totalSteps = 0;

		takeInput.value = "";
		currentStepInput.value = "";
		totalSteps.innerHTML = "";
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

			if (this.settings.totalSteps == 0) {
				await this.loadCurrentAction();
			}

			while (this.settings.currentStep <= this.settings.totalSteps) {
				let requestOptions = new XhrOptions({
					method: HttpMethod.Post,
					url: this.settings.currentAction,
					body: queryify({
						currentStep: this.settings.currentStep,
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
				this.settings.currentStep++;

				let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
				currentStepInput.value = this.settings.currentStep.toString();

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
		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		let currentStageInput = <HTMLInputElement>document.querySelector('#current-stage');
		let takeInput = <HTMLInputElement>document.querySelector('#take');

		if (takeInput.value) {
			this.settings.take = parseInt(takeInput.value);
		}

		if (currentStepInput.value) {
			this.settings.currentStep = parseInt(currentStepInput.value) - 1;
		}

		currentStageInput.value = (stage + 1).toString();

		startButton.disabled = true;
		currentStepInput.disabled = true;
		currentStageInput.disabled = true;
		takeInput.disabled = true;
	}

	stop() {
		let startButton = <HTMLInputElement>document.querySelector('#start-button');
		let currentStepInput = <HTMLInputElement>document.querySelector('#current-step');
		let currentStageInput = <HTMLInputElement>document.querySelector('#current-stage');
		let totalSteps = <Element>document.querySelector('#total-steps');
		let takeInput = <HTMLInputElement>document.querySelector('#take');

		if (this.settings.currentStep > this.settings.totalSteps) {
			this.settings.lastRecordId = 0;
			this.settings.currentStep = 0;
			this.settings.totalSteps = 0;
			this.settings.take = 0;

			currentStepInput.value = "";
			totalSteps.innerHTML = "";
			takeInput.value = "";
		}

		startButton.disabled = false;
		currentStepInput.disabled = false;
		currentStageInput.disabled = false;
		takeInput.disabled = false;
	}
}