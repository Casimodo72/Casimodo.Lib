"use strict";
var casimodo;
(function (casimodo) {
    (function (ui) {

        ui.templates = {
            getTemplate: function (name) {
                return this.get(name);
            },
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

            // KABU TODO: Move out of lib code.
            MoTreeView: `#var isManager=casimodo.data.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style=''#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#};##if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}#`,

            MoTreeViewOld: `#var isManager=casimodo.data.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style='font-weight:bold'#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}}#`,
        };
    })(casimodo.ui || (casimodo.ui = {}));
})(casimodo || (casimodo = {}));