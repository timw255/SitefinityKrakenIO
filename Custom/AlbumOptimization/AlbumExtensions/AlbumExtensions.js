function optimizeCommandHandler(sender, args) {
    if (args._commandName == "optimize") {

        var dataItem = clickedDataArgs.get_dataItem();

        jQuery.ajax({
            type: "POST",
            url: sender._baseUrl + "api/Optimization/" + args._dataItem.Id,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            processdata: false,
            success: function (taskId) {
                clickedDataArgs.get_dataItem().ScheduledTaskInfo = { Id: taskId };
                addScheduledTaskToPolledList(clickedDataArgs, dataItem.Id);
            },
            error: function (jqXHR) {
                alert(Telerik.Sitefinity.JSON.parse(jqXHR.responseText).Detail);
            }
        });
    }
}

function albumsListLoaded(sender, args) {
    sender.add_itemCommand(optimizeCommandHandler);
}