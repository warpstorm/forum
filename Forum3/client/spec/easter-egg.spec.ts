import { EasterEgg } from '../scripts/easter-egg';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let html = new HtmlHelper();
let document = html.document();
let easterEgg = new EasterEgg(document);

describe('EasterEgg', () => {
	it('should remove hidden on mouseenter', () => {
		let eventElement = html.element('div');
		eventElement.setAttribute('id', 'easter-egg');

		let targetElement = html.element('div');
		targetElement.setAttribute('id', 'danger-sign');
		targetElement.classList.add('hidden');

		easterEgg.addEasterEggListener();

		eventElement.dispatchEvent(html.event('mouseenter'));

		chai.expect(targetElement.classList.contains('hidden')).to.equal(false);
	});

	it('should replace hidden on mouseenter', () => {
		let eventElement = document.createElement('div');
		eventElement.setAttribute('id', 'easter-egg');

		let targetElement = document.createElement('div');
		targetElement.setAttribute('id', 'danger-sign');
		targetElement.classList.add('hidden');

		easterEgg.addEasterEggListener();

		eventElement.dispatchEvent(html.event('mouseenter'));
		eventElement.dispatchEvent(html.event('mouseleave'));

		chai.expect(targetElement.classList.contains('hidden')).to.equal(true);
	});
});