$.getJSON("/user", function (user) {
    $("h3").text("Welcome user.name");
});
$("#addButton").click(function () {
    $.magnificPopup.open({
        items: {
            type:'inline',
            src: $("#newtimepopup-template").html()
        }
    });
    var s = "";
    jobs.forEach(function (t) {
        s += "<option value='"+t+"'>"+t+"</option>"
    });
    $("#submittimeform").find("select").empty().append(s);
});


$("#profileButton").magnificPopup({
    items: {
        type:'inline',
        src: $("#profilepopup-template").html()
    }
});
var $sessions = $("#tbody");
function addSessionToTable(session) {
    $sessions.append(
    "<tr data-obj='"+JSON.stringify(session)+"'>" +
        "<td>"+session.Hours+"</td>" +
        "<td>"+moment(session.Start).format("L LT")+"</td>" +
        "<td>"+moment(session.End).format("L LT")+"</td>" +
        "<td>"+session.Earned+"</td>" +
        "<td><i class='fa fa-cog' aria-hidden='true'></i><i class='fa fa-times' aria-hidden='true'></td>" +
    "</tr>");
}
$("body").on("submit", "#submittimeform", function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    if (form.get("duration") === "" && form.get("end") === "")
        return;
    ajaxPost("/submittime", form, function (sess) {
        addSessionToTable(sess);
        $.magnificPopup.close();
    })
});
$("body").on("click", "#submittimeform input[type=checkbox]", function () {
    var date = $("#newtimeenddate");
    var dur = $("#newtimeduration");

    if( $(this).is(':checked'))
    {
        dur.hide();
        dur.find("input").removeProp("required");
        date.show();
        date.find("input").prop("required", "required");
    }
    else
    {
        date.hide();
        date.find("input").removeProp("required");
        dur.show();
        dur.find("input").prop("required", "required");
    }
});
$("tbody").on("click", "tr", function (ev) {
    ev.preventDefault();
    var session = JSON.parse($(this).attr("data-obj"));
    console.log(session);
});

var jobs = ["Game teaching", "Cleaning"];
function ajaxPost(url, formdata, success, fail) {
    $.ajax({url: url, data:formdata, type:"POST", casche: false, contentType: false, processData: false,
        statusCode: {200: success, 401: fail, 404:fail }});
}


function comparer(index) {
    return function(a, b) {
        var valA = getCellValue(a, index), valB = getCellValue(b, index)
        return $.isNumeric(valA) && $.isNumeric(valB) ? valA - valB : valA.localeCompare(valB)
    }
}
$('.sortable').click(function(){
    var table = $(this).parents('table').eq(0);
    var rows = table.find('tr:gt(0)').toArray().sort(comparer($(this).index()));
    this.asc = !this.asc;
    if (!this.asc){rows = rows.reverse()}
    for (var i = 0; i < rows.length; i++){table.append(rows[i])}
});
