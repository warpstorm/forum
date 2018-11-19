type EventType = "click" | "mouseenter" | "mouseleave" | "mousedown" | "mouseup";

interface Element {
	hide(): void;
	show(): void;
	on(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
	off(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
}

Element.prototype.hide = function (): void {
	let classList = (<Element>this).classList;

	if (!classList) {
		throw new Error('Element does not contain a class list.');
	}

	if (!classList.contains('hidden')) {
		classList.add('hidden');
	}
};

Element.prototype.show = function (): void {
	let classList = (<Element>this).classList;

	if (!classList) {
		throw new Error('Element does not contain a class list.');
	}

	if (classList.contains('hidden')) {
		classList.remove('hidden');
	}
};

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.on = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	this.addEventListener(eventName, callback);
}

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.off = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	this.removeEventListener(eventName, callback);
} 
