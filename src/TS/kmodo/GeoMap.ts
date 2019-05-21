namespace kmodo {

    // German ZipCodes -> Communes: https://www.geodaten-deutschland.de/postleitzahlen-strassen-liste-deutschland.php

    class GoogleMapInitializer extends cmodo.ComponentBase {
        _key: string;
        private isInitialized: boolean = false;

        constructor() {
            super();
        }

        init() {
            if (!this.isInitialized) {
                $.getScript("https://maps.googleapis.com/maps/api/js?v=3.36&language=de&region=DE&libraries=places,geometry,drawing&callback=kmodo.googleMapInitializer.onScriptReady&key=" + this._key);

                //$.getScript("https://maps.googleapis.com/maps/api/js?v=3.36&language=de&region=DE&libraries=places,geometry&key=" + this._key,
                //    e => {
                //        this.onScriptReady();
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
    export const googleMapInitializer = new GoogleMapInitializer();

    export interface IUTMValue {
        zone: string;
        easting: number;
        northing: number;
    }

    export interface GeoPlaceEditorPropMappingSettings {
        City?: string;
        CountryStateId?: string;
        CountryId?: string;
    }

    interface GeoPlaceEditorProps {
        Street: string;
        ZipCode: string;
        // Commune: string;
        City?: string;
        CountryStateId?: string;
        CountryId?: string;

        Longitude: string;
        Latitude: string;

        UtmZone: string; 
        UtmEasting: string;
        UtmNorthing: string;
    }
   
    export interface GeoPlaceEditorValues extends kmodo.ObservableObject {
        Street: string;
        ZipCode: string;
        // Commune: string;
        City?: string;
        CountryStateId?: string;
        CountryId?: string;

        Longitude: number;
        Latitude: number;

        UtmZone: string; 
        UtmEasting: number;
        UtmNorthing: number;
    }

    export class GeoPlaceEditorInfo {
        vm: GeoPlaceEditorValues;
        PlaceInfo: GeoPlaceInfo;
        _map: GeoPlaceEditorProps;

        constructor(vm: GeoPlaceEditorValues, map?: GeoPlaceEditorPropMappingSettings) {
            this.vm = vm;
            this.PlaceInfo = null;
            this._map = {
                Street: "Street",
                ZipCode: "ZipCode",
                // Commune: "Commune",
                City: "City",
                CountryStateId: "CountryStateId",
                CountryId: "CountryId",
                Longitude: "Longitude",
                Latitude: "Latitude",
                UtmZone: "UtmZone",
                UtmEasting: "UtmEasting",
                UtmNorthing: "UtmNorthing"
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
            const vm = this.vm;
            const place = this.PlaceInfo || (this.PlaceInfo = new GeoPlaceInfo());
            const m = this._map;
            // NOTE: We don't have a street number property on entities.
            this._set(vm, m.Street, place, "Street");
            this._set(vm, m.ZipCode, place, "ZipCode");
            // this._set(vm, m.Commune, place, "Commune");
            this._set(vm, m.City, place, "City");
            this._set(vm, m.CountryStateId, place, "StateLong", this.getDisplayNameByIdConverter("CountryState"));
            this._set(vm, m.CountryId, place, "CountryLong", this.getDisplayNameByIdConverter("Country"));
            this._set(vm, m.Longitude, place, "Longitude");
            this._set(vm, m.Latitude, place, "Latitude");
            this._set(vm, m.UtmZone, place, "UtmZone");
            this._set(vm, m.UtmEasting, place, "UtmEasting");
            this._set(vm, m.UtmNorthing, place, "UtmNorthing");
        }

        applyChanges(): void {
            const vm = this.vm;
            const place = this.PlaceInfo;
            const m = this._map;
            // NOTE: We don't have a street number property on entities.
            place.Street = (place.Street || "") + (place.StreetNumber ? " " + place.StreetNumber : "");
            this._apply(vm, m.Street, place);
            this._apply(vm, m.ZipCode, place);
            // this._apply(vm, m.Commune, place);
            this._apply(vm, m.City, place);
            this._apply(vm, m.CountryStateId, place, "StateShort", this.getIdByCodeConverter("CountryState"));
            this._apply(vm, m.CountryId, place, "CountryShort", this.getIdByCodeConverter("Country"));
            this._apply(vm, m.Longitude, place);
            this._apply(vm, m.Latitude, place);
            this._apply(vm, m.UtmZone, place);
            this._apply(vm, m.UtmEasting, place);
            this._apply(vm, m.UtmNorthing, place);
        }

        private _set(source: Partial<kendo.data.ObservableObject>, sourceProp: string,
            target: GeoPlaceInfo, targetProp: any,
            converter?: Function): void {

            const sourceValue = cmodo.getValueAtPropPath(source, sourceProp);

            target.set(targetProp || sourceProp,
                typeof sourceValue === "undefined"
                    ? null
                    : converter
                        ? converter(sourceValue)
                        : sourceValue);
        }

        private _apply(target: Partial<kendo.data.ObservableObject>, targetProp: string, source: GeoPlaceInfo, sourceProp?: string, converter?: Function) {
            // If the target obj references props from a referenced obj: Don't change values of the referenced object.
            if (targetProp.indexOf(".") !== -1)
                return;

            if (typeof target[targetProp] === "undefined")
                return;

            const sourceValue = source.get(sourceProp || targetProp);

            const value = converter
                ? converter(sourceValue)
                : sourceValue;

            target.set(targetProp, value !== "" ? value : null);
        }

        private getDisplayNameByIdConverter(type: string)
            : (id: string) => string {
            return (id: string) => cmodo.entityMappingService.getDisplayNameById(type, id);
        }

        private getIdByCodeConverter(type: string)
            : (code: string) => string {
            return (code: string) => cmodo.entityMappingService.getIdByCode(type, code);
        }
    }

    export class GeoPlaceInfo extends kendo.data.ObservableObject {
        Street: string;
        StreetNumber: string;
        ZipCode: string;
        // Commune: string;
        City: string;
        StateShort: string;
        StateLong: string;
        CountryShort: string;
        CountryLong: string;
        Longitude: number;
        Latitute: number;
        UtmZone: string;
        UtmEasting: number;
        UtmNorthing: number;

        constructor() {
            super();
            super.init(this);
        }

        getDisplayAddress() {
            let result = "";
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
            // types > "sublocality_level_1" > long_name > Commune
            // types > "locality" > long_name > City
            // types -> "adminitrative_area_level_1" > long_name > Bundesland ("Schleswig-Holstein")
            // types -> "adminitrative_area_level_1" > short_name > Bundesland ("SH")
            // types -> "country" > long_name > Land ("Deutschland")
            // types -> "country" > short_name > Land ("DE")

            this.set("Street", this._get(place, "route"));
            this.set("StreetNumber", this._get(place, "street_number"));
            this.set("ZipCode", this._get(place, "postal_code"));

            // "Gemeinde": 
            //   sublocality indicates a first - order civil entity below a locality.
            //   For some locations may receive one of the additional types: 
            //   sublocality_level_1 to sublocality_level_5.
            //   Each sublocality level is a civil entity.
            //   Larger numbers indicate a smaller geographic area.
            // this.set("Commune", this._get(place, "sublocality_level_1")); // TODO: or "sublocality"?

            this.set("City", this._get(place, "locality"));

            this.set("StateShort", this._get(place, "administrative_area_level_1", true));
            this.set("StateLong", this._get(place, "administrative_area_level_1"));
            this.set("CountryShort", this._get(place, "country", true));
            this.set("CountryLong", this._get(place, "country"));

            const location = place.geometry.location;
            this.set("Latitude", location.lat());
            this.set("Longitude", location.lng());

            const utm = convertLatLngToUTM({
                lat: location.lat(),
                lng: location.lng()
            });

            this.set("UtmZone", utm ? utm.zone : null);
            this.set("UtmEasting", utm ? utm.easting : null);
            this.set("UtmNorthing", utm ? utm.northing : null);
        }

        private _get(place: google.maps.places.PlaceResult, type: string, short?: boolean): string {
            // Get specific address component of Google Map place data.
            for (const component of place.address_components) {
                if (component.types.some(x => x === type)) {
                    return short ? component.short_name : component.long_name;
                }
            }

            return null;
        }
    }

    export class GoogleMapCustomOverlayFactory {

        static createEditableTextBox(view: GeoMapViewBase, location: google.maps.LatLng) {
            const textBox = new GoogleMapCustomOverlayFactory.EditableTextBox(view, location) as google.maps.OverlayView;
           
            return textBox;
        }

        static EditableTextBox: any;

        static init() {

            GoogleMapCustomOverlayFactory.EditableTextBox = class extends google.maps.OverlayView {
                private map: google.maps.Map;
                private div: HTMLDivElement = null;
                private _isDragging = false;
                private _dragOrigin: google.maps.Point = null;

                constructor(
                    private view: GeoMapViewBase,
                    private location: google.maps.LatLng) {

                    super();

                    this.map = view.map;

                    // Explicitly call setMap on this overlay.
                    this.setMap(this.map);
                }

                private _createView(): void {
                    const div = document.createElement("div");
                    div.className = "km-google-map-editable-text-box";
                    div.draggable = true;

                    var textDiv = document.createElement("div");
                    textDiv.contentEditable = "true";
                    textDiv.spellcheck = false;
                    div.appendChild(textDiv);

                    google.maps.event.addDomListener(div, "dragstart", e => {
                        this._isDragging = true;
                        this._dragOrigin = new google.maps.Point(e.clientX, e.clientY);
                    });

                    google.maps.event.addDomListener(div, "dragend", e => {
                        if (!this._isDragging || e.clientX < 0 && e.clientY < 0) {
                            return;
                        }

                        this._isDragging = false;

                        //const x = e.clientX; // - e.offsetX;
                        //const y = e.clientY; // - e.offsetY;
                        const left = this._dragOrigin.x - e.clientX;
                        const top = this._dragOrigin.y - e.clientY;
                        const pos = this.getProjection().fromLatLngToDivPixel(this.location);

                        this.location = this.getProjection().fromDivPixelToLatLng(new google.maps.Point(pos.x - left, pos.y - top));

                        this.draw();
                        //div.style.left = x + 'px';
                        //div.style.top = y + 'px';
                    });

                    //google.maps.event.addListener(div, "dragleave", e => {
                    //    this._isDragging = false;
                    //});

                    //google.maps.event.addDomListener(div, 'drag', e => {
                    //    if (!this._isDragging || e.clientX < 0 && e.clientY < 0) {
                    //        return;
                    //    }

                    //    //const x = e.clientX - this.get('mouseX'); // + this.get('imgX');
                    //    //const y = e.clientY - this.get('mouseY'); // + this.get('imgY');

                    //    //div.style.left = x + 'px';
                    //    //div.style.top = y + 'px';

                    //    //var overlayProjection = this.getProjection();
                    //    //var sw = overlayProjection.fromContainerPixelToLatLng(new google.maps.Point(x, y + img.offsetHeight));
                    //    //var ne = overlayProjection.fromContainerPixelToLatLng(new google.maps.Point(x + img.offsetWidth, y));
                    //    //this.set('sw', sw);
                    //    //this.set('ne', ne);
                    //});

                    //const moveAdorner = document.createElement("div");
                    //moveAdorner.className = "km-move-adorner";
                    //div.appendChild(moveAdorner);

                    //const textarea = document.createElement("textarea");
                    //div.appendChild(textarea);

                    //div.addEventListener("mouseenter", (ev) => {
                    //    this.view._disableMapFunctions("pointer");
                    //});

                    //div.addEventListener("mouseleave", (ev) => {
                    //    this.view._restoreMapFunctions();
                    //});

                    //// Create the img element and attach it to the div.
                    //const img = document.createElement('img');
                    //img.src = this.image_;
                    //img.style.width = '100%';
                    //img.style.height = '100%';
                    //img.style.position = 'absolute';
                    //div.appendChild(img);

                    // See https://developers.google.com/maps/documentation/javascript/reference/overlay-view#OverlayView.preventMapHitsAndGesturesFrom
                    (google.maps.OverlayView as any).preventMapHitsAndGesturesFrom(div);

                    this.div = div;
                }

                /**
                 * onAdd is called when the map's panes are ready and the overlay has been
                 * added to the map.
                 */
                onAdd() {
                    this._createView();

                    //const overlayProjection = this.getProjection();
                    //const position = overlayProjection.fromLatLngToDivPixel(this.location);
                    //this.div.style.left = position.x + "px";
                    //this.div.style.top = position.y + "px";

                    this.getPanes().floatPane.appendChild(this.div);
                }

                draw() {
                    const overlayProjection = this.getProjection();

                    // Retrieve the southwest and northeast coordinates of this overlay
                    // in latlngs and convert them to pixels coordinates.
                    // We'll use these coordinates to resize the DIV.
                    const position = overlayProjection.fromLatLngToDivPixel(this.location);

                    const div = this.div;
                    div.style.left = position.x + 'px';
                    div.style.top = position.y + 'px';
                }

                // The onRemove() method will be called automatically from the API if
                // we ever set the overlay's map property to 'null'.
                onRemove() {
                    if (this.div.parentNode)
                        this.div.parentNode.removeChild(this.div);
                    this.div = null;
                }

            }
        }
    }

    export function convertLatLngToUTM(coords: google.maps.LatLngLiteral): IUTMValue {
        try {
            const lat = coords.lat;
            const lng = coords.lng;

            // Compute zone
            // Sources: https://gis.stackexchange.com/questions/13291/computing-utm-zone-from-lat-long-point
            // original https://www.wavemetrics.com/code-snippet/convert-latitudelongitude-utm

            let zoneNum = Math.floor((coords.lng + 180) / 6) + 1;

            if (lat >= 56.0 && lat < 64.0 && lng >= 3.0 && lng < 12.0) {
                // TODO: what is this case actually?
                zoneNum = 32;
            }
            // Special zones for Svalbard
            else if (lat >= 72.0 && lat < 84.0) {
                if (lng >= 0.0 && lng < 9.0) {
                    zoneNum = 31;
                } else if (lng >= 9.0 && lng < 21.0) {
                    zoneNum = 33;
                } else if (lng >= 21.0 && lng < 33.0) {
                    zoneNum = 35;
                } else if (lng >= 33.0 && lng < 42.0) {
                    zoneNum = 37;
                }
            }

            const zone: string = "" + zoneNum + getUTMLetterDesignator(lat);

            const utm = "+proj=utm +zone=" + zone;
            const wgs84 = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";

            const value = proj4(wgs84, utm, [coords.lng, coords.lat]);

            //if (true) {
            //    console.log("WGS84 to UTM: zone: " + zone);
            //    console.log(value);
            //}

            return {
                zone: zone,
                easting: value[0],
                northing: value[1]
            }
        } catch (err) {
            handleError(err);
            return null;
        }
    }

    function getUTMLetterDesignator(lat: number): string {
        // Source: https://www.wavemetrics.com/code-snippet/convert-latitudelongitude-utm

        let letter: string = null;
        if (-80 <= lat && lat <= 84) {
            letter = "CDEFGHJKLMNPQRSTUVWXX"[Math.floor((lat + 80) / 8)]
        }

        if (letter)
            return letter;

        throw new Error("Error while computing letter designator of " +
            `latitude ${lat}: latitude is outside of the UTM limits.`);
    }

    function handleError(err: any): void {
        let msg: string = null;
        if (typeof err === "string") {
            msg = err;
        } else {
            msg = err.message;
        }
        cmodo.showError(msg);
    }
}
