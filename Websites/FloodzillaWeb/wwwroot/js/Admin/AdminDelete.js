var targetDeleteRecords = [];
var deleteIdList;
function InitAdminDelete(deleteIdListName) {
    deleteIdList = $(deleteIdListName);

    $("#btnDelete").on('click', function () {
        if (targetDeleteRecords.length == 0) {
            deleteIdList.val("");
            return;
        }
        deleteIdList.val(targetDeleteRecords);
        $("#ConfirmModel").modal("show");
    });

    $("#checkAll").change(function () {

        var table = $("table").DataTable();
        var rows = table.rows({ 'search': 'applied' }).nodes();

        var checkBoxs = $('input[type="checkbox"]', rows);

        targetDeleteRecords = [];

        if ($(this).is(":checked")) {
            checkBoxs.prop('checked', true);

            $.each(checkBoxs, function (index, item) {
                targetDeleteRecords.push(item.value);
            });

        }
        else {
            checkBoxs.prop('checked', false);
        }
        if (targetDeleteRecords.length > 0) {
            $("#btnDelete").text("Delete (" + targetDeleteRecords.length + ")");
            $("#btnDelete").removeClass("disabled");
        }
        else {
            $("#btnDelete").text("Delete");
            $("#btnDelete").addClass("disabled");
        }

    });

    $("table>tbody").on('change', 'input[type="checkbox"]', function () {
        if ($(this).is(":checked")) {
            targetDeleteRecords.push($(this).val());
            $("#btnDelete").text("Delete (" + targetDeleteRecords.length + ")");
            $("#btnDelete").removeClass("disabled");
        }
        else {
            var indexOfRegion = targetDeleteRecords.indexOf($(this).val());
            targetDeleteRecords.splice(indexOfRegion, 1);
            if (targetDeleteRecords.length == 0) {
                $("#btnDelete").text("Delete");
                $("#btnDelete").addClass("disabled");
            }
            else {
                $("#btnDelete").text("Delete (" + targetDeleteRecords.length + ")");
            }

        }
    });

}

