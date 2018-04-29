function PostToPath(path, parameters) {
	var antiForgeryTokenValue = $("input[name=__RequestVerificationToken]").val();

	var form = $('<form></form>');

	form.attr("method", "post");
	form.attr("action", path);

	var antiForgeryToken = $("<input />");
	antiForgeryToken.attr("type", "hidden");
	antiForgeryToken.attr("name", "__RequestVerificationToken");
	antiForgeryToken.attr("value", antiForgeryTokenValue);
	form.append(antiForgeryToken);

	$.each(parameters, function (key, value) {
		var field = $('<input></input>');

		field.attr("type", "hidden");
		field.attr("name", key);
		field.attr("value", value);

		form.append(field);
	});

	$(document.body).append(form);
	form.submit();
}

// for inserting text into textareas at the cursor location
function InsertAtCaret(areaElement, text) {
	var scrollPos = areaElement.scrollTop;
	var strPos = 0;
	var br = ((areaElement.selectionStart || areaElement.selectionStart === '0') ? "ff" : (document.selection ? "ie" : false));
	var range;

	if (br === "ie") {
		areaElement.focus();
		range = document.selection.createRange();
		range.moveStart('character', -areaElement.value.length);
		strPos = range.text.length;
	} else if (br === "ff") {
		strPos = areaElement.selectionStart;
	}

	var front = (areaElement.value).substring(0, strPos);
	var back = (areaElement.value).substring(strPos, areaElement.value.length);

	areaElement.value = front + text + back;

	strPos = strPos + text.length;

	if (br === "ie") {
		areaElement.focus();
		range = document.selection.createRange();
		range.moveStart('character', -areaElement.value.length);
		range.moveStart('character', strPos);
		range.moveEnd('character', 0);
		range.select();
	} else if (br === "ff") {
		areaElement.selectionStart = strPos;
		areaElement.selectionEnd = strPos;
		areaElement.focus();
	}

	areaElement.scrollTop = scrollPos;
}