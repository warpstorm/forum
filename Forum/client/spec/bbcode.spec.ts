import { BBCode } from '../app/bbcode';
import { HtmlHelper } from './html-helper';
import * as chai from 'chai';

let html = new HtmlHelper();
html.loadDocumentFromPath('client/spec/bbcode.spec.html');

let bbCode = new BBCode((<Window>html.window()).document);

describe('BBCode', () => {
	it('should toggle bbc-spoiler-hover style on click', () => {
		bbCode.addSpoilerListener();

		chai.expect(hasClassAfterClick()).to.be.true;
		chai.expect(hasClassAfterClick()).to.be.false;

		function hasClassAfterClick() {
			let clickElement = <Element>html.get('.bbc-spoiler');
			html.click(clickElement);
			return clickElement.classList.contains('bbc-spoiler-hover');
		}
	});

	it('find textarea content', () => {
		let textareaElement = html.get('textarea');
		let textLength = ((<Element>textareaElement).textContent || "").length;

		chai.expect(textLength).to.be.greaterThan(0);
	});

	it('adds bbcode to textarea', () => {
		let textareaElement = <HTMLTextAreaElement>html.get('textarea');
		let originalTextLength = textareaElement.value.length;

		bbCode.addBBCodeListener();

		let clickElement = <Element>html.get('[bbcode="img"]');
		html.click(clickElement);

		chai.expect(textareaElement.value.length).to.be.greaterThan(originalTextLength);
	});
});