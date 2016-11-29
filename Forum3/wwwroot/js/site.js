$(function () {
    // TODO - enable shift-click and middle click to new windows
    $("[clickableLinkParent]").on("click", function (e) {
        e.stopPropagation();
        window.location.href = $(this).find("a").eq(0).attr("href");
    });

    $("#createTopicButton").on("click", function (e) {
        OpenModal($(e).data("source"));
    });
});

function OpenModal(url) {
    if (url && url.length > 0) {
        $("#modal").html("");
        $("#modal").load(url);
    }
}