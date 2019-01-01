import { show, hide } from "../helpers";

export class EasterEgg {
	constructor(private doc: Document) {}

	init(): void {
		let element = <Element>this.doc.querySelector('#easter-egg');
		let dangerSign = <Element>this.doc.querySelector('#danger-sign');
		
		element.addEventListener('mouseenter', function () {
			show(dangerSign);
		});

		element.addEventListener('mouseleave', function () {
			hide(dangerSign);
		});
	}
}
