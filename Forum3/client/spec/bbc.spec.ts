import { BBCode } from '../scripts/bbc';

import * as chai from 'chai';
import { JSDOM } from 'jsdom';

describe('BBCode', () => {
	let bbCode = new BBCode();
	let jsdom = new JSDOM();
	let document = jsdom.window.document;

	it('should contain hover style', () => {
		let span = document.createElement('span');
		span.classList.add('bbc-spoiler-hover');
		chai.expect(span.classList.contains('bbc-spoiler-hover')).to.equal(true);
	});

	it('should remove hover style', () => {
		let event = document.createEvent('HTMLEvents');
		event.initEvent('test', false, true);

		let span = document.createElement('span');
		span.classList.add('bbc-spoiler-hover');
		span.addEventListener('test', bbCode.showSpoiler);
		span.dispatchEvent(event);

		chai.expect(span.classList.contains('bbc-spoiler-hover')).to.equal(false);
	});
});