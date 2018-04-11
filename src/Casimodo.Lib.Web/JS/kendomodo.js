"use strict";

kendo.ui.Pager.prototype.options.messages.allPages = "Alle";
kendo.ui.Pager.prototype.options.messages.display = "{0} - {1} von {2}";

var kendomodo;
(function (kendomodo) {

    kendomodo.enableContextMenuItems = function (menu, names, enabled) {
        var items = kendomodo.getContextMenuItems(menu, names);

        if (enabled)
            items.removeClass("k-hidden");
        else
            items.addClass("k-hidden");
    };

    kendomodo.getContextMenuItems = function (menu, names) {

        if (typeof names === "string")
            names = names.split(",").map(function (x) { return x.trim(); });

        var query = "";
        for (var i = 0; i < names.length; i++) {
            if (i > 0)
                query += ", ";
            query += "li[data-name='" + names[i] + "']";
        }


        return menu.element.find(query);
    };

    kendomodo.getDefaultContextMenuAnimation = function () {
        return { open: { effects: "slideIn:down" }, duration: 72 };
    };

    kendomodo.getDefaultTabControlAnimation = function () {
        return {
            open: {
                effects: "fadeIn", duration: 30
            }
        };
    };

    kendomodo._modalWindowsCount = 0;

    kendomodo.setModalWindowBehavior = function (wnd) {
        // Used for setting of opacity the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        wnd.one("open", kendomodo.onModalWindowOpening);
        wnd.one("activate", kendomodo.onModalWindowActivated);
        wnd.one("close", kendomodo.onModalWindowClosed);
    };

    kendomodo.onModalWindowOpening = function (e) {
        // Increase model window counter.
        // Used for setting of opacity the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        kendomodo._modalWindowsCount++;

        if (kendomodo._modalWindowsCount > 1) {
            $(document.body).addClass("opening-modal-window");
        }
    };

    kendomodo.onModalWindowActivated = function (e) {
        // Used for setting of opacity of the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        if (kendomodo._modalWindowsCount > 1) {
            $(document.body).removeClass("opening-modal-window");
        }
    };

    kendomodo.onModalWindowClosed = function (e) {
        // Decrease model window counter.
        // Used for setting of opacity of the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        kendomodo._modalWindowsCount--;
    };

    kendomodo.getDefaultDialogWindowAnimation = function () {
        return {
            open: {
                effects: "fadeIn",
                duration: 400
            },
            close: {
                effects: "fadeOut",
                duration: 400
            }
        };
    };

    kendomodo.findKendoWindow = function ($context) {
        var $window = $context.closest('div [data-role=window], .k-popup-edit-form');

        return $window.data('kendoWindow');
    };

    kendomodo.initDialogActions = function ($container, args) {

        var wnd = kendomodo.findKendoWindow($container);

        $container.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", function () {
            args.buildResult();
            args.isCancelled = false;
            args.isOk = true;
            wnd.close();
        });

        $container.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
            args.isCancelled = true;
            args.isOk = false;
            wnd.close();
        });
    };

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // KABU TODO: REMOVE? Not used.
    //kendomodo.onFileUploadFailed = function (e) {
    //    alert("File Error");
    //    var prop = $(e.sender.element).data('file-prop');
    //    // KABU TODO: IMPL
    //};

    // KABU TODO: REMOVE? Not used.
    //kendomodo.onFileUploadRemoving = function (e) {
    //    alert("File removing ", e.files);
    //    var prop = $(e.sender.element).data('file-prop');
    //    // KABU TODO: IMPL
    //};

    kendomodo.getColorCellTemplate = function (color) {
        // See http://www.mediaevent.de/css/transparenz.html
        if (!color || casimodo.isEmptyOrWhiteSpace(color))
            // KABU TODO: Which color to use for null?
            color = "rgba(220, 160, 140, 0.5)";

        return "<div style='width: 30px; background-color: " + color + "'>&nbsp;</div>";
    };

    // KABU TODO: REMOVE? Not used.
    //kendomodo.getColorValueCellTemplate = function (value, color) {
    //    if (typeof value === "undefined" || value === null || value === "")
    //        return "";

    //    // See http://www.mediaevent.de/css/transparenz.html
    //    if (!color || casimodo.isEmptyOrWhiteSpace(color) || color === "#ffffff")
    //        // KABU TODO: Which color to use for null?
    //        color = "black";

    //    return "<span style='color: " + color + "'>" + kendo.htmlEncode(value) + "</span>";
    //};

    // KABU TODO: REMOVE? KEEP: maybe we can use this in the future.
    kendomodo.getShowPhotoCellTemplate = function (uri) {
        return "<a class='kendomodo-button-show-image k-button' href='#' data-file-uri='" + uri + "'><span class='casimodo-icon-show-image'></span></a>";
        // return "<div data-file-uri='" + uri + "' class='kendomodo-button-show-image'>&nbsp;</div>";
    };

    // Only used by the "_KendoAutoComplete.cshtml" partial view.
    // KABU TODO: _KendoAutoComplete.cshtml is not used currently.
    kendomodo.onExperimentalAutoCompleteChanged = function (e) {
        // "this" is Kendo.AutoComplete.

        throw new Error("Method onExperimentalAutoCompleteChanged is not implemented yet.");

        var filterValue = this.value();

        // KABU TODO: How to find the grid in context from here?
        var grid = $("#peopleGrid").data("kendoGrid");

        // Apply filter.
        if (filterValue) {
            grid.dataSource.filter({
                field: this.options.dataTextField, operator: "eq", value: filterValue
            });
        } else {
            grid.dataSource.filter({
            });
        }
    };

    // Misc utils ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 

    // KABU TODO: Only used by the scheduler. Move to scheduler.
    kendomodo.setContentReadOnly = function ($container) {

        $container.find("input,textarea").each(function () {
            var $elem = $(this);

            if ($elem.prop("tagName").toLowerCase() === "input") {

                if ($elem.closest('.k-widget').length > 0) {
                    var widget = kendo.widgetInstance($elem, kendo.ui);

                    if (typeof widget !== 'undefined')
                        widget.enable(false);
                }
            }

            $elem.attr("readonly", "readonly");
        });
    };

    // KABU TODO: APP-SPECIFIC
    kendomodo.formatStartEndMonthRange = function (startOn, endOn) {
        var s = startOn ? removeLastChar(moment(startOn).format("MMM")) : null,
            e = endOn ? removeLastChar(moment(endOn).format("MMM")) : null;

        if (!s && !e) return "";
        if (s === e) return s;
        if (s && e) return s + "-" + e;
        if (s && !e) return s + "-?";
        return "?-" + e;
    };

    // Only used by formatStartEndMonthRange()
    function removeLastChar(text) {
        return text.substring(0, text.length - 1);
    }

    kendomodo.toDisplayDateTime = function (value) {
        return value ? kendo.toString(new Date(value), "dd.MM.yyyy  HH:mm") : null;
    };

    kendomodo.toDisplayDate = function (value) {
        return value ? kendo.toString(new Date(value), "dd.MM.yyyy") : null;
    };

    kendomodo.toDisplayTime = function (value) {
        return value ? kendo.toString(new Date(value), "HH:mm") : null;
    };

    kendomodo.guid = function () {
        return kendo.guid();

        // KABU TODO: Maybe use instead: http://slavik.meltser.info/?p=142
        /*
        function _p8(s) {
            var p = (Math.random().toString(16) + "000000000").substr(2, 8);
            return s ? "-" + p.substr(0, 4) + "-" + p.substr(4, 4) : p;
        }
        return _p8() + _p8(true) + _p8(true) + _p8();
        */
    };

})(kendomodo || (kendomodo = {}));

(function ($, kendo) {

    $.extend(true, kendo.ui.validator, {
        rules: {
            // This fixes Kendo's - in my eyes - incorrect handling of booleans on complex properties.
            // See: http://www.telerik.com/forums/using-kendo-validator-with-mvc-model-properties
            mvcrequired: function (input) {
                if (input.filter("[data-val-required]").length) {
                    var value = input.val();
                    return !(value === "" || !value);
                }
                return true;
            }
        },
        messages: {
            required: (input) => kendo.format("\"{0}\" ist erforderlich.", input.attr("data-display-name")),
            pattern: "{0} ist ungültig",
            min: "{0} muss größer oder gleich sein als {1}",
            max: "{0} muss kleiner oder gleich sein als {1}",
            step: "{0} ist ungültig",
            email: "{0} ist keine gültige E-Mail",
            url: "{0} ist keine gültige URL",
            date: "{0} ist kein gültiges Datum",
            dateCompare: 'End date should be greater than or equal to the start date',
            mvcrequired: function (input) {
                return input.attr("data-val-required");
            }
        }
    });
})(jQuery, kendo);

