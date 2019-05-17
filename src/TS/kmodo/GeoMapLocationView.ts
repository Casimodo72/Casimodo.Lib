namespace kmodo {

    export class GeoMapLocationView extends GeoMapViewBase {
        constructor(options: GeoMapViewOptions) {
            super(options);

            this._options.isDrawingEnabled = true;

            this.scope = kendo.observable({
                sizeMode: "standard",
                refresh: e => {
                    this.refreshCore();
                },
                undo: e => {
                    this.undoOperation();
                },
                isPlacingTextBox: false,
                addTextBox: e => {
                    kmodo.setScopeCommandActive(e, this.getModel(), "isPlacingTextBox", true);
                    this.addEditableTextBox(() => {
                        kmodo.setScopeCommandActive(e, this.getModel(), "isPlacingTextBox", false);
                    });
                },
                isFreehandDrawingEnabled: false,
                toggleFreehandDrawing: e => {
                    this.onFreehandDrawingEnabledChanged(kmodo.toggleScopeOption(e, this.getModel(), "isFreehandDrawingEnabled"));
                }
            });

            this.getModel().bind("change", e => {
                if (e.field === "sizeMode") {
                    this.setMapSizeMode(this.getModel().get("sizeMode"));
                }
            });

            // GeoMapLocationView.initCommuneList();
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

        setContextFilter(context: ContextPlaceInfo): void {
            this._contextPlaceInfo = context;
        }

        private refreshCore(): Promise<void> {
            return this._loadMap()
                .then(() => {
                    this.createView();
                    this.clear();
                    this.displayContextLocation();
                });
        }

        private displayContextLocation(): void {

            const location = this._contextPlaceInfo;

            // KABU TODO: Currently a ProjectSegment is expected as the context location.
            if (!location.projectSegmentId)
                return;

            let url = "/odata/ProjectSegments/Query()?$select=Id,Number,Latitude,Longitude,Street,ZipCode&$expand=Contract($select=City;$expand=CountryState($select=Code))";
            url += "&$filter=";
            url += " Id eq " + location.projectSegmentId;

            cmodo.oDataQuery(url)
                .then((items: any[]) => {
                    if (items.length === 1)
                        this.addProjectSegment(items[0]);
                });
        }

        private addProjectSegment(psegment: any): void {

            if (!this._hasDataLatLong(psegment))
                return;

            const address = this._buildAddressText(psegment.Street, psegment.ZipCode, psegment.Contract.City, psegment.Contract.CountryState);

            const psegmentLinkHtml = this._formatEntityLink("ProjectSegment", psegment.Id, this._formatTextStrong(address));

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
            let width = "";
            let height = "";
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
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

            this.$view = $("#geo-map-view-" + this._options.id);

            this.initBasicComponents();

            kendo.bind(this.$view.find(".geo-map-header"), this.getModel());

            this._initMap();
        }


    }
}