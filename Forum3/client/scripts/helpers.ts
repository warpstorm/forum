type Parameter = { [key: string]: string };

export function postToPath(path, parameters: Parameter[]) {
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
export function insertAtCaret(areaElement: HTMLTextAreaElement, text: string) {
	if (!areaElement) {
		console.log("Undefined element");
		return;
	}

	if (!text || text.length == 0) {
		console.log("No text specified");
		return;
	}

	let compatible = areaElement.selectionStart || areaElement.selectionStart == 0;

	if (!compatible) {
		areaElement.value += text;
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