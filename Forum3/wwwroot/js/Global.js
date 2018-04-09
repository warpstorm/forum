var BBCode = {
	"bold": "[b]  [/b]",
	"italics": "[i]  [/i]",
	"url": "[url=]  [/url]",
	"quote": "[quote]\n\n\n[/quote]",
	"img": "[img] [/img]",
	"underline": "[u]  [/u]",
	"strike": "[s]  [/s]",
	"color": "[color=#A335EE]  [/color]",
	"list": "[ul]\n[li] [/li]\n[li] [/li]\n[li] [/li]\n[/ul]",
	"numlist": "[ol]\n[li] [/li]\n[li] [/li]\n[li] [/li]\n[/ol]",
	"code": "[code]\n\n\n[/code]",
	"size": "[size=10]   [/size]",
};

$(function () {
	$(".open-menu").on("click.open-menu", OpenMenu);

	$("[clickable-link-parent] a").on("click", function () {
		event.preventDefault();
	});

	// TODO: Add middle click and shift click events too.
	$("[clickable-link-parent]").on("mousedown", OpenLink);

	ShowPages();

	$("#easter-egg").on("mouseenter", function () {
		$("#danger-sign").removeClass("hidden");
	});

	$("#easter-egg").on("mouseleave", function () {
		$("#danger-sign").addClass("hidden");
	});

	$(".add-smiley").on("click.show-smiley-selector", AddSmiley);

	$(".add-bbcode").on("click.add-bbcode", AddBBCode);
});

function AddBBCode(event) {
	event.preventDefault();

	var targetTextArea = $(this).parents("form").find("textarea")[0];
	var targetCode = $(this).attr("bbcode");

	InsertAtCaret(targetTextArea, BBCode[targetCode]);
}

function AddSmiley(event) {
	event.preventDefault();

	var targetTextArea = $(this).parents("form").find("textarea")[0];

	ShowSmileySelector(this, function (smileyImg) {
		var smileyCode = $(smileyImg).attr("code");

		if (targetTextArea.value !== "") {
			smileyCode = " " + smileyCode;
		}

		InsertAtCaret(targetTextArea, smileyCode);

		CloseSmileySelector();
	});
}

function ShowSmileySelector(target, imgCallback) {
	CloseSmileySelector();

	var buttonOffset = $(target).offset();
	var buttonHeight = $(target).outerHeight();

	$("#smiley-selector").show();

	var screenFalloff = buttonOffset.left + $("#smiley-selector").outerWidth() + 20 - window.innerWidth;

	var selectorLeftOffset = buttonOffset.left;

	if (screenFalloff > 0) {
		selectorLeftOffset -= screenFalloff;
	}

	var selectorTopOffset = buttonOffset.top + buttonHeight;

	$("#smiley-selector").offset({
		top: selectorTopOffset,
		left: selectorLeftOffset
	});

	$("#smiley-selector").on("click.smiley-selector-prevent-bubble", function (bubbleEvent) {
		bubbleEvent.stopPropagation();
	});

	setTimeout(function () {
		$("#smiley-selector img").on("click.smiley-image", function () {
			imgCallback(this);
		});

		$("body").on("click.close-smiley-selector", function () {
			CloseSmileySelector();
		});
	}, 50);
}

function CloseSmileySelector() {
	$("#smiley-selector").offset({
		top: 0,
		left: 0
	});

	$("#smiley-selector").hide();

	setTimeout(function () {
		$("body").off("click.close-smiley-selector");
		$("#smiley-selector img").off("click.smiley-image");
		$("#smiley-selector").off("click.smiley-selector-prevent-bubble");
	}, 50);
}

