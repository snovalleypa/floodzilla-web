var targetDeleteRecords = [];
var deleteIdList;
var targetUndeleteRecords = [];
var undeleteIdList;

function InitAdminToggleDelete(toggleId, deleteIdListName, undeleteIdListName) {

    $('.noShowDeleted').show();
    $('.showDeleted').hide();
    
    $(toggleId).change(function() {
        if ($(toggleId).is(':checked')) {
            $('.noShowDeleted').hide();
            $('.showDeleted').show();
        } else {
            $('.noShowDeleted').show();
            $('.showDeleted').hide();
        }
    });
    
    deleteIdList = $(deleteIdListName);
    undeleteIdList = $(undeleteIdListName);

    $("#btnDelete").on('click', function () {
        if (targetDeleteRecords.length == 0) {
            deleteIdList.val("");
            return;
        }
        deleteIdList.val(targetDeleteRecords);
        $("#DelConfirmModel").modal("show");
    });

    $("#btnUndelete").on('click', function () {
        if (targetUndeleteRecords.length == 0) {
            undeleteIdList.val("");
            return;
        }
        undeleteIdList.val(targetUndeleteRecords);
        $("#UndelConfirmModel").modal("show");
    });

    $("#checkAll").change(function () {

        var table = $("table").DataTable();
        var rows = table.rows({ 'search': 'applied' }).nodes();

        var checkBoxs = $('input[type="checkbox"]', rows);

        targetDeleteRecords = [];
        targetUndeleteRecords = [];

        if ($(this).is(":checked")) {
            checkBoxs.prop('checked', true);

            $.each(checkBoxs, function (index, item) {
                if ($(this).data('isdeleted')) {
                    targetUndeleteRecords.push(item.value);
                } else {
                    targetDeleteRecords.push(item.value);
                }
            });

        }
        else {
            checkBoxs.prop('checked', false);
        }
        updateDelUndelCounts(targetDeleteRecords, $("#btnDelete"), "Delete");
        updateDelUndelCounts(targetUndeleteRecords, $("#btnUndelete"), "Undelete");
    });

    function updateDelUndelCounts(records, btn, verb){
        if (records.length > 0) {
            btn.text(verb + " (" + records.length + ")");
            btn.removeClass("disabled");
        } else {
            btn.text(verb);
            btn.addClass("disabled");
        }
    }

    function updateDelUndel(cbox, records, btn, verb) {
        if (cbox.is(":checked")) {
            records.push(cbox.val());
            btn.text(verb + " (" + records.length + ")");
            btn.removeClass("disabled");
        } else {
            var indexOfRegion = records.indexOf(cbox.val());
            records.splice(indexOfRegion, 1);
            if (records.length == 0) {
                btn.text(verb);
                btn.addClass("disabled");
            }
            else {
                btn.text(verb + " (" + records.length + ")");
            }
        }
    }

    $("table>tbody").on('change', 'input[type="checkbox"]', function () {
        if ($(this).data('isdeleted')) {
            updateDelUndel($(this), targetUndeleteRecords, $("#btnUndelete"), 'Undelete');
        } else {
            updateDelUndel($(this), targetDeleteRecords, $("#btnDelete"), 'Delete');
        }
    });

}

