function wordCount(field) {
    var number = 0;

    // Split the value of input by
    // space to count the words
    var matches = $(field).val().split(" ");

    // Count number of words
    number = matches.filter(function (word) {
        return word.length > 0;
    }).length;

    // Final number of words

    $("#response-hint").text("(" + (100 - number) + " words remaining)");
}

$(function () {
    $("#QuestionAresponse")
        .each(function () {
            var input = "#" + this.id;

            // Count words when keyboard
            // key is released
            $(this).keyup(function () {
                wordCount(input);
            });
        });
});

$(document).ready(function () {
    $("#QuestionAresponse")
        .each(function () {
            var input = "#" + this.id;
            wordCount(input);
        });
});
