$(function () {
	$(".add-smiley").on("click.show-smiley-selector", AddSmiley);
});

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