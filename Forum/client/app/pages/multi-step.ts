import { MultiStepSettings } from "../models/page-settings/multi-step-settings";
import { XhrOptions } from "../models/xhr-options";
import { HttpMethod } from "../definitions/http-method";
import { Xhr } from "../services/xhr";
import { hide, show, queryify } from "../helpers";
import { XhrResult } from "../models/xhr-result";
import { XhrException } from "../models/xhr-exception";

function getSettings(): MultiStepSettings {
	let genericWindow = <any>window;

	return new MultiStepSettings({
		page: genericWindow.page,
		totalPages: genericWindow.totalPages,
		take: genericWindow.take,
		nextAction: genericWindow.nextAction
	});
}

export class MultiStep {
	private settings: MultiStepSettings;

	constructor() {
		this.settings = getSettings();
	}

	init() {
		this.updateStatus();

		let startButton = <Element>document.querySelector('button#start');
		startButton.addEventListener('click', this.eventStartButtonClick);
	}

	updateStatus(): void {
		let bar = <HTMLElement>document.querySelector('.completed-bar');
		let percent = 100 * this.settings.page / this.settings.totalPages;
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

	eventStartButtonClick = async (event: Event): Promise<void> => {
		let target = <HTMLButtonElement>event.currentTarget;
		target.disabled = true;

		let startButton = <HTMLButtonElement>document.querySelector('button#start');
		let togglePauseOnError = <HTMLInputElement>document.querySelector('#pause-on-error');
		let togglePauseAfterNext = <HTMLInputElement>document.querySelector('#pause-after-next');
		let takeInput = <HTMLInputElement>document.querySelector('#take');
		let pageInput = <HTMLInputElement>document.querySelector('#current-page');
		pageInput.disabled = true;

		this.settings.page = parseInt(pageInput.value) - 1;

		while (this.settings.page <= this.settings.totalPages) {
			let requestOptions = new XhrOptions({
				method: HttpMethod.Post,
				url: this.settings.nextAction,
				body: queryify({
					page: this.settings.page,
					totalpages: this.settings.totalPages,
					take: takeInput.value
				})
			});

			let xhrResult = await Xhr.request(requestOptions);

			this.log(xhrResult);

			this.updateStatus();
			this.settings.page++;
			pageInput.value = this.settings.page.toString();

			if (xhrResult.status != 200 && togglePauseOnError.checked) {
				break;
			}

			if (togglePauseAfterNext.checked) {
				togglePauseAfterNext.checked = false;
				break;
			}
		}

		startButton.disabled = false;
		pageInput.disabled = false;
	}
}