
namespace kmodo {

    interface VicinityPaneInfo {
        name: string;
        displayName: string;
    }

    interface VicinityPlaceCategory {
        name: string;
        displayName: string;
        searchTypes: string[];
        radius: number;
        maxRadius: number;
        minItemCount?: number;
        searchText?: string;
        items: any[]
    }

    interface RetryOptions {
        func: Function;
        times: number;
        delay: number;
        retryErrorFilter: (ex: string) => boolean;
    }

    interface VicinityPlaceModel extends kendo.data.Model {
        categoryName: string;
        categoryDisplayName: string;
        isCandidate: boolean;
        // GM place search result result values:
        placeId: string;
        name: string;
        address: string;
        isDuplicateAddress: boolean;
        isDuplicateAddressCandidate: boolean;
        location: google.maps.LatLngLiteral;
        // GM distance matrix values:
        // Needed for sorting in the view: duration and distance
        duration: number;
        distance: number;
        // Will hold the GM distance matrix request result.
        dist: any;
        isDistanceError: boolean;
        distanceRequestErrorText: string;
        // GM details request values:
        phone: string;
        isDetailsError: boolean;
        detailsRequestErrorText: string;

        // NOTE: The following are not observable.
        place: google.maps.places.PlaceResult;
        marker: google.maps.Marker;
        _wasDetailsSet: boolean;
        routes: google.maps.DirectionsResult;
    }

    interface GeoMapLocationVicinityViewArgs extends ViewComponentArgs {
        contractId: string;
        projectId: string;
        projectSegmentId: string;
    }

    interface ViewModel extends ViewComponentModel {
        currentView: VicinityPaneInfo;
        views: kendo.data.ObservableArray;
    }

    export class GeoMapLocationVicinityView extends GeoMapViewBase {
        private $vicinityPanel: JQuery;
        protected args: GeoMapLocationVicinityViewArgs;
        private _isOnlyCurrentCategoryVisible: boolean;
        private _candidateVicinityPlaceZIndex: number;
        private _nonCandidateVicinityPlaceZIndex: number;
        private _selectedVicinityPlaceZIndex: number;
        private _totalDistanceRequestItems: number;
        private _selectedVicinityPlace: VicinityPlaceModel;
        private _categories: VicinityPlaceCategory[];
        private views: kendo.data.ObservableArray;
        private _allVicinityPlaces: VicinityPlaceModel[];
        private _vicinitiesViewDataSource: kendo.data.DataSource;
        private _vicinitiesView: kendo.ui.Grid;
        private _contextPlaceLocation: google.maps.LatLngLiteral;

        constructor(options: GeoMapViewOptions) {
            super(options);

            var self = this;

            this._isOnlyCurrentCategoryVisible = true;
            this._candidateVicinityPlaceZIndex = 90;
            this._nonCandidateVicinityPlaceZIndex = 80;
            this._selectedVicinityPlaceZIndex = 110;
            this._totalDistanceRequestItems = 0;
            this._selectedVicinityPlace = null;

            this._categories = [];
            var doctorTypes = ["doctor"];
            var hospitalTypes = ["hospital"];
            // KABU TODO: IMPORTANT: Clarify which initial radius to use.
            var defaultRadius = 5;
            // KABU TODO: IMPORTANT: Clarify which max radius to use.
            var defaultMaxRadius = 10000;
            var defaultMinNumPerCategory = 3;
            this._addVicinityPlaceCategory("Durchgangsarzt", "Durchgangsarzt",
                doctorTypes, defaultRadius, defaultMaxRadius, defaultMinNumPerCategory,
                "Durchgangsarzt");
            this._addVicinityPlaceCategory("EyeSpecialist", "Augenarzt",
                doctorTypes, defaultRadius, defaultMaxRadius, defaultMinNumPerCategory,
                "Augenarzt");
            this._addVicinityPlaceCategory("Dermatologist", "Hautarzt",
                doctorTypes, defaultRadius, defaultMaxRadius, defaultMinNumPerCategory,
                "Hautarzt");
            this._addVicinityPlaceCategory("GeneralPractitioner", "Allgemeinarzt",
                doctorTypes, defaultRadius, defaultMaxRadius, defaultMinNumPerCategory,
                "Allgemeinarzt");
            this._addVicinityPlaceCategory("BerufsgenossenschaftlichesKrankenhaus",
                "Berufsgenossenschaftliches Krankenhaus",
                hospitalTypes, defaultRadius, defaultMaxRadius, defaultMinNumPerCategory,
                "Berufsgenossenschaftliches Krankenhaus");
            this._addVicinityPlaceCategory("Hospital", "Hospital",
                hospitalTypes, 10, defaultMaxRadius, defaultMinNumPerCategory);

            this.views = new kendo.data.ObservableArray([]);

            this._addPaneInfo({
                name: "overview",
                displayName: "Übersicht"
            });

            for (let item of this._categories)
                self._addPaneInfo({
                    name: item.name,
                    displayName: item.displayName
                });

            // Define view model.
            this.scope = kendo.observable({
                views: this.views,
                // Show overview intially.
                currentView: this.views[0] as VicinityPaneInfo,
                generateDocument: function (e) {
                    self._generateDocumentAsync();
                },
                generateDocumentHtmlPreview: function (e) {
                    self._generateDocumentHtmlPreviewAsync();
                },
                refresh: function (e) {
                    self.refreshCore();
                }
            });
            this.getModel().bind("change", function (e) {
                if (e.field === "currentView") {
                    // Activate category or show overview.
                    self._activateView(self.getModel().currentView.name);
                }
            });

            this._allVicinityPlaces = [];
            this._vicinitiesViewDataSource = this._createVicinityListDataSource();
            this._vicinitiesView = null;
        }

