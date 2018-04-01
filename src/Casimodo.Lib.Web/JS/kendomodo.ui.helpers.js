
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui.progress = function (isbusy, $el) {
            if (!$el || !$el.length)
                $el = $(window.document.body);

            kendo.ui.progress($el, isbusy);
        };

        ui.showFileDownloadDialog = function (info, $context) {
            var args = new casimodo.ui.DialogArgs('7a516302-3fbc-48ed-91fc-422351c10b9f');
            args.item = info;
            casimodo.ui.dialogArgs.add(args);

            var wnd = $('<div/>')
                //.appendTo(casimodo.ui.findDialogContainer($context))
                .kendoWindow({
                animation: kendomodo.getDefaultDialogWindowAnimation(),
                modal: true,
                visible: false,
                title: "Datei herunterladen",
                width: 500,
                minWidth: 500,
                height: 180, minHeight: 180,
                deactivate: function () { this.destroy(); }
            }).data('kendoWindow');
            kendomodo.setModalWindowBehavior(wnd);
            wnd.center().open();
            wnd.refresh({ url: "/Mos/FileDownloadDialog", cache: true });
        };

        ui.executeDialogAction = function ($obsolete, title, url, dialogId, options, windowOptions, callback) {

            var args = new casimodo.ui.DialogArgs(dialogId, options.value || null);

            if (options.values)
                args.values = options.values;

            if (options.filters)
                args.filters = options.filters;

            casimodo.ui.dialogArgs.add(args);

            var ownd = windowOptions || {};
            ownd.width = ownd.width || null;
            ownd.minWidth = ownd.minWidth || null;
            ownd.maxWidth = ownd.maxWidth || null;
            ownd.height = ownd.height || null;
            ownd.minHeight = ownd.minHeight || null;
            ownd.maxHeight = ownd.maxHeight || null;

            var wnd = $("<div/>")
                //.appendTo(casimodo.ui.findDialogContainer())
                .kendoWindow({
                animation: kendomodo.getDefaultDialogWindowAnimation(),
                modal: true,
                title: title,
                width: ownd.width,
                minWidth: ownd.minWidth,
                maxWidth: ownd.maxWidth,
                height: ownd.height,
                minHeight: ownd.minHeight,
                maxHeight: ownd.maxHeight,
                close: function (e) {
                    if (!callback) return;
                    callback(args);
                },
                deactivate: function () {
                    this.destroy();
                }
            }).data('kendoWindow');
            kendomodo.setModalWindowBehavior(wnd);
            wnd.center().open();
            wnd.refresh({ url: url, cache: true });
        };

        ui.createUploadComponent = function ($elem, text, url, success) {

            var upload = $elem.kendoUpload({
                localization: {
                    select: text
                },
                showFileList: false,
                multiple: false,
                async: {
                    saveUrl: url,
                    autoUpload: true
                },
                success: function (e) {
                    // Hide the irritating warning icon.
                    //$upload.find(".k-icon.k-warning").hide();

                    // Refresh the whole view.
                    success();
                },
                error: function (e) {
                    var $status = e.sender.wrapper.find(".k-upload-status");

                    // Remove any messages added by Kendo from the DOM.
                    casimodo.jQuery.removeContentTextNodes($status);

                    $status.append("Fehler");
                    $status.css("color", "red");
                }
                //complete: function () { }
            }).data("kendoUpload");

            upload.wrapper.find("em").remove();

            return upload;
        };

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));