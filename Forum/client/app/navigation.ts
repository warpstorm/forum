import { isFirefox, hide, show } from './helpers';

export class Navigation {
	constructor(private doc: Document) {}

	addListeners(): void {
		this.addListenerOpenMenu();
		this.addListenerClickableLinkParent();
		this.setupPageNavigators();
	}

	addListenerOpenMenu(): void {
		this.doc.querySelectorAll('.open-menu').forEach(element => {
			element.addEventListener('click', this.eventOpenMenu);
		});
	}

    addListenerUnhidePages(pageNavigatorElement: Element): void {
        pageNavigatorElement.querySelectorAll('.unhide-pages').forEach(element => {
			element.removeEventListener('click', this.eventUnhidePages);
			element.addEventListener('click', this.eventUnhidePages);
        });
	}

	addListenerClickableLinkParent(): void {
		let linkParents = document.querySelectorAll('[clickable-link-parent]')

		for (var i = 0; i < linkParents.length; i++) {
			let linkParent = linkParents[i];
			let link = linkParent.querySelector('a');

			if (link) {
				link.removeEventListener('click', this.eventPreventDefault);
				link.addEventListener('click', this.eventPreventDefault);
			}

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
		if ((<any>this.doc).currentPage === undefined || (<any>this.doc).totalPages === undefined)
			return;

		this.doc.querySelectorAll('.pages').forEach(pageNavigatorElement => {
			this.addListenerUnhidePages(pageNavigatorElement);
			let currentPage = (<any>this.doc).currentPage;
			this.updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement, currentPage);
			this.updatePageControlsVisibility(pageNavigatorElement, currentPage);
		});
	}

	updatePageControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let pageElements = pageNavigatorElement.querySelectorAll(".page");

		for (let i = currentPage - 2; i < currentPage; i++) {
			if (i < 0)
				continue;

			show(pageElements[i - 1]);
		}

		for (let i = currentPage; i <= currentPage + 2; i++) {
			if (i - 1 > pageElements.length)
				continue;

			show(pageElements[i - 1]);
		}
	}

	updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let totalPages = (<any>this.doc).totalPages;

		if (currentPage - 2 > 1) {
			pageNavigatorElement.querySelectorAll('.more-pages-before').forEach(element => {
				show(element);
			});
		}

		if (currentPage + 2 < totalPages) {
			pageNavigatorElement.querySelectorAll('.more-pages-after').forEach(element => {
				hide(element);
			});
		}
	}

	eventUnhidePages = (event: Event) => {
		let target = <Element>event.currentTarget;

		if (!target.parentElement) {
			return;
		}

		target.parentElement.querySelectorAll('.page').forEach(element => {
			show(element);
		});

		hide(target.parentElement.querySelector('.more-pages-before'));
		hide(target.parentElement.querySelector('.more-pages-after'));
	}

	eventOpenLink = (event: Event) => {
		this.eventStopPropagation(event);

		let url;
		let targetElement = <HTMLElement>event.currentTarget;

		if (targetElement.tagName == 'a') {
			url = targetElement.getAttribute('href');
		}
		else {
			url = (<Element>(<Element>targetElement.closest('[clickable-link-parent]')).querySelector('a')).getAttribute('href');
		}

		if (url) {
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
		}

		return true;
	}

	eventOpenMenu = (event: Event) => {
		this.eventCloseMenu(event);

		let targetElement = <HTMLElement>event.currentTarget;

		targetElement.removeEventListener('click', this.eventOpenMenu);
		targetElement.addEventListener('click', this.eventCloseMenu);

		targetElement.querySelectorAll('.menu-wrapper').forEach(element => {
			show(element);
		});

		let body: Element = this.doc.getElementsByTagName('body')[0];

		setTimeout(() => {
			body.addEventListener('click', this.eventCloseMenu);
		}, 50);
	}

	eventCloseMenu = (event: Event) => {
		var dropDownMenuElements = this.doc.querySelectorAll('.menu-wrapper');

		for (var i = 0; i < dropDownMenuElements.length; i++) {
			hide(dropDownMenuElements[i]);
		}

		this.doc.querySelectorAll('.open-menu').forEach(element => {
			element.removeEventListener('click', this.eventCloseMenu);

			element.removeEventListener('click', this.eventOpenMenu);
			element.addEventListener('click', this.eventOpenMenu);
		});

		this.doc.getElementsByTagName('body')[0].removeEventListener('click', this.eventCloseMenu);
	}

	private eventPreventDefault = (event: Event) => {
		event.preventDefault();
	}

	private eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}