        getModel(): ViewModel {
            return super.getModel() as ViewModel;
        }

        private _addPaneInfo(info: VicinityPaneInfo): void {
            this.views.push(info);
        }

        refreshWith(settings: ContextPlaceInfo): Promise<void> {
            // KABU TODO: INCONSISTENT: In GeoMapLocationView we use _contextPlaceInfo for this.
            this.args = settings;
            this._contextPlaceLocation = null;

            return this.refreshCore();
        }

        refresh(): Promise<void> {
            return this.refreshCore();
        }

        private _clearOnError(): void {
            this.clear();
            this._clearCore();
        }

        private _clearCore(): void {
            this._selectVicinityPlace(null);
            this._allVicinityPlaces = [];
            this._vicinitiesViewDataSource.data([]);
            this._totalDistanceRequestItems = 0;
            this.getModel().set("currentView", this.views[0]);
        }

        private refreshCore(): Promise<void> {
            var self = this;

            this._clearCore();

            return this._loadMap()
                .then(() => {
                    self.createView();
                    self.clear();
                    self.displayContextLocation();
                });
        }

        private displayContextLocation(): void {
            var self = this;

            if (!this.args.projectSegmentId)
                return;

            var url = "/odata/ProjectSegments/Query()?$select=Id,Number,Latitude,Longitude,Street,ZipCode&$expand=Contract($select=City;$expand=CountryState($select=Code))";
            url += "&$filter=";
            url += " Id eq " + this.args.projectSegmentId;

            cmodo.oDataQuery(url)
                .then(function (items) {
                    if (items.length === 1)
                        self.addProjectSegment(items[0]);
                });
        }

        private addProjectSegment(psegment: any): void {
            var self = this;

            if (!this._hasDataLatLong(psegment))
                return;

            this._contextPlaceLocation = {
                lat: psegment.Latitude,
                lng: psegment.Longitude
            };

            var address = this._buildAddressText(psegment.Street, psegment.ZipCode, psegment.Contract.City, psegment.Contract.CountryState);

            var psegmentLinkHtml = this._formatEntityLink("ProjectSegment", psegment.Id, this._formatTextStrong(address));

            this.addMarker({
                position: {
                    lat: psegment.Latitude,
                    lng: psegment.Longitude
                },
                //color: "#00ff00",
                title: address,
                content: psegmentLinkHtml,
                // We'll set a ZIndex lower than the vicinity place markers so they
                //   should be easier to select via mouse (I guess).
                zIndex: 0
            });

            var loc = { lat: psegment.Latitude, lng: psegment.Longitude };
            this.setMapCenter(loc);
            this.setMapZoom(10);

            var allVicinityPlaces: VicinityPlaceModel[] = [];
            var candidates: VicinityPlaceModel[] = [];

            kmodo.progress(true, this.$vicinityPanel);

            var vicinityPlaceAsyncSearches = this._categories
                .map(category =>
                    // Query places async.
                    self._findVicinityPlacesPerCategoryAsync(loc, category)
                        .then((vicinityPlacesPerCategory) => {
                            // Add all places to array when each category search finishes.
                            allVicinityPlaces.push(...vicinityPlacesPerCategory);

                            return vicinityPlacesPerCategory;
                        })
                        .then((vicinityPlacesPerCategory) => {
                            console.debug("GM vicinity places: " + vicinityPlacesPerCategory.length + " (" + category.name + ") (TOTAL: " + allVicinityPlaces.length + ")");
                        })
                        .catch(ex => {
                            console.error(ex);
                            throw ex;
                        })
                );

            // Wait for all promises to finish.
            Promise.all(vicinityPlaceAsyncSearches)
                // Query GM distance matrix for places of this category.
                .then(() => self._computeVicinityPlaceDistancesAsync(allVicinityPlaces))
                .then(() => {

                    console.debug("GM TOTAL distance matrix items: " + self._totalDistanceRequestItems);

                    // Candidates: Group by category and mark first 3 nearest places as candidates.
                    Enumerable.from(allVicinityPlaces)
                        .groupBy(x => x.categoryName)
                        .forEach(item => {
                            // Mark the first *three* nearest (by duration, distance) places as candidates.
                            Enumerable.from(item.getSource())
                                // Ignore places without distance data.
                                .where(x => !x.isDistanceError)
                                .orderBy(x => x.duration)
                                .thenBy(x => x.distance)
                                .take(3)
                                .forEach(candidate => {
                                    // Mark as candidate.
                                    candidate.set("isCandidate", true);
                                });
                        });

                    // KABU TODO: IMPORTANT: Remove places with erroneous distance results?

                    // Get all candidates.
                    candidates = Enumerable.from(allVicinityPlaces)
                        .where(x => x.isCandidate)
                        .toArray();

                    self._allVicinityPlaces.push(...allVicinityPlaces);
                })
                // Query phone numbers for all candidates.      
                .then(() => self._getVicinityPlaceDetailsAsync(candidates))
                .then(() => {

                    // Create map markers.
                    for (let p of allVicinityPlaces)
                        self._createVicinityPlaceMarker(p);

                    // Enable place events.
                    for (let p of self._allVicinityPlaces)
                        self._vicinityPlaceAttachEvents(p);
                })
                .catch(ex => {
                    console.error(ex);
                    cmodo.showError("Die Google Map Anfrage schlug ganz oder teilweise " +
                        "fehl und wurde komplett verworfen da unvollständige Daten in dieser Ansicht " +
                        "keinen Sinn machen. " +
                        "Versuchen Sie die Ansicht zu aktualisieren.");
                    self._clearOnError();
                })
                .finally(() => {
                    kmodo.progress(false, self.$view);
                });
        }

