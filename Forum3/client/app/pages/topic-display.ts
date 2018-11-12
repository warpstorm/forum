//$(function () {
//	$(".reply-button").on("click.show-reply-form", ShowReplyForm);

//	$(".thought-button").on("click.show-smiley-selector", function (e) {
//		e.preventDefault();

//		var messageId = $(this).attr("message-id");

//		ShowSmileySelector(this, function (smileyImg) {
//			var smileyId = $(smileyImg).attr("smiley-id");

//			PostToPath("/Messages/AddThought", {
//				MessageId: messageId,
//				SmileyId: smileyId
//			});

//			CloseSmileySelector();
//		});
//	});

//	$("blockquote.reply").on("click.show-full-reply", ShowFullReply);

//	$("[toggle-board]").on("click", ToggleBoard);

//	if (window.showFavicons)
//		$(".link-favicon").show();
//});

//function ToggleBoard(event) {
//	event.stopPropagation();

//	let self = this;

//	if (self.toggling)
//		return;

//	self.toggling = true;

//	if (window.assignedBoards === undefined || window.togglePath === undefined)
//		return;

//	let boardId = parseInt($(this).attr("board-id"));

//	let imgSrc = $("[board-flag=" + boardId + "]").attr("src");

//	if (assignedBoards.includes(boardId)) {
//		assignedBoards.remove(boardId);
//		imgSrc = imgSrc.replace("checked", "unchecked");
//	}
//	else {
//		assignedBoards.push(boardId);
//		imgSrc = imgSrc.replace("unchecked", "checked");
//	}

//	$("[board-flag=" + boardId + "]").attr("src", imgSrc);

//	$.get(togglePath + "&BoardId=" + boardId, function () {
//		self.toggling = false;
//	});
//}

//function ShowFullReply() {
//	$(this).off("click.show-full-reply");
//	$(this).on("click.close-full-reply", CloseFullReply);

//	$(this).find(".reply-preview").addClass("hidden");
//	$(this).find(".reply-body").removeClass("hidden");
//}

//function CloseFullReply() {
//	$(this).find(".reply-body").addClass("hidden");
//	$(this).find(".reply-preview").removeClass("hidden");

//	$(this).off("click.show-full-reply");
//	$(this).on("click.show-full-reply", ShowFullReply);
//}

//function ShowReplyForm() {
//	$(".reply-form").not(".hidden").addClass("hidden");
//	$(".reply-button").off("click.show-reply-form");
//	$(".reply-button").off("click.hide-reply-form");
//	$(".reply-button").on("click.show-reply-form", ShowReplyForm);
//	$(this).off("click.show-reply-form");
//	$(this).parents("section").find(".reply-form").removeClass("hidden");
//	$(this).on("click.hide-reply-form", HideReplyForm);
//}

//function HideReplyForm() {
//	$(this).off("click.hide-reply-form");
//	$(this).parents("section").find(".reply-form").addClass("hidden");
//	$(this).on("click.show-reply-form", ShowReplyForm);
//}