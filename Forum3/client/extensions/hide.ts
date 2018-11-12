interface Element {
	hide(): void;
}

Element.prototype.hide = function(): void {
	(<Element>this).classList.add('hidden');
};
