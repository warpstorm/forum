import { throwIfNull } from "./helpers";

export default function () {
	// expects document to be defined at the global scope.
	var easterEgg = new EasterEgg(document);
	easterEgg.addEasterEggListener();
}

export class EasterEgg {
	constructor(private htmlDocument: Document) {}

	addEasterEggListener(): void {
		let element = this.htmlDocument.querySelector('#easter-egg');

		throwIfNull(element, 'element');

		let self = this;

		element.addEventListener('mouseenter', function () {
			self.htmlDocument.getElementById('danger-sign').classList.remove('hidden');
		});

		element.addEventListener('mouseleave', function () {
			self.htmlDocument.getElementById('danger-sign').classList.add('hidden');
		});
	}
}