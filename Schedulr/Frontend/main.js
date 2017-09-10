
$("#addButton").click(function () {

    console.log("Hellooo!");

    document.getElementById("contentbody").innerHTML = '<object type="text/html" data="AddSessionPopup.html"> </object>';

    //$.magnifiquePopup.
});

$("#profileButton").click(function () {
    window.location = "localhost:5000/login";
});
