
interface Element {
	on(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
	off(eventName: EventType, callback: EventListenerOrEventListenerObject): void;
}

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.on = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	this.addEventListener(eventName, callback);
}

// Using this prototype instead of addEventListener directly ensures that the eventName is always compliant.
Element.prototype.off = function (eventName: EventType, callback: EventListenerOrEventListenerObject): void {
	this.removeEventListener(eventName, callback);
} 
