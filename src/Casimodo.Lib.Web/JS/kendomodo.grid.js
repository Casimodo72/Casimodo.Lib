"use strict";

kendo.ui.Grid.prototype.options.messages.commands.update = "Speichern";

var kendomodo;
(function (kendomodo) {

    kendomodo.gridReferenceFilterColTemplateNullable = function (args) {
        kendomodo.gridReferenceFilterColTemplate(args, true);
    };

    kendomodo.gridReferenceFilterColTemplate = function (args, nullable) {

        var options = {
            dataSource: args.dataSource,
            dataValueField: "text",
            dataTextField: "text",
            valuePrimitive: true
        };

        if (nullable) {
            options.optionLabel = " ";
        }

        args.element.kendoDropDownList(options);
    };

    kendomodo.gridEnumFilterColTemplateNullable = function (args) {
        kendomodo.gridEnumFilterColTemplate(args, true);
    };

    kendomodo.gridEnumFilterColTemplate = function (args, nullable) {

        var options = {
            dataSource: args.dataSource,
            dataValueField: "value",
            dataTextField: "text",
            valuePrimitive: true
        };

        if (nullable) {
            options.optionLabel = {
                value: "", text: " "
            };
        }

        args.element.kendoDropDownList(options);
    };

    kendomodo.getGridRowDataItem = function (grid, $elem) {
        return grid.dataItem($elem.closest("tr[role='row']"));
    };

})(kendomodo || (kendomodo = {}));
