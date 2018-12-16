namespace kmodo {

    class GoogleMapInitializer extends cmodo.ComponentBase {
        _key: string;
        private isInitialized: boolean = false;

        constructor() {
            super();
        }

        init() {
            if (!this.isInitialized) {
                $.getScript("https://maps.googleapis.com/maps/api/js?v=3.34&language=de&region=DE&libraries=places,geometry,drawing&callback=kmodo.googleMapInitializer.onScriptReady&key=" + this._key);

                //$.getScript("https://maps.googleapis.com/maps/api/js?v=3.34&language=de&region=DE&libraries=places,geometry&key=" + this._key,
                //    function (e) {
                //        self.onScriptReady();
                //    });
            }
            else
                this.onScriptReady();
        }

        onScriptReady() {
            this.isInitialized = true;
            this.trigger("scriptReady", { sender: this });
        }
    }
    export let googleMapInitializer = new GoogleMapInitializer();

    export interface GeoPlaceEditorPropMappingSettings {
        City?: string;
        CountryStateId?: string;
        CountryId?: string;
    }

    interface GeoPlaceEditorPropMap {
        Street: string;
        ZipCode: string;
        City: string;
        CountryStateId: string;
        CountryId: string;
        Longitude: string;
        Latitude: string
    }

    export class GeoPlaceEditorInfo {
        vm: kendo.data.ObservableObject;
        PlaceInfo: GeoPlaceInfo;
        _map: GeoPlaceEditorPropMap;

        constructor(vm: kendo.data.ObservableObject, map?: GeoPlaceEditorPropMappingSettings) {
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

            // Apply property mapping if provided.
            if (map)
                this._mapCore(map);

            this.build();
        }

        map(map: GeoPlaceEditorPropMappingSettings): void {
            this._mapCore(map);
            this.build();
        }

        private _mapCore(map: GeoPlaceEditorPropMappingSettings): void {
            this._map = Object.assign(this._map, map);
        }

        build(): void {
            var vm = this.vm;
            var place = this.PlaceInfo || (this.PlaceInfo = new GeoPlaceInfo());
            var m = this._map;
            // NOTE: We don't have a street number property on entities.
            this._set(vm, m.Street, place, "Street");
            this._set(vm, m.ZipCode, place, "ZipCode");
            this._set(vm, m.City, place, "City");
            this._set(vm, m.CountryStateId, place, "StateLong", this.getCountryStateDisplayName);
            this._set(vm, m.CountryId, place, "CountryLong", this.getCountryDisplayName);
            this._set(vm, m.Longitude, place, "Longitude");
            this._set(vm, m.Latitude, place, "Latitude");
        }

        applyChanges(): void {
            var vm = this.vm;
            var place = this.PlaceInfo;
            var m = this._map;
            // NOTE: We don't have a street number property on entities.
            place.Street = (place.Street || "") + (place.StreetNumber ? " " + place.StreetNumber : "");
            this._apply(vm, m.Street, place);
            this._apply(vm, m.ZipCode, place);
            this._apply(vm, m.City, place);
            this._apply(vm, m.CountryStateId, place, "StateShort", this.getCountryStateDisplayName);
            this._apply(vm, m.CountryId, place, "CountryShort", this.getCountryDisplayName);
            this._apply(vm, m.Longitude, place);
            this._apply(vm, m.Latitude, place);
        }

        private _set(source: kendo.data.ObservableObject, sourceProp: string, target: GeoPlaceInfo, targetProp: any, converter?: Function): void {

            var sourceValue = cmodo.getValueAtPropPath(source, sourceProp);

            target.set(targetProp || sourceProp,
                typeof sourceValue === "undefined"
                    ? null
                    : converter
                        ? converter(sourceValue)
                        : sourceValue);
        }

        private _apply(target: kendo.data.ObservableObject, targetProp: string, source: GeoPlaceInfo, sourceProp?: string, converter?: Function) {
            // If the target obj references props from a referenced obj: Don't change values of the referenced object.
            if (targetProp.indexOf(".") !== -1)
                return;

            if (typeof target[targetProp] === "undefined")
                return;

            var sourceValue = source.get(sourceProp || targetProp);

            var value = converter
                ? converter(sourceValue)
                : sourceValue;

            target.set(targetProp, value !== "" ? value : null);
        }

        private getCountryStateDisplayName(id: string): string {
            return cmodo.entityMappingService.getDisplayNameById("CountryState", id);
        }

        private getCountryDisplayName(id: string): string {
            return cmodo.entityMappingService.getDisplayNameById("Country", id);
        }
    }

    export class GeoPlaceInfo extends kendo.data.ObservableObject {
        Street: string;
        StreetNumber: string;
        ZipCode: string;
        City: string;
        StateShort: string;
        StateLong: string;
        CountryShort: string;
        CountryLong: string;
        Longitude: number;
        Latitute: number;

        // TODO: REMOVE:private _place: google.maps.places.PlaceResult;

        constructor() {
            super();
            super.init(this);
        }

        getDisplayAddress() {
            var result = "";
            if (this.Street) result += this.Street;
            if (this.StreetNumber) result += " " + this.StreetNumber;
            if (this.ZipCode || this.City) {
                result += ", ";
                if (this.ZipCode) result += this.ZipCode;
                if (this.City) result += " " + this.City;
            }
            if (this.StateLong) result += ", " + this.StateLong;
            // Omit country because we are hard-coding to Germany elsewhere anyway.
            //if (this.CountryLong) result += ", " + this.CountryLong;

            return result;
        }

        buildFromGoogleMapsPlace(place: google.maps.places.PlaceResult) {

            // TODO: REMOVE: this._place = place;

            // address_components:
            // types > "route" > long_name > Street
            // types > "street_number" > long_name > Street
            // types > "locality" > long_name > City
            // types -> "adminitrative_area_level_1" > long_name > Bundesland ("Schleswig-Holstein")
            // types -> "adminitrative_area_level_1" > short_name > Bundesland ("SH")
            // types -> "country" > long_name > Land ("Deutschland")
            // types -> "country" > short_name > Land ("DE")

            this.set("Street", this._get(place, "route"));
            this.set("StreetNumber", this._get(place, "street_number"));
            this.set("ZipCode", this._get(place, "postal_code"));
            this.set("City", this._get(place, "locality"));
            this.set("StateShort", this._get(place, "administrative_area_level_1", true));
            this.set("StateLong", this._get(place, "administrative_area_level_1"));
            this.set("CountryShort", this._get(place, "country", true));
            this.set("CountryLong", this._get(place, "country"));

            var location = place.geometry.location;
            this.set("Longitude", location.lng());
            this.set("Latitude", location.lat());
        }

        private _get(place: google.maps.places.PlaceResult, type: string, short?: boolean): string {

            // Get specific address component of Google Map place data.

            let component: google.maps.GeocoderAddressComponent;

            for (let i = 0; i < place.address_components.length; i++) {
                component = place.address_components[i];

                for (let k = 0; k < component.types.length; k++) {

                    if (component.types[k] === type)
                        return short ? component.short_name : component.long_name;
                }
            }

            return null;
        }
    }
}
