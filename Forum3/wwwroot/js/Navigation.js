$(function () {
	$(".open-menu").on("click.open-menu", OpenMenu);

	$("[clickable-link-parent] a").on("click", function () {
		event.preventDefault();
	});

	BindLinks();
	ShowPages();
});

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

function BindLinks() {
	$("[clickable-link-parent]").off("boundLinks");

	if (isFirefox)
		$("[clickable-link-parent]").on("click.boundLinks", OpenLink);
	else
		$("[clickable-link-parent]").on("mousedown.boundLinks", OpenLink);
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