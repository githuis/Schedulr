var jobs = [];
var $sessions = $("#sessionTable");
var $body = $("body");

function getCellValue(row, index){ return $(row).children('td').eq(index).html() }

function addSessionToTable(session) {
    return "<tr data-id='" + session.Id + "'>" +
        "<td>" + session.Hours.toFixed(1).replace(".0", "") + "</td>" +
        "<td>" + moment(session.StartDate).format("YYYY-MM-DD H:mm") + "</td>" +
        "<td>" + moment(session.EndDate).format("YYYY-MM-DD H:mm") + "</td>" +
        "<td>" + session.Earned.toFixed(1).replace(".0", "")+ "</td>" +
        "<td><i class='fa fa-cog' aria-hidden='true'></i><i id='delete-session' class='fa fa-times' aria-hidden='true'></i></td>" +
        "</tr>";
}

function formatSessionText(session) {
    return moment(session.StartDate).format("YYYY-MM-DD H:mm") + "\t" + moment(session.EndDate).format("YYYY-MM-DD H:mm") + "\tHours: " + session.Hours.toFixed(1).replace(".0", "") + "\r\n";
}

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
        var valA = getCellValue(a, index), valB = getCellValue(b, index);
        return $.isNumeric(valA) && $.isNumeric(valB) ? valA - valB : valA.localeCompare(valB)
    }
}

$.getJSON("/user", function (user) {
    $("h3").text("Welcome " + user.Username);
    jobs = [];
    var s = "";
    user.Jobs.forEach(function (job) {
        jobs.push(job.Name);
        s += "<option value='" + job.Name + "'>" + job.Name + "</option>";

    });


    $("#search-job").append(s);

    $("#search-form").submit(function (ev) {
        ev.preventDefault();
        var fd = new FormData($(this)[0]);
        var query = $(this).serialize();
        $.getJSON("/sessions?" + query, function (sessions) {
            var saveText = "";
            var emailHtml = "";
            var s = "";
            var total = 0;
            sessions.forEach(function (session) {
                s += addSessionToTable(session);
                saveText += formatSessionText(session);
                emailHtml += "<tr>" +
                    "<td style='padding: 3px 15px;'>"+moment(session.StartDate).format("YYYY-MM-DD H:mm")+"</td>" +
                    "<td style='padding: 3px 15px;'>"+moment(session.EndDate).format("YYYY-MM-DD H:mm")+"</td>" +
                    "<td style='padding: 3px 15px; text-align:center;'>"+session.Hours.toFixed(1) +"</td>" +
                    "</tr>";
                total += session.Hours;
            });
            saveText += "\t\t\t\t\t\t\t\t\t\tTotal: " + total.toFixed(1);
            $sessions.append(s);
            $("#totalHours").text("Total hours: " + total.toFixed(1));
            $("#downloadResults").attr("href", "data:text/plain;charset=utf-8," + encodeURIComponent(saveText));
            $("#downloadResults").attr("download", fd.get("job ") + ".txt");
            $("#sendResults").attr("href", "mailto:?body="+encodeURIComponent("<table>" +
                "<thead>" +
                    "<tr>" +
                        "<th style='padding: 3px 15px;'>Start time</th>" +
                        "<th style='padding: 3px 15px;'>End time</th>" +
                        "<th style='padding: 3px 15px;'>Hours</th>" +
                    "</tr>" +
                "</thead>" +
                "<tfoot>" +
                    "<tr>" +
                        "<td style='padding: 3px 15px;'></td>" +
                        "<td style='padding: 3px 15px;'></td>" +
                        "<td style='padding: 3px 15px;'>Total: "+total.toFixed(1)+"</td>" +
                    "</tr>" +
                "</tfoot>" +
                "<tbody>" + emailHtml + "</tbody>" +

                "</table>"));
        });

    }).submit();

    //Enter today and 1 month ago into search form
    $("#search-form").find("#time-form-start").val(moment().subtract(1, 'months').format("YYYY-MM-DDT00:00"));
    $("#search-form").find("#time-form-end").val(moment().format("YYYY-MM-DDT23:59"));

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
    var time = moment().format("YYYY-MM-DDTHH:mm");
    $("#submittimeform").find("#start-time-input").val(time);
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
    var job = form.get("name");
    var arr = [];
    $("#newJobRules").find("tr").each(function (tr) {
        arr.push(JSON.parse($(this).attr("data-obj")))
    });
    form.append("rules", JSON.stringify(arr));
    ajaxPost("/submitnewjob", form, function (sess) {
        jobs.push(job);
        $.magnificPopup.close();
        $("#search-job").append("<option value='" + job + "'>" + job + "</option>");
    });

    
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
        dur.find("input").removeAttr("required");
        date.show();
        date.find("input").attr("required", "required");
    }
    else {
        date.hide();
        date.find("input").removeAttr("required");
        dur.show();
        dur.find("input").attr("required", "required");
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

$body.on("click", "#manageJobs", function () {
    $.magnificPopup.open({
        items: {
            type: 'inline',
            src: $("#managejobs-template").html()
        }
    });

    var str = "";
    jobs.forEach(function (t) { 
        str += "<li>" + t +
            "<i class='fa fa-times' style='float:right;' aria-hidden='true'></i>" +
            "<i class='fa fa-cog' style='float:right;' aria-hidden='true'></i></li>";
    });
    $('#manage-jobs-list').append(str);
});

$body.on("click", "#manage-jobs-list li .fa-times", function () {
    var $this = $(this);
    var job = $this.parent().text();
    var fd = new FormData();
    fd.append("job", job);
    
    ajaxPost("/deletejob", fd, function ()
    {
        $this.parent().remove();
    }, function () {
       console.log("Could not delete job: " + job);
    });
});

$sessions.on("click", "tr", function (ev) {
    ev.preventDefault();
    //var session = JSON.parse($(this)[0]);
    //console.log(session);
});

$body.on("click", "#delete-session", function () {
    var row = $(this).parent().parent();
    var sessionObject = row.attr("data-id");

    var form = new FormData();
    form.append("deleteTarget", sessionObject);
    row.remove();

    ajaxPost("/deletesession", form, function () { console.log("Succesfully deleted"); }, function () { console.log("Failed to delete");});

});

$('.sortable').click(function () {
    var table = $(this).parents('table').eq(0);
    var rows = table.find('tr:gt(0)').toArray().sort(comparer($(this).index()));
    this.asc = !this.asc;
    if (!this.asc) { rows = rows.reverse() }
    for (var i = 0; i < rows.length; i++) { table.append(rows[i]) }
});