        private _generateDocumentAsync(): Promise<any> {
            var self = this;

            kmodo.progress(true, self.$view);

            return this._callGeoMapDocumentGeneratorServiceAsync(
                "/api/FlexEmailDocument/GenerateGeoMapProjectHealthInVicinityDocument")
                .then(function () {
                    cmodo.showInfo("Das Dokument wurde erstellt und gespeichert.");
                })
                .catch(function (ex) {
                    cmodo.showError("Fehler: Das Dokument konnte nicht erstellt/gespeichert werden.");
                })
                .finally(() => {
                    kmodo.progress(false, self.$view);
                });
        }

        private _generateDocumentHtmlPreviewAsync(): Promise<any> {
            var self = this;

            kmodo.progress(true, self.$view);

            return this._callGeoMapDocumentGeneratorServiceAsync(
                "/api/FlexEmailDocument/GetGeoMapProjectHealthInVicinityHtmlPreview",
                { resultDataType: "html" })
                .then(function (html) {
                    var win = window.open("", "_blank");
                    win.document.write(html);
                    win.document.close();
                    win.document.title = "Dokument - Vorschau";
                })
                .finally(() => {
                    kmodo.progress(false, self.$view);
                });
        }

        private _callGeoMapDocumentGeneratorServiceAsync(url: string, transportOptions?: any): Promise<any> {
            // Calls Web API in order to generate the final document and save it to the Mo file system.
            var self = this;

            return new Promise((resolve, reject) => {

                kmodo.progress(true, self.$view);

                // Convert vicinity places to Web API's place info.
                var apiPlaces = Enumerable.from(self._allVicinityPlaces)
                    .where(x => x.isCandidate)
                    .toArray()
                    .map(x => ({
                        CategoryName: x.categoryName,
                        Name: x.name,
                        Address: x.address,
                        Phone: x.phone,
                        Distance: x.distance,
                        DistanceText: x.dist.distance.text,
                        Duration: x.duration,
                        DurationText: x.dist.duration.text
                    }));

                var apiArgs = {
                    ContractId: self.args.contractId,
                    ProjectId: self.args.projectId,
                    ProjectSegmentId: self.args.projectSegmentId,
                    VicinityPlaces: apiPlaces
                };

                cmodo.webApiPost(url,
                    apiArgs,
                    transportOptions)
                    .then((response) => {
                        resolve(response);
                    })
                    .catch((ex) => {
                        reject(ex);
                    });

            })
                .finally(() => {
                    kmodo.progress(false, self.$view);
                });
        }

        private _getVicinityPlaceDetailsAsync(vicintyPlaces: VicinityPlaceModel[]): Promise<void> {
            var self = this;

            // Observed GM's limiting behavior:
            // An unknown number of first requests will succeed in parallel.
            // Max 10. (observed: sotimes only 7, sometimes 9, etc.)
            //
            // Strategy:
            // Start executing all requests sequentially without a delay.
            // After first error: wait one second and retry.
            // Proceed with rest of requests.

            const delay = 1500;
            const retryTimes = 2;

            const detailsRequest = (vicinityPlace: VicinityPlaceModel) => self._queryPlaceDetails(
                {
                    placeId: vicinityPlace.placeId,
                    fields: ["formatted_phone_number"]
                })
                .then((placeDetails: google.maps.places.PlaceResult) => {
                    self._setVicinityPlaceDetails(vicinityPlace, placeDetails);
                });

            const requestWithDelayedRetry = (place: VicinityPlaceModel) =>
                self._retryAsync(
                    {
                        func: () => detailsRequest(place),
                        times: retryTimes,
                        delay: delay,
                        retryErrorFilter: (ex) => ex === "OVER_QUERY_LIMIT"
                    });

            return vicintyPlaces.reduce(
                (p, vicinityPlace) => p.then(() => requestWithDelayedRetry(vicinityPlace)),
                Promise.resolve());
        }

        // KABU TODO: REMOVE? Not used
        //private _queryMultiplePlaceDetails(vicinityPlaces: VicinityPlaceModel[]): Promise<void[]> {
        //    // KABU TODO: Doesn't work with referer restrictions.
        //    const url = "https://maps.googleapis.com/maps/api/place/details/json?";

        //    var self = this;

        //    return Promise.all(vicinityPlaces.map(place =>
        //        self._queryGooleMapWebApi(url + "placeid=" + place.placeId + "&fields=formatted_phone_number")
        //            .then(response => {
        //                if (response.status === "OK") {
        //                    console.debug("GM got Details");
        //                }
        //            })
        //    ));
        //}

