import { throwIfNull } from "./helpers";

export class EasterEgg {
	constructor(private doc: Document) {}

	init(): void {
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