type EventType = "click" | "mouseenter" | "mouseleave" | "mousedown" | "mouseup";

interface Element {
	hide(): void;
	show(): void;
	on(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
	off(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
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

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.on = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	(<Element>this).addEventListener(eventName, callback);
}

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.off = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	(<Element>this).removeEventListener(eventName, callback);
} 
