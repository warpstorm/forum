$(function () {
	$(".open-menu").each(function () {
		CloseMenu(this);
	});
	
	$("[clickable-link-parent]").on("mousedown", function (e) {
		var url = $(this).find("a").eq(0).attr("href");

		switch (e.which) {
			case 1:
				if (e.shiftKey)
					window.open(url, "_blank");
				else
					window.location.href = url;
				break;

			case 2:
				window.open(url, "_blank");
				break;
		}

		return true;
	});
});

function OpenMenu(menu) {
	$(menu).find(".drop-down-menu-wrapper").removeClass("hidden");
	$(menu).off("click.open-menu");

	$(menu).on("click.close-menu", function () {
		CloseMenu(menu);
	});

	setTimeout(function () {
		$("body").on("click.close-menu", function () {
			CloseMenu(menu);
		});
	}, 50);
}

function CloseMenu(menu) {
	$(menu).find(".drop-down-menu-wrapper").addClass("hidden");
	$(menu).off("click.close-menu");
	$("body").off("click.close-menu");

	$(menu).on("click.open-menu", function (e) {
		OpenMenu(menu);
	});
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