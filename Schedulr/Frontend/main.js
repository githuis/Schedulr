
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

