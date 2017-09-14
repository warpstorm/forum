$(function () {
    var mergeFromCategoryId = "";

    $(".mergeCategory").on("click", function() {
        if (mergeFromCategoryId === "") {
            mergeFromCategoryId = $(this).attr("categoryId");
            $(this).parent(".table-row").children(".table-cell").css("background-color", "#ACD");
        } else {
            var mergeToCategoryId = $(this).attr("categoryId");

			if (mergeToCategoryId === mergeFromCategoryId)
                return;

            PostToPath("/Boards/MergeCategory", {
                "FromId": mergeFromCategoryId,
				"ToId": mergeToCategoryId
            });
        }
    });
});

$(function () {
	var mergeFromBoardId = "";

	$(".mergeBoard").on("click", function () {
		if (mergeFromBoardId === "") {
			mergeFromBoardId = $(this).attr("boardId");
			$(this).parent(".table-row").children(".table-cell").css("background-color", "#ACD");
		} else {
			var mergeToBoardId = $(this).attr("boardId");

			if (mergeToBoardId === mergeFromBoardId)
				return;

			PostToPath("/Boards/MergeBoard", {
				"FromId": mergeFromBoardId,
				"ToId": mergeToBoardId
			});
		}
	});
});