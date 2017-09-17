$("form").submit(function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    if (form.get("password1") !== form.get("password2")){
        $(".status").text("Passwords do not match");
        return;
    }
    $.ajax({
        url: "/register",
        type: 'POST',
        data: form,
        cache: false,
        contentType: false,
        processData: false,
        statusCode: {
            200: function(){
                location.replace("/login")
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