        /*
        private _queryGooleMapWebApi(url: string): Promise<any> {
            // KABU TODO: Doesn't work because this GM API does not work
            // with referer restrictions. We have restricted GM to our websites.
            // Thus using this makes only sense in a server scenario with IP restrictions.
            return new Promise((resolve, reject) => {
                url += "&key=" + kmodo.googleMapInitializer._key;
                $.ajax({
                    url: url,
                    type: "GET",
                    crossDomain: true,
                    dataType: "json",
                    success: function (response) {
                        resolve(response);
                    },
                    error: function (xhr, status) {
                        reject(status);
                    }
                });
            });
        };
        */

        private _activateView(viewName: string): void {
            var self = this;

            // Activate category or show overview.

            this._selectVicinityPlace(null);

            // Clear vicinity list in panel.
            this._vicinitiesViewDataSource.data([]);

            if (viewName === "overview") {

                // Show all markers.
                for (let marker of this._getAllVicinityPlaceMarkers())
                    marker.setVisible(true);
            }
            else {

                // Validate places (e.g. duplicate addresses).
                this._validateVicinityPlacesOfCategory(viewName);

                var vicinityPlacesOfCategory = this._allVicinityPlaces.filter(x => x.categoryName === viewName);

                // Set current vicinity list data in panel.
                this._vicinitiesViewDataSource.data(vicinityPlacesOfCategory);

                // Show only markers of current category.
                if (this._isOnlyCurrentCategoryVisible) {

                    for (let marker of this._getAllVicinityPlaceMarkers()) {
                        if (self._getMarkerVicinityPlace(marker).categoryName === viewName)
                            marker.setVisible(true);
                        else
                            marker.setVisible(false);
                    }
                }
            }
        }

        private _validateVicinityPlacesOfCategory(categoryName: string): void {
            // Validate places (e.g. duplicate addresses).

            var vicinityPlacesOfCategory = this._allVicinityPlaces.filter(x => x.categoryName === categoryName);

            // Find places with duplicate addresses.
            var placesByAddress = Enumerable.from(vicinityPlacesOfCategory)
                .groupBy(x => x.address)
                .toArray();

            for (let item of placesByAddress) {

                var placesOfAddress = item.getSource();

                // Duplicate addresses will be shown with an orange background.
                var isDuplicateAddress = placesOfAddress.length > 1;
                for (let x of placesOfAddress) {
                    x.set("isDuplicateAddress", isDuplicateAddress);
                    x.set("isDuplicateAddressCandidate", false);
                }

                // Duplicate addresses in *candidates* will be shown with a red background.
                var candidatePlacesOfAddress = placesOfAddress.filter(x => x.isCandidate);
                var isDuplicateAddressCandidate = candidatePlacesOfAddress.length > 1;
                for (let x of candidatePlacesOfAddress)
                    x.set("isDuplicateAddressCandidate", isDuplicateAddressCandidate);
            }
        }

        private _getAllVicinityPlaceMarkers(): google.maps.Marker[] {
            var self = this;
            return Enumerable.from(this._locationMarkers)
                .where(marker => self._getMarkerVicinityPlace(marker) !== null)
                .toArray();
        }

        private _addVicinityPlaceCategory(name: string, displayName: string, searchTypes: string[],
            radius: number, maxRadius: number,
            minItemCount?: number, searchText?: string): void {

            var category: VicinityPlaceCategory = {
                name: name,
                displayName: displayName,
                searchTypes: searchTypes,
                searchText: searchText,
                radius: radius,
                maxRadius: maxRadius,
                minItemCount: minItemCount,
                items: []
            };
            this._categories.push(category);
        }

        private _findVicinityPlacesPerCategoryAsync(
            contextLocation: google.maps.LatLngLiteral,
            category: VicinityPlaceCategory)
            : Promise<VicinityPlaceModel[]> {

            var self = this;

            return new Promise(function (resolve, reject) {

                // Find places in vicinity of the context place.

                var queryStrategy = "textSearch";

                // See https://developers.google.com/maps/documentation/javascript/reference/places-service
                var placesService = new google.maps.places.PlacesService(self.map);

                var search = (cat: VicinityPlaceCategory, radius: number) => {

                    // See https://developers.google.com/maps/documentation/javascript/reference/places-service#TextSearchRequest
                    var request = {
                        location: contextLocation,
                        // NOTE: Using "textSearch" the radius acts as a bias rather than a real restriction.
                        //   I.e. results won't be restricted to this radius.
                        radius: radius, // Used by "nearbySearch" only.
                        // https://developers.google.com/places/supported_types
                        type: cat.searchTypes,
                        query: cat.searchText, // Used by "textSearch" only.
                        keyword: cat.searchText // Used by "nearbySearch" only.
                    };

                    var queryFunc = placesService.textSearch;
                    if (queryStrategy === "nearbySearch")
                        queryFunc = placesService.nearbySearch;

                    // textSearch or nearbySearch
                    queryFunc.apply(placesService,
                        [request, function (
                            searchResultPlaces: google.maps.places.PlaceResult[],
                            status: google.maps.places.PlacesServiceStatus) {

                            if (status !== google.maps.places.PlacesServiceStatus.OK &&
                                status !== google.maps.places.PlacesServiceStatus.ZERO_RESULTS) {

                                console.error("GM PlacesService.textSearch(): ERROR: " + status +
                                    ", category: " + category.displayName);
                                reject(status);
                            }
                            else {
                                if (queryStrategy === "textSearch" ||
                                    (searchResultPlaces.length >= cat.minItemCount || radius >= cat.maxRadius)) {

                                    // Save radius which sastisfied the min item count rule.
                                    category.radius = radius;

                                    // Create vicinity place objects from results.
                                    var vicinityPlacesPerCategory = searchResultPlaces
                                        .map(place => self._createVicinityPlaceCore(category, place));

                                    resolve(vicinityPlacesPerCategory);
                                }
                                else {
                                    // Retry with a greater radius.
                                    search(cat, Math.min(radius * 2, cat.maxRadius));
                                }
                            }
                        }]);
                };

                search(category, category.radius);
            });
        }

