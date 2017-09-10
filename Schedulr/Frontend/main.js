
// $("#addbutton").click(function () {
//
//     console.log("hellooo!");
//
//
//     $.magnificPopup.open( {type:'inline', src: $("#newtimepopup-template").html()})
// });

$("#addButton").magnificPopup({
    items: {
   type:'inline',
   src: $("#newtimepopup-template").html()}
});

$("#profileButton").click(function () {
    console.log("Redirecting to login");
    window.location.href = "/login";
    
});


$("body").on("submit", "#submittimeform", function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    ajaxPost("/submittime", form, function (e) {
        console.log(e);
        $.magnificPopup.close();
    })
});

function ajaxPost(url, formdata, success, fail) {
    $.ajax({url: url, data:formdata, type:"POST", casche: false, contentType: false, processData: false,
        statusCode: {200: success, 401: fail, 404:fail }});
    
}

