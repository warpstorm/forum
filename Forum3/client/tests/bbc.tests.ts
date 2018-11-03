import { BBCode } from '../scripts/bbc';

import * as chai from 'chai';

const expect = chai.expect;

describe('BBCode', () => {
	let bbCode = new BBCode();

	it('should add or remove hover style', () => {
		let span = new HTMLSpanElement();
		span.classList.add("bbc-spoiler-hover");

		span.addEventListener

		let event = new Event("thing");
		event.currentTarget = span;

		bbCode.showSpoiler(event);

		expect(event.currentTarget).to.equal(7);
	});
});