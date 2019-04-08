namespace kmodo {

    export class GeoMapPlaceLookup extends GeoMapViewBase {
        private _dialogWindow: kendo.ui.Window = null;

        constructor(options: GeoMapViewOptions) {
            super(options);

        }

        setArgs(args): void {
            this.args = args;
            this.getModel().set("item", args.item ? kendo.observable(args.item) : kendo.observable(new kmodo.GeoPlaceInfo()));

            if (this._options.isDialog) {
                this.args.isCancelled = false;
                this.args.isOk = false;

                this.args.buildResult = () => {
                    const item = this.getCurrentItem();

                    this.args.item = item;
                };
            }
        }

        refresh(): Promise<void> {
            return this._loadMap()
                .then(() => {
                    this._initMap();
                    this._findContextPlace();
                });
        }

        private _findContextPlace(): void {
            const address = this.getCurrentItem().getDisplayAddress();
            if (address) {
                // NOTE: Setting the search text does not trigger a search.
                this.setSearchText(address);
                // Search for the place by address.
                this.findContextPlaceByAddress(address);
            }
        }

        createView(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            this.$view = $("#geo-map-lookup-view-" + this._options.id);

            this.initBasicComponents();

            this._$addressInfo = this.$view.find(".geo-map-address-info");
            kendo.bind(this._$addressInfo, this.getModel());

            if (this._options.isDialog)
                this._initComponentAsDialog();
        }

        private _initComponentAsDialog(): void {
            this._dialogWindow = kmodo.findKendoWindow(this.$view);
            this._initDialogWindowTitle();

            // KABU TODO: Move to helper.
            // KABU TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the widget.
            const $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", (e) => {
                if (!this.getCurrentItem())
                    return false;

                this.args.buildResult();
                this.args.isCancelled = false;
                this.args.isOk = true;

                this._dialogWindow.close();
                return false;
            });

            // KABU TODO: Move to helper.
            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", () => {
                this.args.isCancelled = true;
                this.args.isOk = false;

                this._dialogWindow.close();
            });
        };

        // KABU TODO: Move to helper.
        private _initDialogWindowTitle(): void {
            let title = "";

            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._options.isLookup)
                    title += " wählen";
            }

            this._dialogWindow.title(title);
        }
    }
}