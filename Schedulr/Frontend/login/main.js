$("form").submit(function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    $.ajax({
        url: "/login",
        data: form,
        cache: false,
        contentType: false,
        processData: false,
        statusCode: {
            200: function(){
                location.replace("/")
            },
            401: function () {
                $(".status").text("Invalid credentials")
            },
            404: function () {
                $(".status").text("Could not connect to server")
            }
        }
    })
});