        private _computeVicinityPlaceDistancesAsync(vicinityPlaces: VicinityPlaceModel[])
            : Promise<VicinityPlaceModel[]> {

            var self = this;

            const _sleep = (ms: number) =>
                new Promise(resolve => {
                    setTimeout(resolve, ms);
                });

            // 1) Query with chunks of specific size.
            // 2) Handle server errors.
            // 3) Handle OVER_QUERY_LIMIT:
            //    - Compensate "request per second" limitation: wait specific amount of time and
            //      retry the request.

            // OVER_QUERY_LIMIT: Apparently we can only query 100 destinations per second.
            const maxNumPerInterval = 100;
            const intervalDelay = 1000;
            const maxRetries = 5;

            const requestPerInterval = (interval: number, vicinityPlaces: VicinityPlaceModel[]) =>
                _sleep(interval)
                    .then(() => self._retryAsync(
                        {
                            func: () => self._perChunkDistanceMatrixRequestAsync(vicinityPlaces),
                            times: maxRetries,
                            delay: intervalDelay,
                            retryErrorFilter: (ex) => ex === "OVER_QUERY_LIMIT"
                        }));

            return new Promise(function (resolve, reject) {

                if (vicinityPlaces.length === 0) {
                    resolve(vicinityPlaces);
                    return;
                }

                // NOTE: "Requests per interval" are executed *sequentially*.
                self._toChunkedList(vicinityPlaces, maxNumPerInterval)
                    // Aggregate into single promise chain.
                    .reduce(
                        (p, items, i) => p.then(() => requestPerInterval(i !== 0 ? intervalDelay : 0, items)),
                        Promise.resolve())
                    .then(() => {
                        resolve(vicinityPlaces);
                    })
                    .catch(ex => {
                        reject(ex);
                    });
            });
        }

        private _perChunkDistanceMatrixRequestAsync(vicinityPlaces: VicinityPlaceModel[]):
            Promise<VicinityPlaceModel[]> {

            var self = this;

            // Docu: "MAX_DIMENSIONS_EXCEEDED — Your request contained more than 25 origins,
            //        or more than 25 destinations."
            var maxNumPerRequest = 25;
            var maxRetries = 5;
            var retryDelay = 1000;

            return new Promise(function (resolve, reject) {

                // Clear previous distance error values.
                for (let item of vicinityPlaces) {
                    item.set("isDistanceError", false);
                    item.set("distanceRequestErrorText", null);
                }

                var promises = self._toChunkedList(vicinityPlaces, maxNumPerRequest)
                    .map(places =>
                        self._retryAsync(
                            {
                                func: () => self._computeVicinityPlaceDistancesCoreAsync(places),
                                times: maxRetries,
                                delay: retryDelay,
                                retryErrorFilter: (ex) => ex === "OVER_QUERY_LIMIT"
                            })
                            .then((places: VicinityPlaceModel[]) => {
                                self._totalDistanceRequestItems += places.length;
                            })
                    );

                Promise.all(promises)
                    .then(() => {
                        resolve(vicinityPlaces);
                    })
                    .catch(ex => {
                        reject(ex);
                    });
            });
        }

        private _retryAsync(options: RetryOptions): Promise<any> {

            return new Promise(function (resolve, reject) {
                var times = options.times;
                var error: any;
                var attemptIndex: number = 0;
                var attempt = function () {
                    if (times === 0) {
                        reject(error);
                    } else {
                        if (attemptIndex > 0)
                            console.debug("GM request RETRYING: attempt " + (attemptIndex + 1));

                        options.func()
                            .then(resolve)
                            .catch(function (ex) {
                                if (!options.retryErrorFilter(ex)) {
                                    reject(ex);
                                    return;
                                }

                                times--;
                                attemptIndex++;
                                error = ex;
                                setTimeout(function () { attempt(); }, options.delay);
                            });
                    }
                };
                attempt();
            });
        }

        private _toChunkedList(items: VicinityPlaceModel[], size: number): VicinityPlaceModel[][] {

            var list: VicinityPlaceModel[][] = [];
            for (let i = 0; i < items.length; i += size)
                list.push(items.slice(i, i + size));

            return list;
        }

