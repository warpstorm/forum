$(function () {
    $(".replyButton").on("click.showReplyForm", ShowReplyForm);
});

function ShowReplyForm() {
    $(".replyForm").not(".hidden").addClass("hidden");
    $(".replyButton").off("click.showReplyForm");
    $(".replyButton").off("click.hideReplyForm");
    $(".replyButton").on("click.showReplyForm", ShowReplyForm);
    $(this).off("click.showReplyForm");
    $(this).parents("section").find(".replyForm").removeClass("hidden");
    $(this).on("click.hideReplyForm", HideReplyForm);
}

function HideReplyForm() {
    $(this).off("click.hideReplyForm");
    $(this).parents("section").find(".replyForm").addClass("hidden");
    $(this).on("click.showReplyForm", ShowReplyForm);
}