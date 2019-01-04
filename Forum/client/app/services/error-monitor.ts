export class ErrorMonitor {
	constructor(private doc: Document) {}

	init(): void {
		var observer = new MutationObserver(this.updatedError);
		var observerConfig: MutationObserverInit = {
			childList: true
		};

		this.doc.querySelectorAll(".error").forEach(element => {
			if (!element.textContent || element.textContent.trim().length == 0) {
				element.classList.add('hidden');
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
						(mutation.target as HTMLElement).classList.add('hidden');
					}
					else {
						(mutation.target as HTMLElement).classList.remove('hidden');
					}
					break;
			}
		}
	}
}
