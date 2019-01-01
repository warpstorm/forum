import { EasterEgg } from '../app/services/easter-egg';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let html = new HtmlHelper();
html.loadDocumentFromPath('client/spec/easter-egg.spec.html');

let easterEgg = new EasterEgg((<Window>html.window()).document);

describe('EasterEgg', () => {
	it('should find a hidden danger-sign', () => {
		easterEgg.init();

		let targetElement = <Element>html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(true);
	});

	it('should remove hidden on mouseenter', () => {
		easterEgg.init();

		let eventElement = <Element>html.get('#easter-egg');
		html.mouseEnter(eventElement);

		let targetElement = <Element>html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(false);
	});

	it('should replace hidden on mouseleave', () => {
		easterEgg.init();

		let eventElement = <Element>html.get('#easter-egg');
		html.mouseEnter(eventElement);
		html.mouseLeave(eventElement);

		let targetElement = <Element>html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(true);
	});
});