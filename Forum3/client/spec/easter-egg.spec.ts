import { EasterEgg } from '../scripts/easter-egg';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let html = new HtmlHelper();
html.loadDocumentFromPath('client/spec/easter-egg.spec.html');

let easterEgg = new EasterEgg(html.window().document);

describe('EasterEgg', () => {
	it('should find a hidden danger-sign', () => {
		easterEgg.addEasterEggListener();

		let targetElement = html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(true);
	});

	it('should remove hidden on mouseenter', () => {
		easterEgg.addEasterEggListener();

		let eventElement = html.get('#easter-egg');
		html.mouseEnter(eventElement);

		let targetElement = html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(false);
	});

	it('should replace hidden on mouseleave', () => {
		easterEgg.addEasterEggListener();

		let eventElement = html.get('#easter-egg');
		html.mouseEnter(eventElement);
		html.mouseLeave(eventElement);

		let targetElement = html.get('#danger-sign');
		chai.expect(targetElement.classList.contains('hidden')).to.equal(true);
	});
});