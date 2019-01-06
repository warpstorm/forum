import { show, hide } from "../helpers";

export class AccountDetails {

	constructor(private doc: Document) {
	}

	init(): void {
		this.bindEvents();
	}

	bindEvents = () => {
		let birthdayToggle = this.doc.querySelector('.birthday-toggle');

		if (birthdayToggle) {
			birthdayToggle.addEventListener('click', this.toggleBirthdaySelectors);
		}
	}

	toggleBirthdaySelectors = (event: Event) => {
		let birthdayToggle = <HTMLInputElement>event.currentTarget;

		this.doc.querySelectorAll('.birthday-selectors').forEach(element => {
			if (birthdayToggle.checked) {
				show(element);
			}
			else {
				hide(element);
			}
		});
	}
}
