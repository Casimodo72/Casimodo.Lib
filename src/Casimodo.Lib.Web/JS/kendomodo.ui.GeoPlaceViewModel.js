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
                    $.getScript("https://maps.googleapis.com/maps/api/js?v=3.22&language=de&region=DE&libraries=places&callback=kendomodo.ui.GoogleMapInitializer.onScriptReady&key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4");

                    //$.getScript("https://maps.googleapis.com/maps/api/js?v=3.22&language=de&region=DE&libraries=places&key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4",
                    //    function (e) {
                    //        self.onScriptReady();
                    //    });
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

        ui.GeoPlaceEditorInfo = (function () {

            var GeoPlaceEditorInfo = function (vm) {
                this.vm = vm;
                this.PlaceInfo = null;
                this._map = {
                    Street: "Street",
                    ZipCode: "ZipCode",
                    City: "City",
                    CountryStateId: "CountryStateId",
                    CountryId: "CountryId",
                    Longitude: "Longitude",
                    Latitude: "Latitude"
                };
                this.build();
            };

            var fn = GeoPlaceEditorInfo.prototype;

            fn.map = function (map) {
                this._map = $.extend(this._map, map);
                this.build();
            };

            fn.build = function () {
                var vm = this.vm;
                var place = this.PlaceInfo || (this.PlaceInfo = new kendomodo.ui.GeoPlaceInfo());
                var m = this._map;
                // NOTE: We don't have a street number property on entities.
                this._set(vm, m.Street, place);
                this._set(vm, m.ZipCode, place);
                this._set(vm, m.City, place);
                this._set(vm, m.CountryStateId, place, "StateLong", geoassistant.CountryStateKeys.getDisplayNameById);
                this._set(vm, m.CountryId, place, "CountryLong", geoassistant.CountryKeys.getDisplayNameById);
                this._set(vm, m.Longitude, place);
                this._set(vm, m.Latitude, place);
            };

            fn.applyChanges = function () {
                var vm = this.vm;
                var place = this.PlaceInfo;
                var m = this._map;
                // NOTE: We don't have a street number property on entities.
                place.Street = (place.Street || "") + (place.StreetNumber ? " " + place.StreetNumber : "");
                _apply(vm, m.Street, place);
                _apply(vm, m.ZipCode, place);
                _apply(vm, m.City, place);
                _apply(vm, m.CountryStateId, place, "StateShort", geoassistant.CountryStateKeys.getIdByCode);
                _apply(vm, m.CountryId, place, "CountryShort", geoassistant.CountryKeys.getIdByCode);
                _apply(vm, m.Longitude, place);
                _apply(vm, m.Latitude, place);
            };

            fn._set = function (source, sourceProp, target, targetProp, converter) {
                //debugger;
                //sourceProp = !this.map
                //    ? sourceProp
                //    : !this.sourcePropMap[sourceProp]
                //        ? sourceProp
                //        : this.sourcePropMap[sourceProp];

                target[targetProp || sourceProp] = typeof source[sourceProp] === "undefined"
                    ? null
                    : converter
                        ? converter(source[sourceProp])
                        : source[sourceProp];
            };

            function _apply(target, targetProp, source, sourceProp, converter) {
                // If the target obj references props from a referenced obj: Don't change values of the referenced object.
                if (targetProp.indexOf(".") !== -1)
                    return;

                if (typeof target[targetProp] === "undefined")
                    return;

                var value = converter
                    ? converter(source[sourceProp || targetProp])
                    : source[sourceProp || targetProp];

                target.set(targetProp, value !== "" ? value : null);
            }

            return GeoPlaceEditorInfo;

        })();

        ui.GeoPlaceInfo = kendo.data.ObservableObject.extend({

            Street: null,
            StreetNumber: null,
            ZipCode: null,
            City: null,
            StateShort: null,
            StateLong: null,
            CountryShort: null,
            CountryLong: null,
            Longitude: null,
            Latitute: null,

            place: null,

            getDisplayAddress: function () {
                var result = "";
                if (this.Street) result += this.Street;
                if (this.StreetNumber) result += " " + this.StreetNumber;
                if (this.ZipCode || this.City) {
                    result += ", ";
                    if (this.ZipCode) result += this.ZipCode;
                    if (this.City) result += " " + this.City;
                }
                if (this.StateLong) result += ", " + this.StateLong;
                if (this.CountryLong) result += ", " + this.CountryLong;

                return result;
            },

            buildFromGoogleMapsPlace: function (item) {

                this.place = item;

                // address_components:
                // types > "route" > long_name > Street
                // types > "street_number" > long_name > Street
                // types > "locality" > long_name > City
                // types -> "adminitrative_area_level_1" > long_name > Bundesland ("Schleswig-Holstein")
                // types -> "adminitrative_area_level_1" > short_name > Bundesland ("SH")
                // types -> "country" > long_name > Land ("Deutschland")
                // types -> "country" > short_name > Land ("DE")

                this.set("Street", this._get(item, "route"));
                this.set("StreetNumber", this._get(item, "street_number"));
                this.set("ZipCode", this._get(item, "postal_code"));
                this.set("City", this._get(item, "locality"));
                this.set("StateShort", this._get(item, "administrative_area_level_1", true));
                this.set("StateLong", this._get(item, "administrative_area_level_1"));
                this.set("CountryShort", this._get(item, "country", true));
                this.set("CountryLong", this._get(item, "country"));

                var location = item.geometry.location;
                this.set("Longitude", location.lng());
                this.set("Latitude", location.lat());
            },

            _get: function (item, type, short) {
                var component;
                for (var i = 0; i < item.address_components.length; i++) {
                    component = item.address_components[i];
                    for (var k = 0; k < component.types.length; k++) {
                        if (component.types[k] === type) {
                            return short ? component.short_name : component.long_name;
                        }
                    }
                }

                return null;
            },

            init: function () {
                kendo.data.ObservableObject.fn.init.call(this, this);
            }
        });

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
