interface Element {
	hide(): void;
	show(): void;
	onClick(action: EventListenerOrEventListenerObject): void;
}

Element.prototype.hide = function (): void {
	(<Element>this).classList.add('hidden');
};

Element.prototype.onClick = function (action: EventListenerOrEventListenerObject): void {
	(<Element>this).addEventListener("click", action);
};

Element.prototype.show = function (): void {
	(<Element>this).classList.remove('hidden');
};