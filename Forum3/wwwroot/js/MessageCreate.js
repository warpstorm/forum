$(function () {
	window.assignedBoards = [];
	$("[toggle-board]").on("click", ToggleBoard);
});

function ToggleBoard(event) {
	event.stopPropagation();

	if (this.toggleLock)
		return;

	this.toggleLock = true;

	if (window.assignedBoards === undefined)
		return;

	let boardId = parseInt($(this).attr("board-id"));

	let imgSrc = $("[board-flag=" + boardId + "]").attr("src");

	if (assignedBoards.includes(boardId)) {
		assignedBoards.remove(boardId);
		imgSrc = imgSrc.replace("checked", "unchecked");
		$("input[name='Selected_" + boardId + "']").val("False");
	}
	else {
		assignedBoards.push(boardId);
		imgSrc = imgSrc.replace("unchecked", "checked");
		$("input[name='Selected_" + boardId + "']").val("True");
	}

	$("[board-flag=" + boardId + "]").attr("src", imgSrc);

	this.toggleLock = false;
}