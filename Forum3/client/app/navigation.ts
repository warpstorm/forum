import { isFirefox } from './helpers';

export default function (): void {
	// expects document to be defined at the global scope.
	let navigation = new Navigation(document);
	navigation.addListeners();
}

export class Navigation {
	constructor(private htmlDocument: Document) {}

	addListeners(): void {
		this.addListenerOpenMenu();
		this.addListenerClickableLinkParent();
		this.setupPageNavigators();
	}

	addListenerOpenMenu(): void {
		this.htmlDocument.querySelectorAll('.open-menu').forEach(element => {
			element.addEventListener('click', this.eventOpenMenu);
		});
	}

    addListenerUnhidePages(pageNavigatorElement: Element): void {
        pageNavigatorElement.querySelectorAll('.unhide-pages').forEach(element => {
			element.addEventListener('click', function (event: Event) {
				let target = <Element>event.currentTarget;

				target.parentElement.querySelectorAll('.page').forEach(element => {
                    element.classList.remove('hidden');
				});

				target.parentElement.querySelector('.more-pages-before').classList.add('hidden');
				target.parentElement.querySelector('.more-pages-after').classList.add('hidden');
            });
        });
	}

	addListenerClickableLinkParent(): void {
		let linkParents = document.querySelectorAll('[clickable-link-parent]')

		for (var i = 0; i < linkParents.length; i++) {
			let linkParent = linkParents[i];

			linkParent.querySelector('a').addEventListener('click', this.eventPreventDefault);

			if (isFirefox()) {
				linkParent.removeEventListener('click', this.eventOpenLink);
				linkParent.addEventListener('click', this.eventOpenLink);
			}
			else {
				linkParent.removeEventListener('mousedown', this.eventOpenLink);
				linkParent.addEventListener('mousedown', this.eventOpenLink);
			}
		}
	}

	setupPageNavigators(): void {
		if ((<any>this.htmlDocument).currentPage === undefined || (<any>this.htmlDocument).totalPages === undefined)
			return;

		this.htmlDocument.querySelectorAll('.pages').forEach(pageNavigatorElement => {
			this.addListenerUnhidePages(pageNavigatorElement);
			let currentPage = (<any>this.htmlDocument).currentPage;
			this.updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement, currentPage);
			this.updatePageControlsVisibility(pageNavigatorElement, currentPage);
		});
	}

	updatePageControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let pageElements = pageNavigatorElement.querySelectorAll(".page");

		for (let i = currentPage - 2; i < currentPage; i++) {
			if (i < 0)
				continue;

			pageElements[i - 1].classList.remove('hidden');
		}

		for (let i = currentPage; i <= currentPage + 2; i++) {
			if (i - 1 > pageElements.length)
				continue;

			pageElements[i - 1].classList.remove('hidden');
		}
	}

	updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let totalPages = (<any>this.htmlDocument).totalPages;

		if (currentPage - 2 > 1) {
			pageNavigatorElement.querySelectorAll('.more-pages-before').forEach(element => {
				element.classList.remove('hidden');
			});
		}

		if (currentPage + 2 < totalPages) {
			pageNavigatorElement.querySelectorAll('.more-pages-after').forEach(element => {
				element.classList.add('hidden');
			});
		}
	}

	eventOpenLink = (event: Event) => {
		this.eventStopPropagation(event);

		let url;
		let targetElement = <HTMLElement>event.currentTarget;

		if (targetElement.tagName == 'a') {
			url = targetElement.getAttribute('href');
		}
		else {
			url = targetElement.closest('[clickable-link-parent]').querySelector('a').getAttribute('href');
		}

		switch ((<KeyboardEvent>event).which) {
			case 1:
				if ((<KeyboardEvent>event).shiftKey) {
					window.open(url, '_blank');
				}
				else {
					window.location.href = url;
				}
				break;

			case 2:
				window.open(url, '_blank');
				break;
		}

		return true;
	}

	eventOpenMenu = (event: Event) => {
		this.eventCloseMenu(event);

		let targetElement = <HTMLElement>event.currentTarget;

		targetElement.removeEventListener('click', this.eventOpenMenu);
		targetElement.addEventListener('click', this.eventCloseMenu);

		targetElement.querySelectorAll('.menu-wrapper').forEach(element => {
			element.classList.remove("hidden");
		});

		let body = this.htmlDocument.getElementsByTagName('body')[0];

		setTimeout(() => {
			body.addEventListener('click', this.eventCloseMenu);
		}, 50);
	}

	eventCloseMenu = (event: Event) => {
		var dropDownMenuElements = this.htmlDocument.querySelectorAll('.menu-wrapper');

		for (var i = 0; i < dropDownMenuElements.length; i++) {
			var dropDownMenuElement = dropDownMenuElements[i];

			if (!dropDownMenuElement.classList.contains('hidden'))
				dropDownMenuElement.classList.add('hidden');
		}

		this.htmlDocument.querySelectorAll('.open-menu').forEach(element => {
			element.removeEventListener('click', this.eventCloseMenu);
			element.removeEventListener('click', this.eventOpenMenu);
			element.addEventListener('click', this.eventOpenMenu);
		});

		this.htmlDocument.getElementsByTagName('body')[0].removeEventListener('click', this.eventCloseMenu);
	}

	eventPreventDefault = (event: Event) => {
		event.preventDefault();
	}

	eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}