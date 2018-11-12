import { isString } from "util";

type Parameter = { [key: string]: string };

export function postToPath(path: string, parameters: Parameter[]): void {
	throwIfNull(path, 'path');
	throwIfNull(parameters, 'parameters');

	let antiForgeryTokenElements = document.querySelectorAll('input[name=__RequestVerificationToken]');

	let antiForgeryTokenValue: string;

	if (antiForgeryTokenElements && antiForgeryTokenElements.length > 0) {
		let antiForgeryTokenElement = antiForgeryTokenElements[0] as HTMLInputElement;
		antiForgeryTokenValue = antiForgeryTokenElement.value;
	}
	else {
		return;
	}

	let form = new HTMLFormElement();
	form.method = "post";
	form.action = path;

	let antiForgeryToken = new HTMLInputElement();
	antiForgeryToken.type = "hidden";
	antiForgeryToken.name = "__RequestVerificationToken";
	antiForgeryToken.value = antiForgeryTokenValue;

	form.append(antiForgeryToken);

	for (let parameter of parameters) {
		let field = new HTMLInputElement();
		field.type = "hidden";
		field.name = parameter.key;
		field.value = parameters[parameter.key];

		form.append(field);
	}

	document.body.append(form);

	form.submit();
}

// for inserting text into textareas at the cursor location
export function insertAtCaret(areaElement: HTMLTextAreaElement, text: string): void {
	throwIfNull(areaElement, 'areaElement');
	throwIfNull(text, 'text');

	let compatible = areaElement.selectionStart || areaElement.selectionStart == 0;

	if (!compatible) {
		areaElement.textContent += text;
		return;
	}

	let startPos = areaElement.selectionStart;
	let endPos = areaElement.selectionEnd;

	let front = (areaElement.value).substring(0, startPos);
	let replaced = (areaElement.value).substring(startPos, endPos);
	let back = (areaElement.value).substring(endPos, areaElement.value.length);

	areaElement.value = front + text + replaced + back;

	areaElement.selectionStart = startPos + text.length;
	areaElement.selectionEnd = startPos + text.length + replaced.length;
	areaElement.focus();

	areaElement.scrollTop = endPos;
}

export function throwIfNull(value: any, name: string): void {
	if (!value) {
		throw new Error(`value of "${name}" is invalid`);
	}

	// I prefer to treat empty strings as null due to my time with C#
	if (isString(value)) {
		value = value.trim();

		if (value.length == 0) {
			throw new Error(`value of "${name}" is invalid`);
		}
	}
}

export function isFirefox() {
	// https://stackoverflow.com/a/26358856/2621693
	if (navigator && navigator.userAgent && navigator.userAgent.indexOf("Firefox") != -1) {
		return true;
	}

	return false;
}

export function queryify(parameters: any = {}): string {
	return Object
		.keys(parameters)
		.map(key => `${encodeURIComponent(key)}=${encodeURIComponent(parameters[key])}`)
		.join('&');
}