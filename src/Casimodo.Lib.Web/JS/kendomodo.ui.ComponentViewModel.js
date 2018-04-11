"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {
        var ComponentViewModel = (function (_super) {
            casimodo.__extends(ComponentViewModel, _super);

            function ComponentViewModel(options) {
                _super.call(this, options);

                this._super = _super;

                // Extend with extra options.
                if (this._options.extra) {
                    for (var prop in this._options.extra)
                        this._options[prop] = this._options.extra[prop];
                }

                this.$view = null;

                this.scope = kendo.observable({ item: null });
                this.scope.bind("change", $.proxy(this._onScopeChanged, this));

                this._isDebugLogEnabled = false;
            }

            var fn = ComponentViewModel.prototype;

            fn._onScopeChanged = function (e) {
                this.trigger("scopeChanged", e);
            };

            fn.refresh = function () {
                // NOP
            };

            // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn._eve = function (handler) {
                return $.proxy(handler, this);
            };

            return ComponentViewModel;

        })(casimodo.ui.ComponentViewModel);
        ui.ComponentViewModel = ComponentViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));