// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {

    var uploadConfirmed = false;

    $("#UploadListinoForm").on("submit", function (e) {

        if (!uploadConfirmed) {
            e.preventDefault();
            $.confirm({
                title: 'Confirm!',
                content: 'Sei sicuro di voler ricaricare tutto il listone? Questo resetterà tutta l asta',
                buttons: {
                    No: function () {
                        return;
                    },
                    Yes: function () {
                        uploadConfirmed = true;
                        $("#UploadListinoForm").submit();
                    }
                }
            });
        }
    });


  
});