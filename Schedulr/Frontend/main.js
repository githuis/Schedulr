var jobs = [];
var $sessions = $("#sessionTable");
var $body = $("body");

$.getJSON("/user", function (user) {
    $("h3").text("Welcome " + user.Username);
    jobs = [];
    var s = "";
    var searchjob = $("#search-job");
    user.Jobs.forEach(function (job) {
        jobs.push(job.Name);
        s += "<option value='" + job.Name + "'>" + job.Name + "</option>";

    })

    
    $("#search-job").append(s);
    
    $("#search-form").submit(function (ev) {
        ev.preventDefault();
        var query = $(this).serialize();
        $.getJSON("/sessions?" + query, function (sessions) {
            //console.log(query);
            var s = "";
            sessions.forEach(function (session) {
                s += addSessionToTable(session);
            })
            $sessions.append(s);
        });

    }).submit();

    //Enter today and 1 month ago into search form
    $("#search-form").find("#time-form-start").val( moment().subtract(1, 'months').format("YYYY-MM-DDTHH:mm"));
    $("#search-form").find("#time-form-end").val(moment().format("YYYY-MM-DDTHH:mm"));
    
}).fail(function (x) {
    window.location.replace("/login");
});


$("#addButton").click(function () {
    $.magnificPopup.open({
        items: {
            type: 'inline',
            src: $("#newtimepopup-template").html()
        }
    });
    var s = "";
    jobs.forEach(function (t) {
        s += "<option value='" + t + "'>" + t+ "</option>"
    });
    $("#submittimeform").find("select").empty().append(s);
    var x =moment().format("YYYY-MM-DDTHH:mm");
    $("#submittimeform").find("#start-time-input").val(x);
    //$("#submittimeform").find("#start-time-input").val(moment().format());
});

$("#profileButton").magnificPopup({
    items: {
        type: 'inline',
        src: $("#profilepopup-template").html()
    }
});

$body.on("submit", "#submittimeform", function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    if (form.get("duration") === "" && form.get("end") === "")
        return;
    ajaxPost("/submittime", form, function (sess) {
        $sessions.append(addSessionToTable(sess));
        $.magnificPopup.close();
    })
});

$body.on("submit", "#addJobForm", function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    var arr = [];
    $("#newJobRules").find("tr").each(function (tr) {
        arr.push(JSON.parse($(this).attr("data-obj")))
    });
    form.append("rules", JSON.stringify(arr));
    ajaxPost("/submitnewjob", form, function (sess) {
        jobs.push(form.get("job"));
        $.magnificPopup.close();
    })
});

$body.on("submit", "#addJobRuleForm", function (ev) {
    ev.preventDefault();
    var form = new FormData($(this)[0]);
    var s = "<tr data-obj='" + JSON.stringify({
        Start: form.get("start"),
        End: form.get("end"),
        Type: form.get("type"),
        Value: form.get("value")
    }) + "'>" +
        "<td>" + form.get("start") + "</td>" +
        "<td>" + form.get("end") + "</td>" +
        "<td>" + form.get("type") + "</td>" +
        "<td>" + form.get("value") + "</td>" +
        "<td><i class='fa fa-times' aria-hidden='true'></i></td>" +
        "</tr>";
    $("#newJobRules").append(s);
    $(this)[0].reset();
});
$body.on("click", "#addJobForm .fa-times", function () {
    $(this).parent().parent().remove();
});

$body.on("click", "#submittimeform input[type=checkbox]", function () {
    var date = $("#newtimeenddate");
    var dur = $("#newtimeduration");

    if ($(this).is(':checked')) {
        dur.hide();
        dur.find("input").removeProp("required");
        date.show();
        date.find("input").prop("required", "required");
    }
    else {
        date.hide();
        date.find("input").removeProp("required");
        dur.show();
        dur.find("input").prop("required", "required");
    }
});

$body.on("click", "#addJob", function () {
    $.magnificPopup.open({
        items: {
            type: 'inline',
            src: $("#newjobpopup-template").html()
        }
    });
});

$sessions.on("click", "tr", function (ev) {
    ev.preventDefault();
    //var session = JSON.parse($(this)[0]);
    //console.log(session);
});

function addSessionToTable(session) {
    return "<tr data-id='" + session.Id + "'>" +
                "<td>" + session.Hours + "</td>" +
                "<td>" + moment(session.StartDate).format("YYYY-MM-DD H:mm") + "</td>" +
                "<td>" + moment(session.EndDate).format("YYYY-MM-DD H:mm") + "</td>" +
                "<td>" + session.Earned + "</td>" +
                "<td><i class='fa fa-cog' aria-hidden='true'></i><i id='delete-session' class='fa fa-times' aria-hidden='true'></i></td>" +
            "</tr>";
}

$body.on("click", "#delete-session", function () {
    var row = $(this).parent().parent();
    var sessionObject = row.attr("data-id");

    var form = new FormData();
    form.append("deleteTarget", sessionObject);
    row.remove();

    ajaxPost("/deletesession", form, function () { console.log("Succesfully deleted"); }, function () { console.log("Failed to delete");});

});

function ajaxPost(url, formdata, success, fail) {
    $.ajax({
        url: url, data: formdata, type: "POST", casche: false, contentType: false, processData: false,
        statusCode: {
            200: success,
            401: function (data) {
                window.location.replace("/login");
            },
            404: fail
        }
    });
}

function comparer(index) {
    return function (a, b) {
        var valA = getCellValue(a, index), valB = getCellValue(b, index)
        return $.isNumeric(valA) && $.isNumeric(valB) ? valA - valB : valA.localeCompare(valB)
    }
}
$('.sortable').click(function () {
    var table = $(this).parents('table').eq(0);
    var rows = table.find('tr:gt(0)').toArray().sort(comparer($(this).index()));
    this.asc = !this.asc;
    if (!this.asc) { rows = rows.reverse() }
    for (var i = 0; i < rows.length; i++) { table.append(rows[i]) }
});
