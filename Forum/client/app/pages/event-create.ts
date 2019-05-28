import flatpickr from "flatpickr";

export class EventCreate {
	timeEnabled: boolean;

	constructor() {
		this.timeEnabled = true;
	}

	init() {
		let self = this;
		self.initializePickers();

		let allDayCheckbox = document.querySelector('#AllDay');

		if (allDayCheckbox) {
			allDayCheckbox.addEventListener('click', function (): void {
				self.initializePickers();
			});
		}
	}

	initializePickers(): void {
		let self = this;

		let allDayCheckbox = document.querySelector('#AllDay') as HTMLInputElement;
		self.timeEnabled = !allDayCheckbox.checked;

		flatpickr('#startPicker', {
			allowInput: true,
			clickOpens: false,
			enableTime: self.timeEnabled,
			dateFormat: self.timeEnabled ? 'Y-m-d H:i' : 'Y-m-d',
			wrap: true
		});

		flatpickr('#endPicker', {
			allowInput: true,
			clickOpens: false,
			enableTime: self.timeEnabled,
			dateFormat: self.timeEnabled ? 'Y-m-d H:i' : 'Y-m-d',
			wrap: true
		});
	}
}
