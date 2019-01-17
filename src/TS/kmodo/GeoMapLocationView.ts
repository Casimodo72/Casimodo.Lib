namespace kmodo {

    export class GeoMapLocationView extends GeoMapViewBase {
        constructor(options: GeoMapViewOptions) {
            super(options);

            var self = this;

            this._options.isDrawingEnabled = true;

            this.scope = kendo.observable({
                sizeMode: "standard",
                refresh: function (e) {
                    self.refreshCore();
                },
                undo: function (e) {
                    self.undoOperation();
                },
                isFreehandDrawingEnabled: false,
                toggleFreehandDrawing: function (e) {
                    self.onFreehandDrawingEnabledChanged(kmodo.toggleScopeOption(e, self.getModel(), "isFreehandDrawingEnabled"));
                }
            });

            this.getModel().bind("change", function (e) {
                if (e.field === "sizeMode") {
                    self.setMapSizeMode(self.getModel().get("sizeMode"));
                }
            });
        }

        private onFreehandDrawingEnabledChanged(enabled: boolean): void {
            if (enabled)
                this._startMapFreehandDrawingMode();
            else
                this._endMapFreehandDrawingMode();
        }

        refresh(): Promise<void> {
            return this.refreshCore();
        }

        refreshWith(settings: ContextPlaceInfo): Promise<void> {
            this._contextPlaceInfo = settings;
            return this.refreshCore();
        }

        private refreshCore(): Promise<void> {
            var self = this;

            return this._loadMap()
                .then(() => {
                    self.createView();
                    self.clear();
                    self.displayContextLocation();
                });
        }

        private displayContextLocation(): void {
            var self = this;

            var location = this._contextPlaceInfo;

            // KABU TODO: Currently a ProjectSegment is expected as the context location.
            if (!location.projectSegmentId)
                return;

            var url = "/odata/ProjectSegments/Query()?$select=Id,Number,Latitude,Longitude,Street,ZipCode&$expand=Contract($select=City;$expand=CountryState($select=Code))";
            url += "&$filter=";
            url += " Id eq " + location.projectSegmentId;

            cmodo.oDataQuery(url)
                .then(function (items: any[]) {
                    if (items.length === 1)
                        self.addProjectSegment(items[0]);
                });
        }

        private addProjectSegment(psegment: any): void {

            if (!this._hasDataLatLong(psegment))
                return;

            var address = this._buildAddressText(psegment.Street, psegment.ZipCode, psegment.Contract.City, psegment.Contract.CountryState);

            var psegmentLinkHtml = this._formatEntityLink("ProjectSegment", psegment.Id, this._formatTextStrong(address));

            this.addMarker({
                position: {
                    lat: psegment.Latitude,
                    lng: psegment.Longitude
                },
                //color: "#00ff00",
                title: address,
                content: psegmentLinkHtml
            });

            this.setMapCenter(new google.maps.LatLng(
                psegment.Latitude,
                psegment.Longitude));

            this.setMapZoom(this.standardZoom);
        }

        private setMapSizeMode(mode: string): void {
            // DIN A4: 21.0cm x 29.7cm
            // DIN A3: 29.7cm x 42.0cm 
            var width = "";
            var height = "";
            if (mode === "standard") {
                // NOP
            }
            else if (mode === "dina4") {
                width = "20cm";
                height = "28.7cm";
            }
            else if (mode === "dina3") {
                // NOTE: DIN A3 is displayed in landspace mode here.
                width = "41cm";
                height = "28cm";
            }

            this._$googleMap.css("min-width", width);
            this._$googleMap.css("max-width", width);
            this._$googleMap.css("min-height", height);
            this._$googleMap.css("max-height", height);
        }

        createView(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            this.$view = $("#geo-map-view-" + this._options.id);

            this.initBasicComponents();

            kendo.bind(this.$view.find(".geo-map-header"), this.getModel());

            this._initMap();
        }
    }
}