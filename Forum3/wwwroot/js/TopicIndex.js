$(function () {
	if (window.after > 0) {
		$("#load-more-topics").removeClass("hidden");
		$("#load-more-topics").on("click", LoadMoreTopics);
	}
});

function LoadMoreTopics() {
	$("#load-more-topics").hide();

	$.ajax({
		dataType: "html",
		url: "/topics/indexmore/" + window.boardId + "/?after=" + window.after,
		success: function (data) {
			$("#topic-list").append(data);

			if (window.moreTopics)
				$("#load-more-topics").show();
		}
	});
}