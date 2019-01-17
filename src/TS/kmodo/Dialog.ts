
namespace kmodo {

    export interface SimpleDialogOptions {
        title?: string;
        kind?: string;
    }
    
    interface InternalDialogOptions {
        title: string;
        kind: string;
        ok: boolean;
        cancel: boolean;
    }

    export function openErrorDialog(message: string, options?: SimpleDialogOptions): Promise<boolean> {
        options = options || {};
        var opts: InternalDialogOptions = {
            title: options.title || "Fehler",
            kind: options.kind || "error",
            ok: true,
            cancel: false
        };

        var result = cmodo._tryCleanupHtml(message);
        if (result.ok)
            message = result.html;

        return _openMessageDialogCore(message, opts);
    }

    export function openWarningDialog(message: string): Promise<boolean> {
        var opts: InternalDialogOptions = {
            title: "Warnung",
            kind: "warning",
            ok: true,
            cancel: false
        };

        return _openMessageDialogCore(message, opts);
    }

    export function openInfoDialog(message: string, options?: SimpleDialogOptions): Promise<boolean> {
        options = options || {};
        var opts: InternalDialogOptions = {
            title: options.title || "Info",
            kind: options.kind || "info",
            ok: true,
            cancel: false
        };

        return _openMessageDialogCore(message, opts);
    }

    export function openInstructionDialog(message: string, options?: SimpleDialogOptions): Promise<boolean> {
        options = options || {};
        var opts: InternalDialogOptions = {
            title: options.title || "Info",
            kind: options.kind || "warning",
            ok: true,
            cancel: false
        };

        return _openMessageDialogCore(message, opts);
    }

    export function openDeletionConfirmationDialog(message: string, options?: SimpleDialogOptions): Promise<boolean> {
        options = options || {};
        var opts: InternalDialogOptions = {
            title: options.title || "Löschen bestätigen",
            kind: options.kind || "warning",
            ok: true,
            cancel: true
        };

        return _openMessageDialogCore(message, opts);
    }

    export function openConfirmationDialog(message: string, options?: SimpleDialogOptions): Promise<boolean> {
        options = options || {};
        var opts: InternalDialogOptions = {
            title: options.title || "Bestätigen",
            kind: options.kind || "",
            ok: true,
            cancel: true
        };

        return _openMessageDialogCore(message, opts);
    }

    function _openMessageDialogCore(message: string, options: InternalDialogOptions): Promise<boolean> {

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
                style += "background-color:lightgoldenrodyellow;font-weight:bold;";
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
                    maxHeight: 500,
                    close: function (e) {
                        // Resolve promise.
                        resolve(dialogResult);
                    },
                    deactivate: function (e) {
                        e.sender.destroy();
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

            wnd.wrapper.find("button.confirmation-ok").text("OK").on("click", (e) => {
                dialogResult = true;
                wnd.close();
                return false;
            });

            if (options.cancel)
                wnd.wrapper.find("button.confirmation-cancel").text("Abbrechen").on("click", (e) => {
                    dialogResult = false;
                    wnd.close();
                    return false;
                });

            wnd.center().open();
            // Promise will be resolved when the window closes.
        });
    }

    // KABU TODO: Only used in OwnedJobList yet.
    export function showFileDownloadDialog(info): void {
        var args = new cmodo.DialogArgs('7a516302-3fbc-48ed-91fc-422351c10b9f');
        args.item = info;
        cmodo.dialogArgs.add(args);

        var wnd = $('<div/>')
            .kendoWindow({
                animation: kmodo.getDefaultDialogWindowAnimation(),
                modal: true,
                visible: false,
                title: "Datei herunterladen",
                width: 500,
                minWidth: 500,
                height: 180, minHeight: 180,
                deactivate: (e) => { e.sender.destroy(); }
            }).data('kendoWindow');
        kmodo.setModalWindowBehavior(wnd);
        wnd.center().open();
        wnd.refresh({ url: "/Mos/FileDownloadDialog", cache: true });
    }



    export function createUploadComponent($elem: JQuery, text: string, url: string, success: Function): kendo.ui.Upload {

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
            upload: function (e) {
                kmodo.useHeaderRequestVerificationToken(e.XMLHttpRequest); 
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
                cmodo.jQueryRemoveContentTextNodes($status);

                $status.append("Fehler");
                $status.css("color", "red");
            }
            //complete: function () { }
        }).data("kendoUpload") as kendo.ui.Upload;

        upload.wrapper.find("em").remove();

        return upload;
    }

    // KABU TODO: REMOVE? Not used.
    /*
    class LoadProgressManager {
        $view: JQuery;
        isBusy: boolean;
        taskCount: number;

        constructor($view: JQuery) {
            this.$view = $view;
            this.isBusy = false;
            this.taskCount = 0;
        }

        start(): LoadProgressManager {
            this.taskCount++;

            if (this.isBusy)
                return;

            kmodo.progress(true, this.$view);
            this.isBusy = true;

            return this;
        }

        end(): void {
            this.taskCount--;

            if (this.taskCount <= 0) {
                kmodo.progress(false, this.$view);
                this.isBusy = false;
            }
        }
    }
    */

    // KABU TODO: REMOVE? Not used
    /*
    function clearDropDownList(component: kendo.ui.DropDownList): void {
        component.text("");
        component.element.val(null);
        component.selectedIndex = -1;
        component._oldIndex = 0;
    }
    */

    // KABU TODO: REMOVE? Not used
    /*
    function findDialogContainer($context) {
        if ($context && $context.length) {
            var $container = $context.closest(".dialog-container");
            if ($container.length)
                return $container;
        }
        return $(window.document.body);
    }
    */

    // KABU TODO: REMOVE? Not used
    /*
    function getPageDialogContainer() {
        if (!window.cmodo.run.$pageDialogContainer)
            window.cmodo.run.$pageDialogContainer = $(window.document.body).find("#page-dialog-container").first();
        return window.cmodo.run.$pageDialogContainer;
    }
    */
}