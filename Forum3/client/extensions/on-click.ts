interface Element {
	onClick(action: EventListenerOrEventListenerObject): void;
}

Element.prototype.onClick = function(action: EventListenerOrEventListenerObject): void {
	(<Element>this).addEventListener("click", action);
};
