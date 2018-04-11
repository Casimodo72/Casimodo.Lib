"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var GeoPlaceLookupViewModel = (function (_super) {
            casimodo.__extends(GeoPlaceLookupViewModel, _super);

            function GeoPlaceLookupViewModel(options) {
                _super.call(this, options);

                this.$addressInfo = null;
                this._dialogWindow = null;
            }

            var fn = GeoPlaceLookupViewModel.prototype;

            fn.setArgs = function (args) {
                var self = this;
                this.args = args;
                this.scope.set("item", args.item ? kendo.observable(args.item) : kendo.observable(new kendomodo.ui.GeoPlaceInfo()));

                if (this._options.isDialog) {
                    this.args.isCancelled = false;
                    this.args.isOk = false;

                    this.args.buildResult = function () {
                        var item = self.getCurrentItem();

                        self.args.item = item;
                    };
                }
            };

            fn._initMap = function () {
                if (this._isMapInitialized)
                    return;

                this._initMapCore();
            };

            fn.refresh = function () {
                var self = this;

                return new Promise(function (resolve, reject) {
                    kendomodo.ui.GoogleMapInitializer.one("scriptReady", function (e) {
                        self._initMap();
                        self.findInitialPlace();
                        resolve();
                    });
                    kendomodo.ui.GoogleMapInitializer.init();
                });
            };

            fn.createComponent = function () {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                this.$view = $("#geo-map-lookup-view-" + this._options.id);

                this.$coordinatesDisplay = this.$view.find(".map-coordinates");
                this.$mapContainer = this.$view.find(".google-map");
                this.$searchInput = this.$view.find(".pac-input");

                this.$addressInfo = this.$view.find("div.address-info");
                kendo.bind(this.$addressInfo, this.scope);

                if (this._options.isDialog)
                    this._initComponentAsDialog();
            };

            fn._initComponentAsDialog = function () {
                var self = this;

                this._dialogWindow = kendomodo.findKendoWindow(this.$view);

                this._initDialogWindowTitle();

                // KABU TODO: IMPORTANT: There was no time yet to develop a
                //   decorator for dialog functionality. That's why the view model
                //   itself has to take care of the dialog commands which are located
                //   *outside* the widget.
                var $dialogCommands = $('#dialog-commands-' + this._options.id);
                // Init OK/Cancel buttons.
                $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", function () {
                    if (!self.getCurrentItem())
                        return false;

                    self.args.buildResult();
                    self.args.isCancelled = false;
                    self.args.isOk = true;

                    self._dialogWindow.close();
                });

                $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
                    self.args.isCancelled = true;
                    self.args.isOk = false;

                    self._dialogWindow.close();
                });
            };

            fn._initDialogWindowTitle = function () {
                var title = "";

                if (this.args.title) {
                    title = this.args.title;
                }
                else {
                    title = this._options.title || "";

                    if (this._options.isLookup)
                        title += " wählen";
                }

                this._dialogWindow.title(title);
            };

            return GeoPlaceLookupViewModel;

        })(kendomodo.ui.GeoMapViewModelBase);
        ui.GeoPlaceLookupViewModel = GeoPlaceLookupViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));