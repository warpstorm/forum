import { show, hide } from "../helpers";

export class AccountDetails {
	init(): void {
		this.bindEvents();
	}

	bindEvents = () => {
		let birthdayToggle = document.querySelector('.birthday-toggle');

		if (birthdayToggle) {
			birthdayToggle.addEventListener('click', this.toggleBirthdaySelectors);
		}
	}

	toggleBirthdaySelectors = (event: Event) => {
		let birthdayToggle = <HTMLInputElement>event.currentTarget;

		document.querySelectorAll('.birthday-selectors').forEach(element => {
			if (birthdayToggle.checked) {
				show(element);
			}
			else {
				hide(element);
			}
		});
	}
}
