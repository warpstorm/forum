$(function () {
    $(".reply-button").on("click.show-reply-form", ShowReplyForm);
});

function ShowReplyForm() {
    $(".reply-form").not(".hidden").addClass("hidden");
    $(".reply-button").off("click.show-reply-form");
    $(".reply-button").off("click.hide-reply-form");
    $(".reply-button").on("click.show-reply-form", ShowReplyForm);
    $(this).off("click.show-reply-form");
    $(this).parents("section").find(".reply-form").removeClass("hidden");
    $(this).on("click.hide-reply-form", HideReplyForm);
}

function HideReplyForm() {
    $(this).off("click.hide-reply-form");
    $(this).parents("section").find(".reply-form").addClass("hidden");
    $(this).on("click.show-reply-form", ShowReplyForm);
}