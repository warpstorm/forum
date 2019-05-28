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
			allDayCheckbox.addEventListener('click', function (event: Event): void {
				self.timeEnabled = !(<HTMLInputElement>event.currentTarget).checked;
				self.initializePickers();
			});
		}
	}

	initializePickers(): void {
		let self = this;

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
