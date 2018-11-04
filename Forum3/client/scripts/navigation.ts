import { isFirefox } from "./helpers";

export default function () {
	// expects document to be defined at the global scope.
	let navigation = new Navigation(document);
	navigation.addListeners();
}

export class Navigation {
	constructor(private htmlDocument: Document) {}

	addListeners() {
		this.addMenuListeners();
		this.addLinkListeners();
		this.showPages();
	}

	addMenuListeners() {
		this.htmlDocument.querySelectorAll(".open-menu").forEach(element => {
			element.addEventListener('click', this.openMenu);
		});
	}

	showPages() {
		if ((<any>this.htmlDocument).currentPage === undefined || (<any>this.htmlDocument).totalPages === undefined)
			return;

		this.htmlDocument.querySelectorAll('.pages').forEach(pageElement => {
			pageElement.querySelectorAll('.unhide-pages').forEach(unhidePagesElement => {
				unhidePagesElement.addEventListener('click', function () {
					let parentElement = unhidePagesElement.parentElement;

					parentElement.querySelectorAll('.page').forEach(element => {
						element.classList.remove('hidden');
					});

					parentElement.querySelectorAll('.more-pages-before').forEach(element => {
						element.classList.add('hidden');
					});

					parentElement.querySelectorAll('.more-pages-after').forEach(element => {
						element.classList.add('hidden');
					});
				})
			});

			let pageElements = pageElement.querySelectorAll(".page");

			let currentPage = (<any>this.htmlDocument).currentPage;
			let totalPages = (<any>this.htmlDocument).totalPages;

			if (currentPage - 2 > 1) {
				pageElement.querySelectorAll('.more-pages-before').forEach(element => {
					element.classList.remove('hidden');
				});
			}

			if (currentPage + 2 < totalPages) {
				pageElement.querySelectorAll('.more-pages-after').forEach(element => {
					element.classList.add('hidden');
				});
			}

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
		});
	}

	addLinkListeners() {
		let linkParents = document.querySelectorAll('[clickable-link-parent]')

		for (var i = 0; i < linkParents.length; i++) {
			let linkParent = linkParents[i];

			linkParent.querySelector('a').addEventListener('click', this.preventDefault);

			if (isFirefox()) {
				linkParent.removeEventListener('click', this.openLink);
				linkParent.addEventListener('click', this.openLink);
			}
			else {
				linkParent.removeEventListener('mousedown', this.openLink);
				linkParent.addEventListener('mousedown', this.openLink);
			}
		}
	}

	openLink = (event: Event) => {
		this.stopPropagation(event);

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

	openMenu = (event: Event) => {
		this.closeMenu(event);

		let targetElement = <HTMLElement>event.currentTarget;

		targetElement.removeEventListener('click', this.openMenu);
		targetElement.addEventListener('click', this.closeMenu);

		targetElement.querySelectorAll('.menu-wrapper').forEach(element => {
			element.classList.remove("hidden");
		});

		let body = this.htmlDocument.getElementsByTagName('body')[0];

		setTimeout(() => {
			body.addEventListener('click', this.closeMenu);
		}, 50);
	}

	closeMenu = (event: Event) => {
		var dropDownMenuElements = this.htmlDocument.querySelectorAll('.menu-wrapper');

		for (var i = 0; i < dropDownMenuElements.length; i++) {
			var dropDownMenuElement = dropDownMenuElements[i];

			if (!dropDownMenuElement.classList.contains('hidden'))
				dropDownMenuElement.classList.add('hidden');
		}

		this.htmlDocument.querySelectorAll('.open-menu').forEach(element => {
			element.removeEventListener('click', this.closeMenu);
			element.removeEventListener('click', this.openMenu);
			element.addEventListener('click', this.openMenu);
		});

		this.htmlDocument.getElementsByTagName('body')[0].removeEventListener('click', this.closeMenu);
	}

	preventDefault = (event: Event) => {
		event.preventDefault();
	}

	stopPropagation = (event: Event) => {
		event.stopPropagation();
	}
}