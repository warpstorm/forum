$(function () {
    $("[clickableLinkParent]").on("click", function (e) {
        e.stopPropagation();
        window.location.href = $(this).find("a").eq(0).attr("href");
    });
});