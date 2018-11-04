import { throwIfNull } from "./helpers";

export default function () {
	var easterEgg = new EasterEgg();

	// expects document to be defined at the global scope.
	easterEgg.addEasterEggListener(document);
}

export class EasterEgg {
	addEasterEggListener(htmlDocument: Document): void {
		throwIfNull(htmlDocument, 'htmlDocument');

		let element = htmlDocument.getElementById('easter-egg');

		if (!element) {
			throw new Error('Element is not defined');
		}

		element.addEventListener('mouseenter', function () {
			htmlDocument.getElementById('danger-sign').classList.remove('hidden');
		});

		element.addEventListener('mouseleave', function () {
			htmlDocument.getElementById('danger-sign').classList.add('hidden');
		});
	}
}