import { isFirefox, hide, show, insertAfter } from '../helpers';

export class Navigation {
	private win: Window;

	constructor(private doc: Document) {
		this.win = <Window>doc.defaultView;
	}

	init(): void {
		this.addListenerOpenMenu();
		this.addListenerClickableLinkParent();
        this.setupPageNavigators();
		this.showScriptFunctionality();
	}

    showScriptFunctionality(): void {
        document.querySelectorAll('.requires-javascript').forEach(element => {
            element.classList.remove('hidden');
		});

		document.querySelectorAll('input[type="checkbox"]').forEach(element => {
			if (!element.classList.contains('scripted')) {
				element.classList.add('scripted');

				let label = document.createElement('label');
				label.htmlFor = element.id;
				insertAfter(label, element);

				label.addEventListener('click', this.eventToggleCheckBox);
			}
		});
    }

	addListenerOpenMenu(): void {
		this.doc.querySelectorAll('.open-menu').forEach(element => {
			element.classList.remove('open-menu-hover');
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
			linkParent.classList.add('pointer');

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
		if (!(<any>this.win).currentPage || !(<any>this.win).totalPages) {
			return;
		}

		this.doc.querySelectorAll('.pages').forEach(element => {
			this.addListenerUnhidePages(element);
			let currentPage = (<any>this.win).currentPage;
			this.updateMorePageBeforeAfterControlsVisibility(element, currentPage);
			this.updatePageControlsVisibility(element, currentPage);
		});
	}

	updatePageControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let pageElements = pageNavigatorElement.querySelectorAll(".page");

		for (let i = currentPage - 2; i < currentPage; i++) {
			if (i < 0) {
				continue;
			}

			show(pageElements[i - 1]);
		}

		for (let i = currentPage; i <= currentPage + 2; i++) {
			if (i - 1 > pageElements.length) {
				continue;
			}

			show(pageElements[i - 1]);
		}
	}

	updateMorePageBeforeAfterControlsVisibility(pageNavigatorElement: Element, currentPage: number): void {
		let totalPages = (<any>this.win).totalPages;

		if (currentPage - 2 > 1) {
			pageNavigatorElement.querySelectorAll('.more-pages-before').forEach(element => {
				show(element);
			});
		}

		if (currentPage + 2 < totalPages) {
			pageNavigatorElement.querySelectorAll('.more-pages-after').forEach(element => {
				show(element);
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
					if ((<KeyboardEvent>event).shiftKey || (<KeyboardEvent>event).ctrlKey) {
						this.win.open(url, '_blank');
					}
					else {
						this.win.location.href = url;
					}
					break;

				case 2:
					this.win.open(url, '_blank');
					break;
			}
		}

		return true;
	}

	eventOpenMenu = (event: Event) => {
		event.stopPropagation();

		let self = this;

		this.eventCloseMenu(event);

		let targetElement = <HTMLElement>event.currentTarget;

		targetElement.removeEventListener('click', self.eventOpenMenu);
		targetElement.addEventListener('click', self.eventCloseMenu);
		
		targetElement.querySelectorAll('.menu-wrapper').forEach(menuWrapperElement => {
			show(menuWrapperElement);

			let dropDownMenuElement = menuWrapperElement.querySelector('.drop-down-menu') as HTMLElement;

			if (dropDownMenuElement) {
				var rect = targetElement.getBoundingClientRect();
				var targetLeft = rect.left + self.win.pageXOffset - (<HTMLElement>self.doc.documentElement).clientLeft;

				let selectorLeftOffset = 0;
				var screenFalloff = targetLeft + dropDownMenuElement.clientWidth + 20 - self.win.innerWidth;

				if (screenFalloff > 0) {
					selectorLeftOffset -= screenFalloff;
				}

				dropDownMenuElement.style.left = selectorLeftOffset + (selectorLeftOffset == 0 ? '' : 'px');
			}
		});

		let body: Element = self.doc.getElementsByTagName('body')[0];

		setTimeout(() => {
			body.addEventListener('click', self.eventCloseMenu);
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

	eventToggleCheckBox = (event: Event) => {
		event.preventDefault();
		event.stopPropagation();

		let targetElement = <HTMLLabelElement>event.currentTarget;
		let checkbox = <HTMLInputElement>document.getElementById(targetElement.htmlFor);

		if (checkbox) {
			checkbox.checked = !checkbox.checked;
		}
	}

	private eventPreventDefault = (event: Event) => {
		event.preventDefault();
	}

	private eventStopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}