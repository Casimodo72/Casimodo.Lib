namespace kmodo {

    export interface GeoMapViewOptions extends ViewComponentOptions {
        isDrawingEnabled?: boolean;
    }

    interface GeoMapSurfaceListeners {
        move: google.maps.MapsEventListener;
        mouseUp: google.maps.MapsEventListener;
        mouseDown: google.maps.MapsEventListener;
    }

    export interface GeoMapMarkerOptions {
        map?: google.maps.Map;
        position: google.maps.LatLngLiteral;
        //lat: number;
        //lng: number;
        color?: string;
        symbol?: any; // E.g.: google.maps.SymbolPath.CIRCLE
        title: string;
        label?: string;
        content?: string;
        doubleClicked?: Function;
        zIndex?: number;
        customData?: any
    }

    export interface ContextPlaceInfo {
        companyId: string;
        contractId: string;
        projectId: string;
        projectSegmentId: string;
    }


    export abstract class GeoMapViewBase extends ViewComponent {
        _options: GeoMapViewOptions;
        $view: JQuery;
        _$googleMap: JQuery;
        _$searchInput: JQuery;
        _$coordinatesDisplay: JQuery;
        _$mapContainer: JQuery;
        _$addressInfo: JQuery = null;
        standardZoom: 12;
        map: google.maps.Map;
        drawingManager: google.maps.drawing.DrawingManager;
        _placesService: google.maps.places.PlacesService;
        _distanceMatrixService: google.maps.DistanceMatrixService;
        _directionsService: google.maps.DirectionsService;
        _directionsRenderer: google.maps.DirectionsRenderer;
        // Define context place info.
        _contextPlaceInfo: ContextPlaceInfo;
        _contextPlaceMarker: google.maps.Marker;
        _mapListeners: GeoMapSurfaceListeners;
        _drawingOverlays: google.maps.MVCObject[];
        _locationMarkers: google.maps.Marker[];
        _selectionCircles: google.maps.Circle[];
        _infoBox: any;
        _isMapInitialized: boolean;
        _isComponentInitialized: boolean;

        constructor(options: GeoMapViewOptions) {
            super(options);

            this._options.isDrawingEnabled = false;

            this.$view = null;

            this.standardZoom = 12;

            this._$coordinatesDisplay = null;
            this._$mapContainer = null;
            this._$searchInput = null;

            this.map = null;

            // Context location
            this._contextPlaceInfo = null;
            this._contextPlaceMarker = null;

            this._mapListeners = {
                move: null,
                mouseUp: null,
                mouseDown: null
            };
            this._drawingOverlays = [];

            this._locationMarkers = [];
            this._selectionCircles = [];
            this._infoBox = null;
            this._isMapInitialized = false;

            this._isComponentInitialized = false;
            this.getModel().set("item", new kmodo.GeoPlaceInfo());
        }

        getCurrentItem(): any {
            return this.getModel().item;
        }

        setSearchText(value: string): void {
            this._$searchInput.val(value);
        }

        initSearchInputBox(): void {
            if (!this._$searchInput || !this._$searchInput.length)
                return;

            // Create search box.
            // https://developers.google.com/maps/documentation/javascript/reference#AutocompleteOptions
            let options = {
                componentRestrictions: {
                    // Restrict to Germany.
                    country: 'DE'
                }
                // https://developers.google.com/places/supported_types#table3
                //types: ["geocode"]
            };
            let searchBoxInputElem = this._$searchInput.get(0) as HTMLInputElement;
            let searchBox = new google.maps.places.Autocomplete(searchBoxInputElem, options);
            this.map.controls[google.maps.ControlPosition.TOP_LEFT].push(searchBoxInputElem);

            // Bias the SearchBox results towards current map's viewport.
            //map.addListener('bounds_changed', () => {
            //    searchBox.setBounds(map.getBounds());
            //});

            // Listen for the event fired when the user selects a prediction and retrieve
            // more details for that place.
            searchBox.addListener('place_changed', () => {
                let place = searchBox.getPlace();
                if (place.geometry)
                    this._activateContextPlace(place);
                else
                    this.findContextPlaceByAddress(place.name);
            });

            // KABU TODO: REMOVE? Was this just an experiment?
            //searchBox.addListener('remove_at', () => {
            //    alert("removed");
            //});
        }

        findContextPlaceByAddress(address: string, options?: any): Promise<void> {
            return new Promise((resolve, reject) => {
                // Geocoder: https://developers.google.com/maps/documentation/javascript/reference/geocoder
                (new google.maps.Geocoder()).geocode({
                    // GeocoderRequest: https://developers.google.com/maps/documentation/javascript/reference/geocoder#GeocoderRequest                      
                    //  address: string,
                    //  location: LatLng,
                    //  placeId: string,
                    //  bounds: LatLngBounds,
                    //  componentRestrictions: GeocoderComponentRestrictions,
                    //  region: string

                    address: address,

                    // GeocoderComponentRestrictions: https://developers.google.com/maps/documentation/javascript/reference/geocoder#GeocoderComponentRestrictions
                    componentRestrictions: {
                        // Restrict to Germany.
                        country: "DE"
                    }
                }, (results: google.maps.GeocoderResult[], status: google.maps.GeocoderStatus) => {

                    if (status !== google.maps.GeocoderStatus.OK) {
                        const msg = "Ich kann diesen Ort nicht finden. " + status;
                        cmodo.showError(msg);
                        reject(new Error(msg));
                    }
                    else {
                        this._activateContextPlace(results[0], options);
                        resolve();
                    }
                });
            });
        }

        _activateContextPlace(place: google.maps.places.PlaceResult | google.maps.GeocoderResult, options?: any): void {

            // TODO: Eliminate use of the "current item".
            // Set place values on data view model.
            this.getCurrentItem().buildFromGoogleMapsPlace(place);

            const geometryLocation = place.geometry.location;
            // TODO: REMOVE?
            // const lat = location.lat().toFixed(6);
            // const lng = location.lng().toFixed(6);

            this.setMapCenter(geometryLocation);
            this._contextPlaceMarker.setPosition(geometryLocation);
            this.setMapZoom(this.standardZoom);

            if (this._infoBox && (!options || !options.isInfoHidden)) {
                this._infoBox.close();

                const infoText = this._formatTextStrong(place.formatted_address);
                this._infoBox.setContent(infoText);
                this._infoBox.open(this.map, this._contextPlaceMarker);
            }
        }

        // Markers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        _getMarkerSymbolOptions(options: any): google.maps.Symbol {
            const symbol: google.maps.Symbol = {
                path: options.symbol || google.maps.SymbolPath.CIRCLE,
                scale: 8,
                strokeColor: options.color || "#ff0000",
                strokeOpacity: options.opacity || 0.4
            };

            return symbol;
        }

        protected getMarkerCustomData(marker: google.maps.Marker): any {
            return this.getData(marker);
        }

        protected getData(item: google.maps.MVCObject): any {
            return item["customData"] || (item["customData"] = {});
        }

        protected setDataValue(item: google.maps.MVCObject, name: string, value: any): void {
            const data = this.getData(item);
            data[name] = value;
        }

        protected getDataValue(item: google.maps.MVCObject, name: string): any {
            return this.getData(item)[name];
        }

        protected addLabel(item: google.maps.MVCObject, label: MapLabel): void {
            const data = this.getData(item);
            const labels = data.labels || (data.labels = []) as MapLabel[];
            labels.push(label);
        }

        _getMarkerOptions(options: GeoMapMarkerOptions): google.maps.MarkerOptions {

            const markerOptions: google.maps.MarkerOptions = {
                map: this.map,
                position: {
                    lat: options.position.lat,
                    lng: options.position.lng
                },
                // TODO: REMOVE: || { lat: options.lat, lng: options.lng },
                title: options.title,
                label: options.label,
                zIndex: options.zIndex
            };

            (markerOptions as any).customData = options.customData;

            if (typeof options.symbol !== "undefined") {
                // https://developers.google.com/maps/documentation/javascript/symbols#predefined
                markerOptions.icon = this._getMarkerSymbolOptions(options);
            }
            else if (options.color) {

                if (options.color[0] === "#")
                    markerOptions.icon = this._createMarkerImage(options.color);
                else
                    markerOptions.icon = this._getMarkerStandardColorUrl(options.color);
            }

            return markerOptions;
        }

        _trackLocationMarker(marker: google.maps.Marker): void {
            this._locationMarkers.push(marker)
        }

        addMarker(options: GeoMapMarkerOptions): google.maps.Marker {
            const markerOptions = this._getMarkerOptions(options);
            const marker = new google.maps.Marker(markerOptions);

            this._trackLocationMarker(marker);

            if (options.content) {
                google.maps.event.addListener(marker, 'click', (e) => {
                    this._infoBox.close();
                    // const content = "<div style='font-family:Roboto,Arial;color:rgb(51, 51, 51)'>" + options.content + "</div>";
                    const content = options.content;
                    this._infoBox.setContent(content);
                    this._infoBox.open(this.map, marker);

                    this.showSelection(marker);
                    this.showDistances(marker);
                });
            }

            if (options.doubleClicked) {
                google.maps.event.addListener(marker, 'dblclick', (e) => {
                    options.doubleClicked(marker, options);
                });
            }

            return marker;
        }

        // Labels ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        removeMarkerLabel(marker: google.maps.Marker, role: string): boolean {
            return this.removeLabel(marker, role);
        }

        findMarkerLabel(marker: google.maps.Marker, role: string): MapLabel {

            return this.findLabel(marker, role);
        }

        removeLabel(item: google.maps.MVCObject, role: string): boolean {
            const labels = this.getDataValue(item, "labels") as MapLabel[];
            if (!labels || !labels.length)
                return false;

            const idx = labels.findIndex(x => (x as any).dataRole === role);
            if (idx === -1)
                return false;

            const label = labels[idx];
            label.setMap(null);
            labels.splice(idx, 1);

            return true;
        }

        findLabel(item: google.maps.MVCObject, role: string): MapLabel {

            let labels = this.getDataValue(item, "labels") as MapLabel[];
            if (!labels || !labels.length)
                return null;

            return labels.find(x => (x as any).dataRole === role);
        }

        createLabel(options: any): MapLabel {
            // Source: https://github.com/googlemaps/js-map-label
            const label = new MapLabel({
                text: options.content,
                position: options.position,
                map: this.map,
                fontSize: 14,
                align: "left"
            });

            return label;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected _clearAllOverlays(): void {
            this.clearSelectionCircles();
            this.clearLocationMarkers();
            this.clearDrawingOverlays();
        }

        clear(): void {
            this._clearAllOverlays();
        }

        clearLocationMarkers(): void {
            // Clear markers
            this.removeItems(this._locationMarkers);
            this._locationMarkers = [];
        }

        clearSelectionCircles(): void {
            for (let x of this._selectionCircles)
                x.setMap(null);
        }

        clearDrawingOverlays(): void {
            this.removeItems(this._drawingOverlays);
            this._drawingOverlays = [];
        }

        protected _openMarkerInfoWindow(marker: google.maps.Marker, content: string): void {
            this._infoBox.close();
            this._infoBox.setContent(content);
            this._infoBox.open(this.map, marker);
        }

        protected _queryPlaceDetails(request: google.maps.places.PlaceDetailsRequest): Promise<google.maps.places.PlaceResult> {
            // https://developers.google.com/maps/documentation/javascript/places#place_details
            // https://developers.google.com/maps/documentation/javascript/examples/place-details

            // WARNING: If you do not specify at least one field with a request,
            //   or if you omit the fields parameter from a request,
            //   ALL possible fields will be returned, and you will be billed accordingly.

            return new Promise((resolve, reject) => {
                this._getPlacesService()
                    .getDetails(request, (place, status) => {
                        if (status === google.maps.places.PlacesServiceStatus.OK) {
                            console.info("GM details: OK");
                            resolve(place);
                        }
                        else {
                            console.warn("GM details: " + status);
                            reject(status);
                        }
                    });
            });
        }

        protected _getPlacesService(): google.maps.places.PlacesService {
            return this._placesService || (this._placesService = new google.maps.places.PlacesService(this.map));
        }

        protected _getDistanceMatrixService(): google.maps.DistanceMatrixService {
            return this._distanceMatrixService || (this._distanceMatrixService = new google.maps.DistanceMatrixService());
        }

        protected _getDirectionsService(): google.maps.DirectionsService {
            return this._directionsService || (this._directionsService = new google.maps.DirectionsService());
        }

        protected _getDirectionsRenderer(): google.maps.DirectionsRenderer {
            // DirectionsRenderer: https://developers.google.com/maps/documentation/javascript/reference/directions#DirectionsRenderer

            return this._directionsRenderer ||
                (this._directionsRenderer = new google.maps.DirectionsRenderer({
                    map: this.map,
                    // DirectionsRendererOptions: https://developers.google.com/maps/documentation/javascript/reference/directions#DirectionsRendererOptions
                    suppressMarkers: true,
                    preserveViewport: true,
                    hideRouteList: true,
                    // PolylineOptions: https://developers.google.com/maps/documentation/javascript/reference/polygon#PolylineOptions
                    polylineOptions: {
                        //strokeColor: "#ff0000"
                    }
                }));
        }

        protected _showDrivigDistanceAsync(fromMarker: google.maps.Marker, toMarker: google.maps.Marker): void {
            this._getDrivingDistanceAsync(fromMarker.getPosition(), toMarker.getPosition(),
                (data) => {
                    const label = this.findMarkerLabel(toMarker, "ProjectDistance");
                    if (!label)
                        return;

                    const text = label.get("text") + "/" + (data.distance.value / 1000).toFixed(1) + " km (" + data.duration.text + ")";
                    label.set("text", text);
                });
        }

        protected _getDrivingDistanceAsync(
            from: google.maps.LatLng,
            to: google.maps.LatLng,
            callback: (data: google.maps.DistanceMatrixResponseElement) => void): void {

            // DRIVING, WALKING, BICYCLING, TRANSIT
            // Doc: https://developers.google.com/maps/documentation/javascript/directions#TravelModes
            // Example: https://developers.google.com/maps/documentation/javascript/examples/directions-travel-modes?hl=en

            this._getDistanceMatrixService().getDistanceMatrix(
                {
                    origins: [from],
                    destinations: [to],
                    travelMode: google.maps.TravelMode.DRIVING,
                    unitSystem: google.maps.UnitSystem.METRIC,
                    avoidHighways: false,
                    avoidTolls: false
                }, (response, status) => {

                    let data: google.maps.DistanceMatrixResponseElement = null;

                    const row = response.rows.length ? response.rows[0] : null;
                    if (row)
                        data = row.elements.length ? row.elements[0] : null;

                    callback(data);
                });
        }

        protected _clearRoutes(): void {
            this._setRoutes({ geocoded_waypoints: [], routes: [] });
        }

        protected _showRoutes(routes: google.maps.DirectionsResult): void {
            this._setRoutes(routes);
        }

        protected _setRoutes(routes: google.maps.DirectionsResult): void {
            this._getDirectionsRenderer().setDirections(routes);
        }

        protected _queryRouteAsync(
            originLocation: google.maps.LatLngLiteral,
            destinationLocation: google.maps.LatLngLiteral)
            : Promise<google.maps.DirectionsResult> {

            return new Promise((resolve, reject) => {
                // DirectionsService: https://developers.google.com/maps/documentation/javascript/directions
                const request: google.maps.DirectionsRequest = {
                    origin: originLocation,
                    destination: destinationLocation,
                    travelMode: google.maps.TravelMode.DRIVING,
                    avoidFerries: true
                };

                this._getDirectionsService().route(request, (response, status) => {
                    if (status === google.maps.DirectionsStatus.OK)
                        resolve(response);
                    else
                        reject(status);
                });
            });
        };

        removeItems(items: any[]): void {
            if (!items || !items.length)
                return;

            for (const x of items) {
                x.setMap(null);

                let labels = x["labels"] as MapLabel[];

                if (labels) {
                    this.removeItems(labels);
                    labels = [];
                }
            }
        }

        showSelection(marker: google.maps.Marker): void {
            // NOP
        }

        showDistances(marker: google.maps.Marker): void {
            // NOP
        }

        setSelectedItemCircles(position: google.maps.LatLng): void {
            if (!this._selectionCircles.length) {

                const circleOptions = {
                    strokeColor: "black",
                    strokeOpacity: 1,
                    strokeWeight: 1,
                    //fillColor: "yellow",
                    fillOpacity: 0,
                    clickable: false,
                    map: this.map,
                    center: position, // or you can pass a google.maps.LatLng object
                    //radius: 5 * 1000 // radius of the circle in metres
                };

                this._createDistanceCircles(circleOptions, 3);
            }
            else {
                for (const x of this._selectionCircles) {
                    x.setCenter(position);
                    x.setMap(this.map);
                }
            }
        }

        _createDistanceCircles(circleOptions, num: number): void {
            // 5 km distances.
            for (let i = 0; i < num; i++) {
                circleOptions.radius = (i + 1) * 5000;
                this._selectionCircles.push(new google.maps.Circle(circleOptions));
            }
        };

        _hasDataLatLong(data) {
            // KABU TODO: Inform the user somehow of the entities (e.g. ProjectSegments)
            //   where lat/long is missing.
            return data && data.Latitude && data.Longitude;
        };

        setMapCenter(position: google.maps.LatLngLiteral | google.maps.LatLng): void {
            this.map.setCenter(position);
        }

        setMapZoom(value: number): void {
            this.map.setZoom(value);
        }

        _initMap(): void {
            if (this._isMapInitialized)
                return;

            this._initMapCore();
        }

        private _initMapCore(): void {
            if (this._isMapInitialized) return;
            this._isMapInitialized = true;

            this._checkGoogleApiLoaded();

            // Init MapLabel lib. It has no TS type declarations.
            (window as any).InitMapLabelLib();

            const hamburg = new google.maps.LatLng(53.550370, 9.994161);

            // MapTypeId.ROADMAP displays the default road map view. This is the default map type.
            // MapTypeId.SATELLITE displays Google Earth satellite images
            // MapTypeId.HYBRID displays a mixture of normal and satellite views
            // MapTypeId.TERRAIN displays a physical map based on terrain information.
            const mapTypeId = google.maps.MapTypeId.ROADMAP;

            // mapOptions: https://developers.google.com/maps/documentation/javascript/reference/map#MapOptions
            const options = {
                zoom: this.standardZoom,
                center: hamburg,
                mapTypeId: mapTypeId,
                scaleControl: true,
                overviewMapControl: true,
                overviewMapControlOptions: { opened: true },
                addressControlOptions: {
                    position: google.maps.ControlPosition.BOTTOM_CENTER
                },
                linksControl: true,
                //gestureHandling: 'greedy'
            };

            const map = this.map = new google.maps.Map(this._$mapContainer[0], options);

            if (this._options.isDrawingEnabled) {
                this.drawingManager = new google.maps.drawing.DrawingManager({
                    map: map,
                    drawingMode: null, //google.maps.drawing.OverlayType.MARKER,
                    drawingControl: true,
                    drawingControlOptions: {
                        position: google.maps.ControlPosition.TOP_CENTER,
                        drawingModes: [
                            google.maps.drawing.OverlayType.CIRCLE,
                            google.maps.drawing.OverlayType.POLYGON,
                            google.maps.drawing.OverlayType.POLYLINE,
                            google.maps.drawing.OverlayType.RECTANGLE]
                    },
                    //markerOptions: { icon: 'https://developers.google.com/maps/documentation/javascript/examples/full/images/beachflag.png' },
                    circleOptions: {
                        fillColor: '#ffff00',
                        fillOpacity: 1,
                        strokeWeight: 5,
                        clickable: false,
                        editable: true,
                        zIndex: 1
                    }
                });

                google.maps.event.addListener(this.drawingManager, 'overlaycomplete', (e) => {
                    this._drawingOverlays.push(e.overlay);
                });
            }

            this._contextPlaceMarker = new google.maps.Marker({
                map: this.map,
                position: null
                // Define the place with a location, and a query string.
                // place: { location: hamburg, query: 'Hamburg'},
                // Attributions help users find your site again.
                // attribution: {}
            });

            this._infoBox = new google.maps.InfoWindow();

            this.initSearchInputBox();
        }

        refresh(): Promise<void> {
            return this._loadMap()
                .then(() => this._initMap());
        }

        _loadMap(): Promise<void> {
            return new Promise((resolve, reject) => {
                kmodo.googleMapInitializer.one("scriptReady", (e) => {
                    resolve();
                });
                kmodo.googleMapInitializer.init();
            });
        }

        // Undo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        undoOperation(): void {
            const overlay = this._drawingOverlays.pop();
            if (!overlay)
                return;

            this.removeItems([overlay]);
        }

        // Free hand drawing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected _startMapFreehandDrawingMode(): void {
            this.map.setOptions({
                draggable: false,
                zoomControl: false,
                streetViewControl: false,
                scrollwheel: false,
                disableDoubleClickZoom: false,
                draggableCursor: "crosshair"
            });

            // Hide drawing manager control.
            //this.drawingManager.setDrawingMode(null);
            this.drawingManager.setOptions({
                drawingMode: null,
                drawingControl: false
            });

            this._mapListeners.mouseDown = google.maps.event.addDomListener(this.map.getDiv(), 'mousedown', (e) => {
                this._drawFreeHandUntilMouseUp();
            });
        }

        protected _endMapFreehandDrawingMode(): void {

            const mouseDown = this._mapListeners.mouseDown;
            this._mapListeners.mouseDown = null;
            google.maps.event.removeListener(mouseDown);

            // TODO: REMOVE: google.maps.event.clearListeners(this.map.getDiv(), 'mousedown');

            this.map.setOptions({
                draggable: true,
                zoomControl: true,
                streetViewControl: true,
                scrollwheel: true,
                disableDoubleClickZoom: true,
                draggableCursor: null
            });

            // Show drawing manager control.
            this.drawingManager.setOptions({
                drawingControl: true
            });
        }

        protected _drawFreeHandUntilMouseUp(): void {
            const freehandLine = new google.maps.Polyline(
                {
                    map: this.map,
                    clickable: false,
                    // TODO: REMOVE
                    //fillColor: '#0099FF',
                    //fillOpacity: 0.7,
                    strokeColor: '#AA2143',
                    strokeWeight: 2,
                });

            // Mouse move: draw.
            const moveEventListener = google.maps.event.addListener(this.map, 'mousemove', (e) => {
                freehandLine.getPath().push(e.latLng);
            });

            // Mouse up: complete drawing.
            google.maps.event.addListenerOnce(this.map, 'mouseup', (e) => {
                google.maps.event.removeListener(moveEventListener);
                // TODO: REMOVE: google.maps.event.clearListeners(this.map.getDiv(), 'mousedown');

                const path = freehandLine.getPath();
                freehandLine.setMap(null);

                // Simplify path.
                const pathArray = path.getArray();
                // KABU TODO: IMPORTANT: We need to adjust the tolerance value (in meters)
                //   according to the zoom of the map. Otherwise drawn polylines in the sub
                //   100 meters resolution will be reduced to a single straight line.
                const effectivePathArray = pathArray; //GDouglasPeucker(pathArray, 100);

                // Ignore empty or too short paths.
                if (effectivePathArray.length > 2) {

                    const polyOptions = {
                        map: this.map,
                        fillColor: '#0099FF',
                        fillOpacity: 0.7,
                        strokeColor: '#AA2143',
                        strokeWeight: 2,
                        clickable: false,
                        zIndex: 1,
                        path: effectivePathArray,
                        editable: false
                    };

                    this._drawingOverlays.push(new google.maps.Polyline(polyOptions));
                }
            });


        }

        private exportAsPng(): Promise<string> {

            kmodo.progress(true, this.$view);

            return new Promise((resolve, reject) => {

                // See https://github.com/niklasvh/html2canvas/issues/345#issuecomment-420260348
                const mapElem = this._$googleMap.find('.gm-style>div:eq(0)')[0];

                html2canvas(mapElem,
                    {
                        useCORS: true,
                        // allowTaint: true,
                    })
                    .then((canvas) => {
                        const dataUrl = canvas.toDataURL("image/png");

                        kmodo.progress(false, this.$view);

                        resolve(dataUrl);
                        //$imgOut.attr("src", dataUrl);
                    });
            });
        }

        private initSaveToFileCommand(): void {
            const $saveToFileCmd = this.$view.find(".geo-map-save-to-file-command");
            if ($saveToFileCmd.length) {
                $saveToFileCmd.on("click", () => {
                    // Convert map to image as data URL.
                    this.exportAsPng()
                        .then((dataUrl) => {
                            // Open save to Mo file system dialog. 
                            kmodo.openById("16b14f2e-907a-4fba-adea-beca4f995c8c",
                                {
                                    title: "Karte als Bild (PNG) speichern",
                                    item: {
                                        imageDataUrl: dataUrl,
                                        context: this._contextPlaceInfo
                                    },
                                    options: {
                                    },
                                    finished: (result) => {
                                        if (result.isOk) {
                                            // NOP
                                        }
                                    }
                                });
                        });
                });
            }
        }

        protected initBasicComponents(): void {
            this._$googleMap = this.$view.find(".google-map");
            this._$addressInfo = this.$view.find(".geo-map-address-info");
            this._$coordinatesDisplay = this.$view.find(".map-coordinates");
            this._$mapContainer = this.$view.find(".google-map");
            this._$searchInput = this.$view.find(".pac-input");

            this.initSaveToFileCommand();
        }

        abstract createView(): void;

        // Utils ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private _checkGoogleApiLoaded(): void {
            if (typeof google === "undefined") {
                alert("ERROR: Google Map API not loaded yet.");
                debugger;
            }
        }

        // Marker visuals ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // https://maps.gstatic.com/mapfiles/place_api/icons/doctor-71.png

        private _getMarkerStandardColorUrl(color: string): string {
            return "http://maps.google.com/mapfiles/ms/icons/" + color + "-dot.png";
        }

        protected _setMarkerColor(marker: google.maps.Marker, color: string): void {
            if (color === "blue")
                marker.setIcon("http://maps.google.com/mapfiles/ms/icons/blue-dot.png");
            else if (color === "yellow")
                marker.setIcon("http://maps.google.com/mapfiles/ms/icons/yellow-dot.png");
        }

        protected _setMarkerStandardColor(marker: google.maps.Marker, color: string): void {
            marker.setIcon(this._getMarkerStandardColorUrl(color));
        }

        protected _setMarkerImage(marker: google.maps.Marker, color: string): void {
            marker.setIcon(this._createMarkerImage(color));
        }

        protected _createMarkerImage(color: string): google.maps.MarkerImage {
            // KABU TODO: IMPORTANT: MarkerImage has been deprecated.

            color = color.substring(1);

            /* Source: http://stackoverflow.com/questions/7095574/google-maps-api-3-custom-marker-color-for-default-dot-marker/7686977#7686977 */
            return new google.maps.MarkerImage(
                "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=%E2%80%A2|" + color,
                new google.maps.Size(22, 40), // 21, 34
                new google.maps.Point(0, 0),
                new google.maps.Point(10, 34));
        }

        // Text formatting ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected _formatTextStrong(text: string): string {
            return "<span style='font-weight:normal;font-size:1.1em'>" + text + "</span>";
        }

        protected _formatEntityLink(entityType: string, entityId: string, content: string): string {
            return "<span class='page-navi' data-navi-part='" + entityType + "' data-navi-id='" + entityId + "'>" +
                content +
                "</span>";
        }

        protected _formatGoogleMapLink(displayText: string, placeId: string): string {
            return "<a href='https://www.google.com/maps/search/?api=1&query=Google&query_place_id=" + placeId + "' target='_blank'>" +
                displayText +
                "</a>";
        }

        //_formatGoogleMapLinkByCID(displayText, cid, lat, lng) {
        //  https://maps.google.com/maps?ll=53.57833,9.798127&z=15&t=m&hl=de&gl=DE&mapclient=apiv3&cid=3787428942786362895
        //    return "<a href='https://maps.google.com/maps?ll=" +
        //        lat + "," + lng +
        //        "&z=15&t=m&hl=de&gl=DE&mapclient=apiv3&cid=" + cid + "' target='_blank'>" +
        //        displayText +
        //        "</a>";
        //}

        protected _buildAddressText(street: string, zipCode: string, city: string, countryStateObj: any): string {
            let address = (street || "");
            if (zipCode || city) {
                address += ",";
                if (zipCode)
                    address += " " + zipCode;

                address += " " + (city || "???");
            }
            if (countryStateObj)
                address += ", " + countryStateObj.Code;

            return address;
        }

        // KABU TOOD: REMOVE? Not used anymore
        /*
        // GDouglasPeucker algorithm source: http://www.bdcc.co.uk/Gmaps/GDouglasPeuker.js
        // Stack-based Douglas Peucker line simplification routine 
        // returned is a reduced GLatLng array 
        // After code by  Dr. Gary J. Robinson,
        // Environmental Systems Science Centre,
        // University of Reading, Reading, UK
        _GDouglasPeucker(source, kink) {
            // source[] Input coordinates in GLatLngs
            // kink	in metres, kinks above this depth kept
            // kink depth is the height of the triangle abc where a-b and b-c are two consecutive line segments 

            var n_source, n_stack, n_dest, start, end, i, sig;
            var dev_sqr, max_dev_sqr, band_sqr;
            var x12, y12, d12, x13, y13, d13, x23, y23, d23;
            var F = ((Math.PI / 180.0) * 0.5);
            var index = new Array(); // aray of indexes of source points to include in the reduced line
            var sig_start = new Array(); // indices of start & end of working section
            var sig_end = new Array();

            // check for simple cases

            if (source.length < 3)
                return (source); // one or two points

            // more complex case. initialize stack

            n_source = source.length;
            band_sqr = kink * 360.0 / (2.0 * Math.PI * 6378137.0);	// Now in degrees
            band_sqr *= band_sqr;
            n_dest = 0;
            sig_start[0] = 0;
            sig_end[0] = n_source - 1;
            n_stack = 1;

            // while the stack is not empty  ...
            while (n_stack > 0) {

                // ... pop the top-most entries off the stacks

                start = sig_start[n_stack - 1];
                end = sig_end[n_stack - 1];
                n_stack--;

                if ((end - start) > 1) {  // any intermediate points ?

                    //... yes, so find most deviant intermediate point to
                    //    either side of line joining start & end points

                    x12 = (source[end].lng() - source[start].lng());
                    y12 = (source[end].lat() - source[start].lat());
                    if (Math.abs(x12) > 180.0)
                        x12 = 360.0 - Math.abs(x12);
                    x12 *= Math.cos(F * (source[end].lat() + source[start].lat())); // use avg lat to reduce lng
                    d12 = (x12 * x12) + (y12 * y12);

                    for (i = start + 1, sig = start, max_dev_sqr = -1.0; i < end; i++) {

                        x13 = (source[i].lng() - source[start].lng());
                        y13 = (source[i].lat() - source[start].lat());
                        if (Math.abs(x13) > 180.0)
                            x13 = 360.0 - Math.abs(x13);
                        x13 *= Math.cos(F * (source[i].lat() + source[start].lat()));
                        d13 = (x13 * x13) + (y13 * y13);

                        x23 = (source[i].lng() - source[end].lng());
                        y23 = (source[i].lat() - source[end].lat());
                        if (Math.abs(x23) > 180.0)
                            x23 = 360.0 - Math.abs(x23);
                        x23 *= Math.cos(F * (source[i].lat() + source[end].lat()));
                        d23 = (x23 * x23) + (y23 * y23);

                        if (d13 >= (d12 + d23))
                            dev_sqr = d23;
                        else if (d23 >= (d12 + d13))
                            dev_sqr = d13;
                        else
                            dev_sqr = (x13 * y12 - y13 * x12) * (x13 * y12 - y13 * x12) / d12; // solve triangle

                        if (dev_sqr > max_dev_sqr) {
                            sig = i;
                            max_dev_sqr = dev_sqr;
                        }
                    }

                    if (max_dev_sqr < band_sqr) {   // is there a sig. intermediate point ?
                        // ... no, so transfer current start point 
                        index[n_dest] = start;
                        n_dest++;
                    }
                    else {
                        // ... yes, so push two sub-sections on stack for further processing
                        n_stack++;
                        sig_start[n_stack - 1] = sig;
                        sig_end[n_stack - 1] = end;
                        n_stack++;
                        sig_start[n_stack - 1] = start;
                        sig_end[n_stack - 1] = sig;
                    }
                }
                else {
                    //... no intermediate points, so transfer current start point 
                    index[n_dest] = start;
                    n_dest++;
                }
            }

            // transfer last point
            index[n_dest] = n_source - 1;
            n_dest++;

            // make return array
            var r = new Array();
            for (i = 0; i < n_dest; i++)
                r.push(source[index[i]]);
            return r;
         }
         */
    }
}