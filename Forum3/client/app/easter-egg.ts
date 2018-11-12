import { throwIfNull } from "./helpers";

export default function () {
	// expects document to be defined at the global scope.
	var easterEgg = new EasterEgg(document);
	easterEgg.addEasterEggListener();
}

export class EasterEgg {
	constructor(private html: Document) {}

	addEasterEggListener(): void {
		let element = this.html.querySelector('#easter-egg');

		throwIfNull(element, 'element');

		let self = this;

		element.addEventListener('mouseenter', function () {
			self.html.getElementById('danger-sign').classList.remove('hidden');
		});

		element.addEventListener('mouseleave', function () {
			self.html.getElementById('danger-sign').classList.add('hidden');
		});
	}
}