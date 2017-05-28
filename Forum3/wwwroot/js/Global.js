$(function () {
	$("body").on("click", function(e) {
		e.stopPropagation();
		$(".drop-down-menu-wrapper").hide();
	});

	$(".open-menu").on("click", function(e) {
		e.stopPropagation();
		$(this).children(".drop-down-menu-wrapper").show();
	});
	
	// TODO - enable shift-click and middle click to new windows
    $("[clickableLinkParent]").on("click", function (e) {
        e.stopPropagation();
        window.location.href = $(this).find("a").eq(0).attr("href");
    });
});

function PostToPath(path, parameters) {
	var form = $('<form></form>');

	form.attr("method", "post");
	form.attr("action", path);

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