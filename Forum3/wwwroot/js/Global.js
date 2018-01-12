$(function () {
	$(".open-menu").on("click.open-menu", OpenMenu);

	$("[clickable-link-parent]").on("mousedown", OpenLink);

	ShowPages();
});

function ShowPages() {
	$(".pages").find("li:first").on("click", function () {
		$(this).parent().find(".page").removeClass("hidden");
	});

	$(".pages").each(function () {
		var pages = $(this).find(".page");

		for (var i = window.currentPage - 2; i < window.currentPage; i++) {
			if (i < 0)
				continue;

			$(pages[i - 1]).removeClass("hidden");
		}

		for (var i = window.currentPage; i <= window.currentPage + 2; i++) {
			if (i - 1 > pages.length)
				continue;

			$(pages[i - 1]).removeClass("hidden");
		}
	});
}

function OpenLink(event) {
	var url;

	if ($(event.target).is("a"))
		url = $(event.target).attr("href");
	else
		url = $(event.target).find("a").eq(0).attr("href");

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
	$(this).find(".drop-down-menu-wrapper").removeClass("hidden");

	setTimeout(function () {
		$("body").on("click.close-menu", CloseMenu);
	}, 50);
}

function CloseMenu() {
	var dropDownMenus = $(".drop-down-menu-wrapper");

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