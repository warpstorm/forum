type MouseEventType = "click" | "mouseenter" | "mouseleave" | "mousedown" | "mouseup";

interface Element {
	hide(): void;
	show(): void;
	on(eventName: MouseEventType, callback: EventListenerOrEventListenerObject): void;
	off(eventName: MouseEventType, callback: EventListenerOrEventListenerObject): void;
}

Element.prototype.hide = function (): void {
	if (!(<Element>this).classList.contains('hidden')) {
		(<Element>this).classList.add('hidden');
	}
};

Element.prototype.show = function (): void {
	if ((<Element>this).classList.contains('hidden')) {
		(<Element>this).classList.remove('hidden');
	}
};

Element.prototype.on = function (eventName: MouseEventType, callback: EventListenerOrEventListenerObject): void {
	(<Element>this).addEventListener(eventName, callback);
}

Element.prototype.off = function (eventName: MouseEventType, callback: EventListenerOrEventListenerObject): void {
	(<Element>this).removeEventListener(eventName, callback);
} 
