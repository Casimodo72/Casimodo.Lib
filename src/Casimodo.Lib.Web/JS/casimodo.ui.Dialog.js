"use strict";

var casimodo;
(function (casimodo) {
    (function (ui) {

        ui.findDialogContainer = function ($context) {

            if ($context && $context.length) {
                var $container = $context.closest(".dialog-container");
                if ($container.length)
                    return $container;
            }

            return $(window.document.body);
        };

        ui.getPageDialogContainer = function () {

            if (!window.casimodo.run.$pageDialogContainer)
                window.casimodo.run.$pageDialogContainer = $(window.document.body).find("#page-dialog-container").first();

            return window.casimodo.run.$pageDialogContainer;
        };

        ui.DialogArgs = (function () {
            var DialogArgs = function (id, value) {
                this.id = id;
                this.value = value || null;
                this.values = [];
                this.buildResult = function () { };
                this.initSpace = function () { };
            };

            var fn = DialogArgs.prototype;

            fn.getValue = function (name) {
                var vals = this.values;

                if (!vals)
                    return undefined;

                for (var i = 0; i < vals.length; i++) {
                    if (vals[i].name === name)
                        return vals[i].value;
                }

                return undefined;
            };

            fn.getFilterValue = function (name) {
                var vals = this.filters;

                if (!vals)
                    return undefined;

                for (var i = 0; i < vals.length; i++) {
                    if (vals[i].field === name)
                        return vals[i].value;
                }

                return undefined;
            };

            return DialogArgs;
        })();

        var DialogArgsContainer = (function () {
            var DialogArgsContainer = function () {
                this.items = [];
                this._events = new casimodo.EventManager();
            };

            var fn = DialogArgsContainer.prototype;

            fn.add = function (item, trigger) {
                this.items.push(item);
                if (trigger)
                    this.trigger(item);
            };

            fn.get = function (id) {

                for (var i = this.items.length - 1; i >= 0; i--) {
                    if (this.items[i].id === id)
                        return this.items[i];
                }

                return null;
            };

            fn.trigger = function (item) {
                this._events.trigger(item.id, { args: item }, this);
            };

            fn.on = function (id, func) {
                this._events.on(id, func);
            };

            fn.consume = function (id) {
                var item = this.get(id);
                this.remove(item);
                return item;
            };

            fn.remove = function (item) {
                if (!item) return;
                var index = this.items.indexOf(item);
                if (index !== -1) {
                    this.items.splice(index, 1);
                }
            };

            return DialogArgsContainer;

        })();
        ui.dialogArgs = new DialogArgsContainer();

    })(casimodo.ui || (casimodo.ui = {}));
})(casimodo || (casimodo = {}));