        private _computeVicinityPlaceDistancesCoreAsync(vicinityPlaces: VicinityPlaceModel[])
            : Promise<VicinityPlaceModel[]> {

            var self = this;

            // DistanceMatrixService: 
            // https://developers.google.com/maps/documentation/javascript/reference/distance-matrix
            // https://developers.google.com/maps/documentation/javascript/distancematrix

            // NOTE: "Limited to 100 elements per client-side request."
            //   "number of origins times the number of destinations defines the number of elements."
            //   "Shared daily free quota of 100,000 elements per 24 hours"

            // NOTE: Thus we can query 100 vicinity places per category. 100.000 elements per day.

            // KABU TODO: Evaluate if we have to shrink the provided number of @vicinityPlaces to 100.
            //   This shouldn't be a problem because we won't get more
            //   than 100 doctors per category, right?

            return new Promise(function (resolve, reject) {

                if (!vicinityPlaces.length) {
                    resolve(vicinityPlaces);
                    return;
                }

                self._getDistanceMatrixService().getDistanceMatrix({
                    origins: [self._contextPlaceLocation],
                    destinations: vicinityPlaces.map(x => x.location),
                    travelMode: google.maps.TravelMode.DRIVING,
                    avoidFerries: true
                }, function (
                    response: google.maps.DistanceMatrixResponse,
                    status: google.maps.DistanceMatrixStatus) {

                        if (status !== google.maps.DistanceMatrixStatus.OK) {

                            console.debug("GM getDistanceMatrix(): ERROR: " + status +
                                ", num places: " + vicinityPlaces.length);

                            // Request failed. Set error message.
                            for (let item of vicinityPlaces) {
                                item.set("isDistanceError", true);
                                item.set("distanceRequestErrorText", "Distanzabfrage schlug fehl");
                            }

                            reject(status);
                        }
                        else {
                            console.debug("GM getDistanceMatrix(): OK (num: " + vicinityPlaces.length + ")");

                            // Response: https://developers.google.com/maps/documentation/javascript/distancematrix#distance_matrix_responses
                            // First row of the response holds the result of the first origin location.
                            var row = response.rows[0];
                            var elements = row.elements;
                            var destinationPlace: VicinityPlaceModel = null;
                            elements.forEach((element: google.maps.DistanceMatrixResponseElement, idx: number) => {

                                // Status codes: https://developers.google.com/maps/documentation/javascript/distancematrix#distance_matrix_status_codes
                                if (element.status === google.maps.DistanceMatrixElementStatus.OK) {

                                    destinationPlace = vicinityPlaces[idx];
                                    destinationPlace.set("duration", element.duration.value);
                                    destinationPlace.set("distance", element.distance.value);
                                    destinationPlace.set("dist", {
                                        distance: element.distance,
                                        duration: element.duration
                                    });
                                }
                                else {
                                    destinationPlace.set("isDistanceError", true);
                                    destinationPlace.set("distanceRequestErrorText", "Distanz ist unbekannt");
                                }
                                // Example response element:
                                // "status": "OK",
                                // "duration": {
                                //    "value": 70778,
                                //    "text": "19 hours 40 mins"
                                // },
                                // "distance": {
                                //    "value": 1887508,
                                //    "text": "1173 mi"
                                // }
                            });

                            resolve(vicinityPlaces);
                        }
                    });
            });
        }

        private _getMarkerVicinityPlace(marker: google.maps.Marker): VicinityPlaceModel {
            return this.getMarkerCustomData(marker) ? (this.getMarkerCustomData(marker).vicinityPlace || null) : null;
        }

        private _setVicinityPlaceDetails(
            vicinityPlace: VicinityPlaceModel,
            placeDetails: google.maps.places.PlaceResult): void {

            // TODO: REMOVE: vicinityPlace.set("address", placeDetails.formatted_address);
            vicinityPlace.set("phone", placeDetails.formatted_phone_number);
            vicinityPlace._wasDetailsSet = true;
        }

        private _createVicinityPlaceCore(
            category: VicinityPlaceCategory,
            place: google.maps.places.PlaceResult)
            : VicinityPlaceModel {

            // Creates the vicinity place VM and initializes its map marker.

            var location = place.geometry.location;
            var vicinityPlace = new kendo.data.Model({
                id: cmodo.guid(),
                categoryName: category.name,
                categoryDisplayName: category.displayName,
                isCandidate: false,
                // GM place search result result values:
                placeId: place.place_id,
                name: place.name,
                address: place.formatted_address,
                isDuplicateAddress: false,
                isDuplicateAddressCandidate: false,
                location: { lat: location.lat(), lng: location.lng() },
                // GM distance matrix values:
                // Needed for sorting in the view: duration and distance
                duration: 0,
                distance: 0,
                // Will hold the GM distance matrix request result.
                dist: null,
                isDistanceError: false,
                distanceRequestErrorText: null,
                // GM details request values:
                phone: null, //
                isDetailsError: false,
                detailsRequestErrorText: null,
                sdfsd: ""
            }) as VicinityPlaceModel;

            vicinityPlace.place = place;
            vicinityPlace._wasDetailsSet = false;

            return vicinityPlace;
        }

        private _vicinityPlaceAttachEvents(vicinityPlace: VicinityPlaceModel): void {
            var self = this;
            vicinityPlace.bind("change", function (e) {
                if (e.field === "isCandidate") {
                    if (vicinityPlace.isCandidate && !vicinityPlace._wasDetailsSet) {
                        // Query details (phone number).
                        self._getVicinityPlaceDetailsAsync([vicinityPlace]);
                    }
                    self._updateVicinityPlaceViewState(vicinityPlace);
                    self._validateVicinityPlacesOfCategory(self.getModel().currentView.name);
                }
            });
        }

