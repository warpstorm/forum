import { show, hide } from "../helpers";

export class ErrorMonitor {
	constructor(private doc: Document) {}

	init(): void {
		var observer = new MutationObserver(this.updatedError);
		var observerConfig: MutationObserverInit = {
			childList: true
		};

		this.doc.querySelectorAll(".error").forEach(element => {
			if (!element.textContent || element.textContent.trim().length == 0) {
				hide(element);
			}

			element.classList.add('error-stylish');

			observer.observe(element, observerConfig);
		});
	}

	updatedError: MutationCallback = (mutationsList) => {
		for (var mutation of mutationsList) {
			switch (mutation.type) {
				case 'childList':
					if (mutation.target.childNodes.length == 0) {
						hide(mutation.target);
					}
					else {
						show(mutation.target);
					}
					break;
			}
		}
	}
}
