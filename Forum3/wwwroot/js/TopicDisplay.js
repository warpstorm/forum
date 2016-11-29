$(function () {
	$(".replyButton").on("click", function () {
		$(this).parents("section").find(".replyControl").removeClass("hide");
	});

	$(".editButton").on("click", function () {
		$(this).parents("section").find(".editControl").removeClass("hide");
	});
});