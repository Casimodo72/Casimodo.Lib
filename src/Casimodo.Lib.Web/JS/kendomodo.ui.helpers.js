﻿
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui.progress = function (isbusy, $el) {
            if (!$el || !$el.length)
                $el = $(window.document.body);

            kendo.ui.progress($el, isbusy);
        };

        ui.onPageNaviEvent = function (e) {
            e.preventDefault();
            e.stopPropagation();

            var $el = $(this);
            var part = $el.data("navi-part");
            var id = $el.data("navi-id");

            kendomodo.ui.navigate(part, id);

            return false;
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

        // KABU TODO: OBSOLETE: REMOVE when not used anymore (only used in onwed.job.list.js).
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

            var wnd = $("<div/>").kendoWindow({
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

        ui.openInfoDialog = function (message, options) {
            options = options || {};
            options.title = options.title || "Info";
            options.kind = options.kind || "info";
            options.ok = true;
            options.cancel = false;

            return kendomodo.ui._openMessageDialogCore(message, options);
        };

        ui.openErrorDialog = function (message, options) {
            options = options || {};
            options.title = options.title || "Fehler";
            options.kind = "error";
            options.ok = true;
            options.cancel = false;

            return kendomodo.ui._openMessageDialogCore(message, options);
        };

        ui.openDeletionConfirmationDialog = function (message, options) {
            options = options || {};
            options.title = options.title || "Löschen bestätigen";
            options.kind = "warning";
            options.ok = true;
            options.cancel = true;

            return kendomodo.ui._openMessageDialogCore(message, options);
        };

        ui.openConfirmationDialog = function (message, options) {
            options = options || {};
            options.title = options.title || "Bestätigen";
            options.kind = options.kind || "";
            options.ok = true;
            options.cancel = true;

            return kendomodo.ui._openMessageDialogCore(message, options);
        };

        ui._openMessageDialogCore = function (message, options) {

            return new Promise(function (resolve, reject) {

                var dialogResult = false;

                // If this is just an info confirmation dialog then the dialog-result is always true.
                if (!options.cancel)
                    dialogResult = true;

                var kind = options.kind;
                var style = "";

                if (kind === 'info')
                    style += "background-color:skyblue;";
                else if (kind === 'warning')
                    style += "background-color:lightgoldenrodyellow;";
                else if (kind === 'error')
                    style += "background-color:orange;font-weight:bold;";

                var wnd = $('<div/>')
                    .kendoWindow({
                        title: options.title,
                        modal: true,
                        visible: false,
                        resizable: false,
                        minWidth: 500,
                        maxWidth: 1000,
                        close: function (e) {
                            // Resolve promise.
                            resolve(dialogResult);
                        },
                        deactivate: function (e) {
                            this.destroy();
                        }
                    }).data('kendoWindow');

                message = message.replace(/\n/g, "<br/>");

                var content = "<div class='confirmation-dialog-content'>" +
                    "<div class='confirmation-dialog-message'" +
                     (style !== "" ? " style='" + style + "'" : "") +
                    ">" +
                    message +
                    "</div><hr/><div class='confirmation-dialog-actions'>";

                if (options.ok)
                    content += "<button class='k-button confirmation-ok'></button>";

                if (options.cancel)
                    content += "<button class='k-button confirmation-cancel'></button>";

                content += "</div></div>";

                wnd.content(content);

                wnd.wrapper.find("button.confirmation-ok").text("OK").on("click", function (e) {
                    dialogResult = true;
                    wnd.close();
                    return false;
                });

                if (options.cancel)
                    wnd.wrapper.find("button.confirmation-cancel").text("Abbrechen").on("click", function (e) {
                        dialogResult = false;
                        wnd.close();
                        return false;
                    });

                wnd.center().open();
            });
        };

        // KABU TODO: REMOVE? Not used anymore.
        ui.showModalTextDialog = function (kind, text) {
            var content = "";

            if (!kind) {
                content = "<span style='font-size:1.2em'>" + text + "</span>";
            }
            else if (kind === 'info') {
                content = "<span style='font-size:1.2em;color:blue'>" + text + "</span>";
            }
            else if (kind === 'warning') {
                content = "<span style='font-size:1.2em;color:orange'>" + text + "</span>";
            }
            else if (kind === 'error') {
                content = "<span style='font-size:1.2em;color:maroon'>" + text + "</span>";
            }

            $('<div/>').kendoWindow({
                modal: true,
                width: 500,
                deactivate: function () {
                    this.destroy();
                }
            })
                .data('kendoWindow')
                .content(content)
                .center()
                .open();
        };

        ui.LoadProgressManager = (function () {
            var constructor = function ($view) {
                this.$view = $view;
                this.isBusy = false;
                this.taskCount = 0;
            };

            var fn = constructor.prototype;

            fn.start = function () {
                this.taskCount++;

                if (this.isBusy)
                    return;

                kendomodo.ui.progress(true, this.$view);
                this.isBusy = true;

                return this;
            };

            fn.end = function () {
                this.taskCount--;

                if (this.taskCount <= 0) {
                    kendomodo.ui.progress(false, this.$view);
                    this.isBusy = false;
                }
            };

            return constructor;
        })();

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));