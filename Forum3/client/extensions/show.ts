interface Element {
	show(): void;
}

Element.prototype.show = function (): void {
	(<Element>this).classList.remove('hidden');
};