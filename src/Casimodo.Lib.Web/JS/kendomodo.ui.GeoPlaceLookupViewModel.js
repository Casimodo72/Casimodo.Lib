"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var GoogleMapInitializer = (function (_super) {
            casimodo.__extends(GoogleMapInitializer, _super);

            function GoogleMapInitializer(options) {
                _super.call(this, options);

                this.isInitialized = false;
            }

            var fn = GoogleMapInitializer.prototype;

            fn.init = function () {
                var self = this;

                if (!this.isInitialized) {
                    //$.getScript("https://maps.googleapis.com/maps/api/js?v=3.22&language=de&region=DE&libraries=places&callback=kendomodo.ui.GoogleMapInitializer.onScriptReady&key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4");

                    $.getScript("https://maps.googleapis.com/maps/api/js?v=3.22&language=de&region=DE&libraries=places&key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4",
                        function (e) {
                            self.onScriptReady();
                        });
                }
                else
                    this.onScriptReady();
            };

            fn.onScriptReady = function () {
                this.isInitialized = true;
                this.trigger("scriptReady", { sender: this });
            };

            return GoogleMapInitializer;
        })(casimodo.ObservableObject);
        ui.GoogleMapInitializer = new GoogleMapInitializer();

        var GeoPlaceLookupViewModel = (function (_super) {
            casimodo.__extends(GeoPlaceLookupViewModel, _super);

            function GeoPlaceLookupViewModel(options) {
                _super.call(this, options);

                this.$view = null;
                this.map = null;
                this.marker = null;
                this.infoWindow = null;
                this._dialogWindow = null;
                this._isComponentInitialized = false;
                this.scope.item = kendo.observable(new kendomodo.ui.GeoPlaceInfo());
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

            fn.getCurrentItem = function () {
                return this.scope.item;
            };

            fn.findInitialPlace = function () {
                var address = this.getCurrentItem().getDisplayAddress();
                if (address) {
                    this.$searchInput.val(address);
                    this.findPlaceByAddress(address);
                }
            }

            fn.initAutoComplete = function () {
                var self = this;
                var map = this.map;

                // Create the search box and link it to the UI element.

                // https://developers.google.com/maps/documentation/javascript/reference#AutocompleteOptions
                var options = {
                    componentRestrictions: {
                        // Restrict to Germany.
                        country: 'DE'
                    }
                    // https://developers.google.com/places/supported_types#table3
                    //types: ["geocode"]
                };

                var input = this.$searchInput[0];
                var searchBox = new google.maps.places.Autocomplete(input); //, options);
                //var searchBox = new google.maps.places.SearchBox(input);
                map.controls[google.maps.ControlPosition.TOP_LEFT].push(input);

                // Bias the SearchBox results towards current map's viewport.
                //map.addListener('bounds_changed', function () {
                //    searchBox.setBounds(map.getBounds());
                //});

                // Listen for the event fired when the user selects a prediction and retrieve
                // more details for that place.
                searchBox.addListener('place_changed', function () {
                    var place = searchBox.getPlace();
                    if (place.geometry)
                        self.applyPlace(place);
                    else
                        self.findPlaceByAddress(place.name);
                });
            };

            fn.findPlaceByAddress = function (address) {
                var self = this;

                (new google.maps.Geocoder()).geocode({
                    'address': address,
                    componentRestrictions: {
                        // Restrict to Germany.
                        country: 'DE'
                    }
                }, function (results, status) {

                    if (status !== google.maps.GeocoderStatus.OK) {
                        alert("Ich kann diesen Ort nicht finden. " + status);
                        return;
                    }

                    self.applyPlace(results[0]);
                });
            };

            fn.applyPlace = function (item) {

                var map = this.map;
                var marker = this.marker;
                var infoWindow = this.infoWindow;

                var geometry = item.geometry;
                var location = geometry.location;
                var lat = location.lat().toFixed(6);
                var lng = location.lng().toFixed(6);

                // Set place on view model.        
                this.getCurrentItem().buildFromGoogleMapsPlace(item);

                map.setCenter(location);
                marker.setPosition(location);
                map.setZoom(12);

                if (this.infoWindow)
                    this.infoWindow.close();

                var info = item.formatted_address + " (" + lat + ", " + lng + ")";
                this.infoWindow.setContent(info);

                this.infoWindow.open(map, marker);
            }

            fn._initMap = function () {
                if (this._isMapInitialized)
                    return true;

                this._isMapInitialized = true;

                var hamburg = new google.maps.LatLng(53.550370, 9.994161);

                // MapTypeId.ROADMAP displays the default road map view. This is the default map type.
                // MapTypeId.SATELLITE displays Google Earth satellite images
                // MapTypeId.HYBRID displays a mixture of normal and satellite views
                // MapTypeId.TERRAIN displays a physical map based on terrain information.
                var mapTypeId = google.maps.MapTypeId.ROADMAP;

                var options = {
                    zoom: 8,
                    center: hamburg,
                    mapTypeId: mapTypeId,
                    overviewMapControl: true,
                    overviewMapControlOptions: { opened: true }
                    //heading: 90,
                    //tilt: 45,
                };

                var map = this.map = new google.maps.Map(this.$mapContainer[0], options);

                var marker = this.marker = new google.maps.Marker({
                    map: map,
                    // Define the place with a location, and a query string.
                    // place: { location: hamburg, query: 'Hamburg'},
                    // Attributions help users find your site again.
                    attribution: {
                        source: 'Google Maps JavaScript API',
                        webUrl: 'https://developers.google.com/maps/'
                    }
                });

                // Construct a new InfoWindow.
                var infoWindow = this.infoWindow = new google.maps.InfoWindow({
                    // content: 'Google Hamburg'
                });

                // Opens the InfoWindow when marker is clicked.
                //marker.addListener('click', function () {
                //    infoWindow.open(map, marker);
                //});

                //google.maps.event.addListener(marker, 'click', function () {
                //    infowindow.open(map, marker);
                //});

                //google.maps.event.addListener(map, 'mousemove', function (event) {
                //    var coord = event.latLng;
                //    $coordinatesDisplay.text("(" + coord.lat().toFixed(6) + ", " + coord.lng().toFixed(6) + ")");
                //});

                this.initAutoComplete();               

                // To enable sign-in on a map created with the Google Maps JavaScript API,
                // load v3.18 or later of the API with the additional signed_in=true parameter.
                // Requires cookies
                // signed_in=true
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

                this.$view = $("#google-map-view-" + this._options.id);

                this.$addressInfo = this.$view.find("div.address-info");
                this.$coordinatesDisplay = this.$view.find(".map-coordinates");
                this.$mapContainer = this.$view.find(".google-map");
                this.$searchInput = this.$view.find(".pac-input");

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

        })(kendomodo.ui.ComponentViewModel);
        ui.GeoPlaceLookupViewModel = GeoPlaceLookupViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));