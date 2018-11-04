import { BBCode } from '../scripts/bbc';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let bbCode = new BBCode();
let html = new HtmlHelper();
let document = html.document();

describe('BBCode', () => {
	it('should contain hover style', () => {
		let element = document.createElement('span');
		element.classList.add('bbc-spoiler-hover');

		chai.expect(element.classList.contains('bbc-spoiler-hover')).to.equal(true);
	});

	it('should remove hover style', () => {
		let element = document.createElement('span');
		element.classList.add('bbc-spoiler-hover');
		element.addEventListener('click', bbCode.showSpoiler);
		element.dispatchEvent(html.event('click'));

		chai.expect(element.classList.contains('bbc-spoiler-hover')).to.equal(false);
	});
});