        private _getVicinityPlaceMarkerColor(vicinityPlace: VicinityPlaceModel): string {
            // Display in blue if selected.
            if (this._isSelected(vicinityPlace))
                return "#3379b5";
            else
                return vicinityPlace.isCandidate ? "#ff0000" : "#ff9600";
        }

        private _getVicinityPlaceMarkerOpacity(vicinityPlace: VicinityPlaceModel): number {
            if (this._isSelected(vicinityPlace))
                return 1;
            else
                return 0.4;
        }

        private _createVicinityPlaceMarker(vicinityPlace: VicinityPlaceModel): void {
            var self = this;

            // Create map marker for vicinity place.
            var markerOptions = this._getMarkerOptions({
                title: vicinityPlace.categoryDisplayName,
                label: null, //category.displayName,
                position: vicinityPlace.location,
                symbol: google.maps.SymbolPath.CIRCLE.toString(),
                color: this._getVicinityPlaceMarkerColor(vicinityPlace),
                customData: {
                    vicinityPlace: vicinityPlace
                },
                zIndex: this._getVicinityPlaceZIndex(vicinityPlace)
            });
            var marker = new google.maps.Marker(markerOptions);
            this._trackLocationMarker(marker);

            vicinityPlace.marker = marker;

            // On marker click: show info window and route.
            google.maps.event.addListener(marker, 'click', (e) => {
                self._selectVicinityPlace(self._getMarkerVicinityPlace(marker));
            });
        }

        private _selectVicinityPlaceInPanel(vicinityPlace: VicinityPlaceModel): void {

            // Change category in vicinity panel.
            var categoryName = vicinityPlace.categoryName;
            if (!this.getModel().currentView || categoryName !== this.getModel().currentView.name) {
                this.getModel().set("currentView",
                    this.getModel().views.find((x: VicinityPaneInfo) => x.name === categoryName));
            }

            // Select place in vicinities panel.
            var gridView = this._vicinitiesView;

            var selectedPlace = gridView.dataItem(gridView.select()) as VicinityPlaceModel;

            if (!selectedPlace || selectedPlace.id !== vicinityPlace.id) {

                gridView.items().each((idx, elem) => {
                    var dataItem = gridView.dataItem(elem) as VicinityPlaceModel;
                    //some condition
                    if (dataItem.id === vicinityPlace.id) {
                        gridView.select(elem);
                        return false;
                    }
                    return true;
                });
            }
        }

        private _showVicinityPlaceRoute(vicinityPlace: VicinityPlaceModel): void {
            var self = this;

            // Show route from context place to vicinity place.
            if (vicinityPlace.routes) {
                this._showRoutes(vicinityPlace.routes);
            }
            else {
                this._queryRouteAsync(this._contextPlaceLocation, vicinityPlace.location)
                    .then(function (routes) {
                        vicinityPlace.routes = routes;
                        self._showRoutes(vicinityPlace.routes);
                    });
            }
        }

        private _updateVicinityPlaceViewState(vicinityPlace: VicinityPlaceModel): void {
            var marker = vicinityPlace.marker;
            if (marker) {
                marker.setZIndex(this._getVicinityPlaceZIndex(vicinityPlace));
                marker.setIcon(this._getMarkerSymbolOptions({
                    color: this._getVicinityPlaceMarkerColor(vicinityPlace),
                    opacity: this._getVicinityPlaceMarkerOpacity(vicinityPlace)
                }));
            }
        }

        private _isSelected(vicinityPlace: VicinityPlaceModel): boolean {
            return this._selectedVicinityPlace && this._selectedVicinityPlace.id === vicinityPlace.id;
        }

        private _getVicinityPlaceZIndex(vicinityPlace: VicinityPlaceModel): number {
            if (this._isSelected(vicinityPlace))
                return this._selectedVicinityPlaceZIndex;
            else
                return vicinityPlace.isCandidate
                    ? this._candidateVicinityPlaceZIndex
                    : this._nonCandidateVicinityPlaceZIndex;
        }

        // KABU TODO: REMOVE
        //_openVicinityPlaceMarkerInfoWindow(marker) {
        //    var vicinityPlace = this.getMarkerCustomData(marker).vicinityPlace;
        //    var content = this._getVicinityPlaceInfoHtml(vicinityPlace);
        //    this._openMarkerInfoWindow(marker, content);
        //};

        private _selectVicinityPlace(vicinityPlace: VicinityPlaceModel): void {

            var prev = this._selectedVicinityPlace;
            this._selectedVicinityPlace = vicinityPlace;
            if (prev) {
                // Revert visuals of previously selected place.
                this._updateVicinityPlaceViewState(prev);
                // Remove routes.
                this._clearRoutes();
            }

            if (vicinityPlace) {
                this._updateVicinityPlaceViewState(vicinityPlace);

                // Show route.
                this._showVicinityPlaceRoute(vicinityPlace);
                // KABU TODO: REMOVE
                // Open info window.
                //this._openVicinityPlaceMarkerInfoWindow(vicinityPlace.marker);

                // Select in vicinity list panel.
                this._selectVicinityPlaceInPanel(vicinityPlace);
            }
        }

        private _createVicinityListDataSource(): kendo.data.DataSource {
            return new kendo.data.DataSource({
                data: [],
                schema: {
                    model: {
                        id: "id",
                        fields: {
                            name: {},
                            distance: { type: "number" },
                            duration: { type: "number" },
                        }
                    }
                },
                sort: this._getVicinityPlacesSortOptions()
            });
        }

