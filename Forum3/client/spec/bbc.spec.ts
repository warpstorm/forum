import { BBCode } from '../scripts/bbc';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let html = new HtmlHelper();
html.loadDocumentFromPath('client/spec/bbc.spec.html');

let bbCode = new BBCode(html.window().document);

describe('BBCode', () => {
	it('should toggle bbc-spoiler-hover style on click', () => {
		bbCode.addSpoilerListener();

		chai.expect(hasClassAfterClick()).to.be.true;
		chai.expect(hasClassAfterClick()).to.be.false;

		function hasClassAfterClick() {
			let clickElement = html.get('.bbc-spoiler');
			html.click(clickElement);
			return clickElement.classList.contains('bbc-spoiler-hover');
		}
	});

	it('find textarea content', () => {
		let textareaElement = html.get('textarea');
		let textLength = textareaElement.textContent.length;

		chai.expect(textLength).to.be.greaterThan(0);
	});

	it('adds bbcode to textarea', () => {
		let textareaElement = html.get('textarea');
		let originalTextLength = textareaElement.textContent.length;

		bbCode.addBBCodeListener();

		let clickElement = html.get('[bbcode="img"]');
		html.click(clickElement);

		chai.expect(textareaElement.textContent.length).to.be.greaterThan(originalTextLength);
	});
});