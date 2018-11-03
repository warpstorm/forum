$(function () {
	$("#easter-egg").on("mouseenter", function () {
		$("#danger-sign").removeClass("hidden");
	});

	$("#easter-egg").on("mouseleave", function () {
		$("#danger-sign").addClass("hidden");
	});
});