        private _getVicinityPlacesSortOptions(): kendo.data.DataSourceSortItem[] {
            return [
                { field: "duration", dir: "asc" },
                //{ field: "distance", dir: "asc" }
            ];
        }

        createView(): void {
            if (this._isComponentInitialized) return;
            this._isComponentInitialized = true;

            this.$view = $("#geo-map-view-" + this._options.id);

            this.initBasicComponents();

            kendo.bind(this.$view.find(".geo-map-header"), this.getModel());

            this._createVicinityPanel();

            this._initMap();
        }

        private _createVicinityPanel(): void {
            var self = this;

            this.$vicinityPanel = this.$view.find(".geo-map-vicinity-panel");

            kendo.bind(this.$vicinityPanel, this.scope);

            this._vicinitiesView = this.$vicinityPanel.find(".geo-map-vicinity-grid").kendoGrid({
                dataSource: this._vicinitiesViewDataSource,
                selectable: "row",
                scrollable: false,
                groupable: false,
                sortable: false,
                pageable: false,
                columns: [{
                    template: (data) => self._getVicinityListRowHtml(data),
                    field: "name",
                    title: ""
                }],
                change: (e) => {
                    let $row = e.sender.select();
                    let dataItem = e.sender.dataItem($row) as VicinityPlaceModel;
                    self._selectVicinityPlace(dataItem);
                },
                dataBound: (e) => {
                    // Data-bind all rows.
                    kmodo.foreachGridRow(e.sender, ($row, dataItem) => {
                        kendo.bind($row, dataItem);
                    });
                }
            }).data("kendoGrid");

            // Hide header.
            this._vicinitiesView.element.find(".k-grid-header").hide();

            // KABU TODO: REMOVE? Tried to use kendoListView instead of kendoGrid.
            //this._vicinitiesView = this.$view.find(".geo-map-vicinity-grid").kendoListView({
            //    selectable: true,
            //    dataSource: this._vicinitiesViewDataSource,
            //    template: (data) => self._getVicinityListRowHtml(data),
            //    change: function () {
            //        var $row = this.select();
            //        var dataItem = this.dataItem($row);
            //        self._selectVicinityPlace(dataItem);
            //    },
            //    dataBound: function (e) {
            //        var view = this;
            //        view.element.children("div").each(function (idx, elem) {
            //            var $row = $(this);
            //            kendo.bind($row, view.dataItem($row));
            //        });
            //    }
            //}).data("kendoListView");
            // KABU TODO: Disabling selection of the row when a check-box is
            //   on that row is clicked doesn't seem to be possible :-(
            // The following attemmpts do not work. The row is always being selected.
            //this._vicinitiesView.element.on('mousedown', 'input[type="checkbox"], label', function (e) {
            //    //e.stopPropagation();
            //    //e.stopImmediatePropagation();
            //});
            //this._vicinitiesView.element.on('mousedown', '>div', function (e) {
            //    if ($(e.target).is("label.k-checkbox-label")) {
            //        e.stopPropagation();
            //        e.stopImmediatePropagation();
            //        return false;
            //    }    
            //});
        }

        private _getVicinityListRowHtml(vicinityPlace: VicinityPlaceModel): string {

            var html = "<div style='background-color:white;color:black;margin-top:1px;margin-bottom:1px;padding:3px'>" +
                //var html = "<div style='padding:3px'>" +
                // Check-box for "isCandidate":
                "<input id='check-" + vicinityPlace.id + "' class='k-checkbox' type='checkbox' data-bind='checked: isCandidate' />" +
                "<label for='check-" + vicinityPlace.id + "' class='k-checkbox-label' style='display:inline-block'></label>" +
                // Category of place
                //"<span style='font-weight:bold'>" + vicinityPlace.categoryDisplayName + "</span>" + "<br/>" +
                // Place info
                this._getVicinityPlaceInfoHtml(vicinityPlace) +
                "</div>";

            return html;
        }

        private _getVicinityPlaceInfoHtml(vicinityPlace: VicinityPlaceModel): string {

            var placeId = vicinityPlace.placeId;

            var html = "<span style='font-weight:bold'>" + vicinityPlace.name + "</span><br/>";

            html += "<div style='margin-top:3px'>";

            var colorStyle = "";
            if (vicinityPlace.isDuplicateAddressCandidate)
                colorStyle = "background-color:#fe7676";
            else if (vicinityPlace.isDuplicateAddress) // fe7676 // fff094
                colorStyle = "background-color:#fff094";

            html += "<span style='" + colorStyle + "'>" + vicinityPlace.address + "</span><br/>";

            html += "Telefon: " + (vicinityPlace.phone || "");

            if (vicinityPlace.dist) {
                var dist = vicinityPlace.dist;
                html +=
                    "<br/>Fahrzeit: <span style='font-weight:bold;background-color:yellow'>" + dist.duration.text + "</span>" +
                    "<br/>Enfernung: <span style='font-weight:bold'>" + dist.distance.text + "</span>";
            }

            html += "<br/>" + this._formatGoogleMapLink("In Google Map öffnen", placeId);

            html += "</div>";

            return this._formatTextStrong(html);
        }
    }
}


