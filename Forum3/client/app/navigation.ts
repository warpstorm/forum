import { isFirefox } from './helpers';

export default function (): void {
	// expects document to be defined at the global scope.
	let navigation = new Navigation(document);
	navigation.addListeners();
}

export class Navigation {
	constructor(private html: Document) {}

	addListeners(): void {
		this.addListenerOpenMenu();
		this.addListenerClickableLinkParent();
		this.setupPageNavigators();
	}

	addListenerOpenMenu(): void {
		this.html.querySelectorAll('.open-menu').forEach(element => {
			element.on('click', this.eventOpenMenu);
		});
	}

    addListenerUnhidePages(pageNavigatorElement: Element): void {
        pageNavigatorElement.querySelectorAll('.unhide-pages').forEach(element => {
			element.off('click', this.eventUnhidePages);
			element.on('click', this.eventUnhidePages);
        });
	}

	addListenerClickableLinkParent(): void {
		let linkParents = document.querySelectorAll('[clickable-link-parent]')

		for (var i = 0; i < linkParents.length; i++) {
			let linkParent = linkParents[i];

			linkParent.querySelector('a').off('click', this.eventPreventDefault);
			linkParent.querySelector('a').on('click', this.eventPreventDefault);

			if (isFirefox()) {
				linkParent.off('click', this.eventOpenLink);
				linkParent.on('click', this.eventOpenLink);
			}
			else {
				linkParent.off('mousedown', this.eventOpenLink);
				linkParent.on('mousedown', this.eventOpenLink);
			}
		}
	}

	setupPageNavigators(): void {
		if ((<any>this.html).currentPage === undefined || (<any>this.html).totalPages === undefined)
			return;

		this.html.querySelectorAll('.pages').forEach(pageNavigatorElement => {
			this.addListenerUnhidePages(pageNavigatorElement);
			let currentPage = (<any>this.html).currentPage;
			this.updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement, currentPage);
			this.updatePageControlsVisibility(pageNavigatorElement, currentPage);
		});
	}

	updatePageControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let pageElements = pageNavigatorElement.querySelectorAll(".page");

		for (let i = currentPage - 2; i < currentPage; i++) {
			if (i < 0)
				continue;

			pageElements[i - 1].show();
		}

		for (let i = currentPage; i <= currentPage + 2; i++) {
			if (i - 1 > pageElements.length)
				continue;

			pageElements[i - 1].show();
		}
	}

	updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let totalPages = (<any>this.html).totalPages;

		if (currentPage - 2 > 1) {
			pageNavigatorElement.querySelectorAll('.more-pages-before').forEach(element => {
				element.show();
			});
		}

		if (currentPage + 2 < totalPages) {
			pageNavigatorElement.querySelectorAll('.more-pages-after').forEach(element => {
				element.hide();
			});
		}
	}

	eventUnhidePages = (event: Event) => {
		let target = <Element>event.currentTarget;

		target.parentElement.querySelectorAll('.page').forEach(element => {
			element.show();
		});

		target.parentElement.querySelector('.more-pages-before').hide();
		target.parentElement.querySelector('.more-pages-after').hide();
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

		targetElement.off('click', this.eventOpenMenu);
		targetElement.on('click', this.eventCloseMenu);

		targetElement.querySelectorAll('.menu-wrapper').forEach(element => {
			element.show();
		});

		let body = this.html.getElementsByTagName('body')[0];

		setTimeout(() => {
			body.on('click', this.eventCloseMenu);
		}, 50);
	}

	eventCloseMenu = (event: Event) => {
		var dropDownMenuElements = this.html.querySelectorAll('.menu-wrapper');

		for (var i = 0; i < dropDownMenuElements.length; i++) {
			dropDownMenuElements[i].hide();
		}

		this.html.querySelectorAll('.open-menu').forEach(element => {
			element.off('click', this.eventCloseMenu);

			element.off('click', this.eventOpenMenu);
			element.on('click', this.eventOpenMenu);
		});

		this.html.getElementsByTagName('body')[0].off('click', this.eventCloseMenu);
	}

	eventPreventDefault = (event: Event) => {
		event.preventDefault();
	}

	eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}