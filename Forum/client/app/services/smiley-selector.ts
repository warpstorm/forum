import { insertAtCaret, show, hide } from '../helpers';

export class SmileySelector {
    private body: HTMLBodyElement;

    constructor(private doc: Document) {
        this.body = doc.getElementsByTagName('body')[0];
    }

    // Used in message forms to insert smileys into textareas.
    init(): void {
        this.doc.querySelectorAll('.add-smiley').forEach(element => {
            element.removeEventListener('click', this.eventShowSmileySelector);
            element.addEventListener('click', this.eventShowSmileySelector);
        });

        this.doc.querySelectorAll('[data-component="smiley-image"]').forEach(element => {
            element.removeEventListener('click', this.eventInsertSmileyCode);
            element.addEventListener('click', this.eventInsertSmileyCode);
        })
    }

    eventShowSmileySelector = (event: Event): void => {
        let self = this;
        let target = <HTMLElement>event.currentTarget;
        let smileySelector = target.querySelector('[data-component="smiley-selector"]');
        show(smileySelector);

        target.removeEventListener('click', self.eventShowSmileySelector);
        target.addEventListener('click', self.eventCloseSmileySelector);

        setTimeout(function () {
            self.body.addEventListener('click', self.eventCloseSmileySelector);
        }, 50);
    }

    eventCloseSmileySelector = (event: Event): void => {
        let self = this;

        let target = <HTMLElement>event.currentTarget;
        let smileySelector = target.querySelector('[data-component="smiley-selector"]');
        hide(smileySelector);

        target.removeEventListener('click', self.eventCloseSmileySelector);
        target.addEventListener('click', self.eventShowSmileySelector);

        self.body.removeEventListener('click', self.eventCloseSmileySelector);
    }

    eventInsertSmileyCode = (event: Event): void => {
        let self = this;

        let eventTarget = <Element>event.currentTarget
        let smileyCode = eventTarget.getAttribute('code') || '';

        let form = <HTMLFormElement>eventTarget.closest('form');
        let targetTextArea = <HTMLTextAreaElement>form.querySelector('textarea');

        if (targetTextArea.value !== '') {
            smileyCode = ` ${smileyCode} `;
        }

        insertAtCaret(targetTextArea, smileyCode);

        self.eventCloseSmileySelector(event);
    }
}
