import { throwIfNull } from "./helpers";

export default function () {
	// expects document to be defined at the global scope.
	var easterEgg = new EasterEgg(document);
	easterEgg.addEasterEggListener();
}

export class EasterEgg {
	constructor(private doc: Document) {}

	addEasterEggListener(): void {
		let element = this.doc.querySelector('#easter-egg');

		throwIfNull(element, 'element');

		let self = this;

		element.addEventListener('mouseenter', function () {
			self.doc.getElementById('danger-sign').classList.remove('hidden');
		});

		element.addEventListener('mouseleave', function () {
			self.doc.getElementById('danger-sign').classList.add('hidden');
		});
	}
}