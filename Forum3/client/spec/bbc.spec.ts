import { BBCode } from '../scripts/bbc';

import 'mocha';
import * as chai from 'chai';

describe('BBCode', () => {
	let bbCode = new BBCode();
	
	it('should add or remove hover style', () => {
		let span = new HTMLSpanElement();
		span.classList.add('bbc-spoiler-hover');
		span.addEventListener('test', bbCode.insertBBCode);

		let event = new Event('test');
		span.dispatchEvent(event);

		chai.expect(span.classList.contains('bbc-spoiler-hover')).to.equal(false);
	});
});