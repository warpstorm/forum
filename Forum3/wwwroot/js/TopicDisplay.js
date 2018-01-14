$(function () {
	$(".reply-button").on("click.show-reply-form", ShowReplyForm);

	$(".thought-button").on("click.show-smiley-selector", function (e) {
		e.preventDefault();

		var messageId = $(this).attr("message-id");

		ShowSmileySelector(this, function (smileyImg) {
			var smileyId = $(smileyImg).attr("smiley-id");

			PostToPath("/Messages/AddThought", {
				MessageId: messageId,
				SmileyId: smileyId
			});

			CloseSmileySelector();
		});
	});

	$("blockquote.reply").on("click.show-full-reply", ShowFullReply);

	$("[toggle-board]").on("click", ToggleBoard);
});

function ToggleBoard(event) {
	event.stopPropagation();

	let self = this;

	if (self.toggling)
		return;

	self.toggling = true;

	if (window.assignedBoards === undefined || window.togglePath === undefined)
		return;

	let boardId = parseInt($(this).attr("board-id"));

	let imgSrc = $("[board-flag=" + boardId + "]").attr("src");

	if (assignedBoards.includes(boardId)) {
		assignedBoards.remove(boardId);
		imgSrc = imgSrc.replace("checked", "unchecked");
	}
	else {
		assignedBoards.push(boardId);
		imgSrc = imgSrc.replace("unchecked", "checked");
	}

	$("[board-flag=" + boardId + "]").attr("src", imgSrc);

	$.get(togglePath + "&BoardId=" + boardId, function () {
		self.toggling = false;
	});
}

function ShowFullReply() {
	$(this).off("click.show-full-reply");
	$(this).on("click.close-full-reply", CloseFullReply);

	$(this).find(".reply-preview").addClass("hidden");
	$(this).find(".reply-body").removeClass("hidden");
}

function CloseFullReply() {
	$(this).find(".reply-body").addClass("hidden");
	$(this).find(".reply-preview").removeClass("hidden");

	$(this).off("click.show-full-reply");
	$(this).on("click.show-full-reply", ShowFullReply);
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

function ShowReplyForm() {
    $(".reply-form").not(".hidden").addClass("hidden");
    $(".reply-button").off("click.show-reply-form");
    $(".reply-button").off("click.hide-reply-form");
    $(".reply-button").on("click.show-reply-form", ShowReplyForm);
    $(this).off("click.show-reply-form");
    $(this).parents("section").find(".reply-form").removeClass("hidden");
    $(this).on("click.hide-reply-form", HideReplyForm);
}

function HideReplyForm() {
    $(this).off("click.hide-reply-form");
    $(this).parents("section").find(".reply-form").addClass("hidden");
    $(this).on("click.show-reply-form", ShowReplyForm);
}