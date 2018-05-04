"use strict";
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui.templates = {
            get: function (name) {

                var item = this._cache[name];
                if (typeof item !== "undefined") return item;

                item = this[name];
                if (typeof item === "undefined") return kendo.template("");

                var template = kendo.template(item);
                this._cache[name] = template;

                return template;
            },
            _cache: {},

            MoTreeView: `#var isManager=casimodo.data.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style=''#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#};##if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}#`,

            MoTreeViewOld: `#var isManager=casimodo.data.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style='font-weight:bold'#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}}#`,

            AllRowsCheckBoxSelectorGridCell: `#var randomId = kendomodo.guid();#<input id='cb-all-#:randomId#' class='k-checkbox all-list-items-selector' type='checkbox' /><label class='k-checkbox-label' for='cb-all-#:randomId#' />`,

            RowCheckBoxSelectorGridCell: `#var randomId = kendomodo.guid();#<input id='cb-#:randomId#' class='k-checkbox list-item-selector' type='checkbox' /><label class='k-checkbox-label list-item-selector' for='cb-#:randomId#' style='display:none'/>`,

            RowRemoveCommandGridCell: `<div class="list-item-remove-command"><span class="k-icon k-delete"></span></div>`
        };
    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));