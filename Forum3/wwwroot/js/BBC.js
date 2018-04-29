var BBCode = {
	"bold": "[b]  [/b]",
	"italics": "[i]  [/i]",
	"url": "[url=]  [/url]",
	"quote": "[quote]\n\n\n[/quote]",
	"spoiler": "[spoiler]  [/spoiler]",
	"img": "[img]  [/img]",
	"underline": "[u]  [/u]",
	"strike": "[s]  [/s]",
	"color": "[color=#A335EE]  [/color]",
	"list": "[ul]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ul]",
	"numlist": "[ol]\n[li]  [/li]\n[li]  [/li]\n[li]  [/li]\n[/ol]",
	"code": "[code]\n\n\n[/code]",
	"size": "[size=10]  [/size]",
};

$(function () {
	$(".add-bbcode").on("click.add-bbcode", AddBBCode);
});

function AddBBCode(event) {
	event.preventDefault();

	var targetTextArea = $(this).parents("form").find("textarea")[0];
	var targetCode = $(this).attr("bbcode");

	InsertAtCaret(targetTextArea, BBCode[targetCode]);
}

function ShowSpoiler(event) {
	// in case they click a link in a spoiler
	event.preventDefault();

	// in case they click a spoiler that is in a link
	event.stopPropagation();

	if ($(this).hasClass("bbc-spoiler-hover"))
		$(this).removeClass("bbc-spoiler-hover");
	else
		$(this).addClass("bbc-spoiler-hover");
}