function ShowPages() {
	$(".pages").find(".unhide-pages").on("click", function () {
		$(this).parent().find(".page").removeClass("hidden");
		$(this).parent().find(".more-pages-before").addClass("hidden");
		$(this).parent().find(".more-pages-after").addClass("hidden");
	});

	$(".pages").each(function () {
		var pages = $(this).find(".page");

		if (window.currentPage === undefined)
			return;

		if (currentPage - 2 > 1)
			$(this).find(".more-pages-before").removeClass("hidden");

		if (currentPage + 2 < totalPages)
			$(this).find(".more-pages-after").removeClass("hidden");

		for (var i = currentPage - 2; i < currentPage; i++) {
			if (i < 0)
				continue;

			$(pages[i - 1]).removeClass("hidden");
		}

		for (var i = currentPage; i <= currentPage + 2; i++) {
			if (i - 1 > pages.length)
				continue;

			$(pages[i - 1]).removeClass("hidden");
		}
	});
}

function OpenLink(event) {
	event.stopPropagation();

	var url;

	if ($(event.target).is("a"))
		url = $(event.target).attr("href");
	else
		url = $(event.target).closest("[clickable-link-parent]").find("a").eq(0).attr("href");

	switch (event.which) {
		case 1:
			if (event.shiftKey)
				window.open(url, "_blank");
			else
				window.location.href = url;
			break;

		case 2:
			window.open(url, "_blank");
			break;
	}

	return true;
}

function OpenMenu() {
	CloseMenu();

	$(this).off("click.open-menu");
	$(this).on("click.close-menu", CloseMenu);
	$(this).find(".menu-wrapper").removeClass("hidden");

	setTimeout(function () {
		$("body").on("click.close-menu", CloseMenu);
	}, 50);
}

function CloseMenu() {
	var dropDownMenus = $(".menu-wrapper");

	for (var i = 0; i < dropDownMenus.length; i++) {
		var dropDownMenu = $(dropDownMenus[i]);

		if (!dropDownMenu.hasClass("hidden"))
			dropDownMenu.addClass("hidden");
	}

	$(".open-menu").off("click.close-menu");
	$("body").off("click.close-menu");

	$(".open-menu").off("click.open-menu");
	$(".open-menu").on("click.open-menu", OpenMenu);
}

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



/*----------------------------------------------------------------------------
 * Prototypes and Browser compatibility
 */

// https://blog.mariusschulz.com/2016/07/16/removing-elements-from-javascript-arrays
Object.defineProperty(Array.prototype, 'remove', {
	value: function (element) {
		let index = this.indexOf(element);

		if (index !== -1)
			this.splice(index, 1);
	}
});

// https://tc39.github.io/ecma262/#sec-array.prototype.includes
if (!Array.prototype.includes) {
	Object.defineProperty(Array.prototype, 'includes', {
		value: function (searchElement, fromIndex) {

			if (this === null) {
				throw new TypeError('"this" is null or not defined');
			}

			// 1. Let O be ? ToObject(this value).
			var o = Object(this);

			// 2. Let len be ? ToLength(? Get(O, "length")).
			var len = o.length >>> 0;

			// 3. If len is 0, return false.
			if (len === 0) {
				return false;
			}

			// 4. Let n be ? ToInteger(fromIndex).
			//    (If fromIndex is undefined, this step produces the value 0.)
			var n = fromIndex | 0;

			// 5. If n ≥ 0, then
			//  a. Let k be n.
			// 6. Else n < 0,
			//  a. Let k be len + n.
			//  b. If k < 0, let k be 0.
			var k = Math.max(n >= 0 ? n : len - Math.abs(n), 0);

			function sameValueZero(x, y) {
				return x === y || (typeof x === 'number' && typeof y === 'number' && isNaN(x) && isNaN(y));
			}

			// 7. Repeat, while k < len
			while (k < len) {
				// a. Let elementK be the result of ? Get(O, ! ToString(k)).
				// b. If SameValueZero(searchElement, elementK) is true, return true.
				if (sameValueZero(o[k], searchElement)) {
					return true;
				}
				// c. Increase k by 1. 
				k++;
			}

			// 8. Return false
			return false;
		}
	});
}