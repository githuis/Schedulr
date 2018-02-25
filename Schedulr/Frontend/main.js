"use strict";

let jobs = [];
let $sessions = $("#sessionTable");
let $body = $("body");
let manageJob;

function getCellValue(row, index) {
    return $(row).children('td').eq(index).html()
}

function addSessionToTable(session) {
    return "<tr data-id='" + session.Id + "'>" +
        "<td>" + session.Hours.toFixed(1).replace(".0", "") + "</td>" +
        "<td>" + moment(session.StartDate).format("YYYY-MM-DD HH:mm") + "</td>" +
        "<td>" + moment(session.EndDate).format("YYYY-MM-DD HH:mm") + "</td>" +
        "<td>" + session.Earned.toFixed(1).replace(".0", "") + "</td>" +
        "<td><i id='edit-session' class='fa fa-cog' aria-hidden='true'></i><i id='delete-session' class='fa fa-times' aria-hidden='true'></i></td>" +
        "</tr>";
}

function formatSessionText(session) {
    return moment(session.StartDate).format("YYYY-MM-DD HH:mm") + "\t" + moment(session.EndDate).format("YYYY-MM-DD HH:mm") + "\tHours: " + session.Hours.toFixed(1).replace(".0", "") + "\r\n";
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

function getWage(jobName, cb) {
    $.getJSON("/getwage?job=" + encodeURIComponent(jobName), data => {
        //console.log(data);
        cb(data.Hourly);
    })
}

function comparer(index) {
    return function (a, b) {
        let valA = getCellValue(a, index), valB = getCellValue(b, index);
        return $.isNumeric(valA) && $.isNumeric(valB) ? valA - valB : valA.localeCompare(valB)
    }
}

$.getJSON("/user", function (user) {
    $("h3").text("Welcome " + user.Username);
    jobs = [];
    let s = "";
    //console.log(user.Jobs);
    user.Jobs.forEach(function (job) {

        jobs.push(job);
        s += "<option value='" + job.Name + "'>" + job.Name + "</option>";

    });


    $("#search-job").append(s);

    $("#search-form").submit(function (ev) {
        ev.preventDefault();
        let job = $(this).find("select").val();
        let query = $(this).serialize();
        $.getJSON("/sessions?" + query, function (sessions) {
            let saveText = "";
            let emailHtml = "";
            let s = "";
            let total = 0;
            sessions.forEach(function (session) {
                s += addSessionToTable(session);
                total += session.Hours;
                saveText += formatSessionText(session);
                emailHtml += "<tr>" +
                    "<td style='padding: 3px 15px;'>" + moment(session.StartDate).format("YYYY-MM-DD HH:mm") + "</td>" +
                    "<td style='padding: 3px 15px;'>" + moment(session.EndDate).format("YYYY-MM-DD HH:mm") + "</td>" +
                    "<td style='padding: 3px 15px; text-align:center;'>" + session.Hours.toFixed(1) + "</td>" +
                    "</tr>";
            });
            saveText += "\t\t\t\t\t\tTotal: " + total.toFixed(1);
            $sessions.empty().append(s);

            $("#totalHours").text("Total hours: " + total.toFixed(1));
            $("#downloadResults").attr("href", "data:text/plain;charset=utf-8," + encodeURIComponent(saveText));
            let startMonth = moment($("#time-form-start").val()).format('MMMM');
            let endMonth = moment($("#time-form-end").val()).format('MMMM');
            let dl = job;
            if (startMonth && startMonth != "Invalid date")
                dl += " - " + startMonth;
            if (endMonth && endMonth != "Invalid date")
                dl += " - " + endMonth;
            dl += ".txt";

            $("#downloadResults").attr("download", dl);

            $("#sendResults").attr("href", "mailto:?body=" + encodeURIComponent("<table>" +
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
                "<td style='padding: 3px 15px;'>Total: " + total.toFixed(1) + "</td>" +
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
    let s = "";
    jobs.forEach(function (t) {
        s += "<option value='" + t.Name + "'>" + t.Name + "</option>"
    });
    $("#submittimeform").find("select").empty().append(s);
    let time = moment().format("YYYY-MM-DDTHH:mm");
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
    let form = new FormData($(this)[0]);
    if (form.get("duration") === "" && form.get("end") === "")
        return;
    ajaxPost("/submittime", form, function (sess) {
        $sessions.append(addSessionToTable(sess));
        $.magnificPopup.close();
    })
});

$body.on("submit", "#addJobForm", function (ev) {
    ev.preventDefault();
    let form = new FormData($(this)[0]);
    let job = form.get("name");
    let arr = [];
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
    let form = new FormData($(this)[0]);

    let j = 0;

    $("#new-rule-days-checkbox input").each(function (i, ele) {
        if (ele.checked)
            j += (1 << i);
    });

    let s = "<tr data-obj='" + JSON.stringify({
            Start: form.get("start"),
            End: form.get("end"),
            Type: form.get("type"),
            Value: form.get("value"),
            Days: j
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
    let date = $("#newtimeenddate");
    let dur = $("#newtimeduration");

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

    let str = "";
    jobs.forEach(function (t) {
        str += "<li>" + t.Name +
            "<i class='fa fa-times' style='float:right;' aria-hidden='true'></i>" +
            "<i class='fa fa-cog fa-dist' style='float:right;' aria-hidden='true'></i></li>";
    });
    $('#manage-jobs-list').append(str);
});

$body.on("submit", "#submitmanagedjobform", ev => {
    ev.preventDefault();
    let form = new FormData();

    let foundjob = $.grep(jobs, function(e){ return e.Name === manageJob; });

    if (foundjob.length === 0)
    {
        console.log("Failed to update data for job: " + manageJob);
        return;
    } else if (foundjob.length === 1)
    {

        let title = $("#managejobpopup").find("#manage-job-title").val();
        let wage = $("#managejobpopup").find("#manage-job-wage").val();

        form.append("jobid", foundjob[0].Id);
        form.append("title", title);
        form.append("wage", wage);


        console.log(title + " - " + wage);

        ajaxPost("/submitmanagedjobform", form, function () { //succ

            $.magnificPopup.open({
                items: {
                    type: 'inline',
                    src: $("#done-manage-job-template").html()
                }
            });

        }, function () { //fail
            console.log("Failed to update data for job: " + manageJob);
        });

    } else {
        console.log("fuck man, multiple jobs with the same name");
    }

    manageJob = "";
});

$body.on("click", "#manage-jobs-list li .fa-times", function () {
    let $this = $(this);
    let job = $this.parent().text();
    let fd = new FormData();
    fd.append("job", job);

    ajaxPost("/deletejob", fd, function () {
        $this.parent().remove();
        $("#search-job option[value=" + job + "]").remove();

    }, function () {
        console.log("Could not delete job: " + job);
    });
});

$body.on("click", "#manage-jobs-list li .fa-cog", function () {
    let $this = $(this);
    let job = $this.parent().text();
    let fd = new FormData();

    manageJob = job;

    $.magnificPopup.open({
        items: {
            type: 'inline',
            src: $("#manage-specific-job-template").html()
        }
    });

    $("#managejobpopup .popup-title").text(job);
    $("#managejobpopup").find("#manage-job-title").val(job);
    getWage(job, v => $("#managejobpopup").find("#manage-job-wage").val(v));

});

$sessions.on("click", "tr", function (ev) {
    ev.preventDefault();
    //let session = JSON.parse($(this)[0]);
    //console.log(session);
});

$body.on("click", "#delete-session", function () {
    let row = $(this).parent().parent();
    let sessionObject = row.attr("data-id");

    let form = new FormData();
    form.append("deleteTarget", sessionObject);
    row.remove();

    ajaxPost("/deletesession", form, function () {
        console.log("Succesfully deleted");
    }, function () {
        console.log("Failed to delete");
    });

});

$body.on("click", "#edit-session", function () {
    let row = $(this).parent().parent();
    let sessionObject = row.attr("data-id");

    console.log(row);
    console.log(row.cells[0]);
    console.log(sessionObject);
});

$('.sortable').click(function () {
    let table = $(this).parents('table').eq(0);
    let rows = table.find('tr:gt(0)').toArray().sort(comparer($(this).index()));
    this.asc = !this.asc;
    if (!this.asc) {
        rows = rows.reverse()
    }
    for (let i = 0; i < rows.length; i++) {
        table.append(rows[i])
    }
});

$body.on("click", "#day-preset-all", function () {
    $("#new-rule-days-checkbox input").prop("checked", true);
    console.log("yayayasdyfsjdflkjsdf");
});

$body.on("click", "#day-preset-wd", function () {
    let r = $("#new-rule-days-checkbox input");
    r.slice(0, 6).prop("checked", true);
    r.slice(5).prop("checked", false);
});

$body.on("click", "#day-preset-we", function () {

    let r = $("#new-rule-days-checkbox input");

    r.slice(0, 5).prop("checked", false);
    r.slice(5).prop("checked", true);
});
