import { signal } from "@angular/core"

import { ItemModel } from "@lib/models"

import { convertLatLngToUTM } from "./utm"

export class MapAddressModel extends ItemModel {
    static fromGoogleMapsResult(mapsResult: google.maps.places.PlaceResult | google.maps.GeocoderResult): MapAddressModel {
        const address = new MapAddressModel()
        address.buildFromGoogleMapsPlace(mapsResult)

        return address
    }

    readonly street = signal("")
    readonly streetNumber = signal("")
    readonly zipCode = signal("")
    readonly city = signal("")
    readonly countryStateCode = signal("")
    readonly countryStateName = signal("")
    readonly countryCode = signal("")
    readonly countryName = signal("")
    readonly latitude = signal<number | null>(null)
    readonly longitude = signal<number | null>(null)
    readonly utmZone = signal("")
    readonly utmEasting = signal<number | null>(null)
    readonly utmNorthing = signal<number | null>(null)

    buildFromGoogleMapsPlace(mapsResult: google.maps.places.PlaceResult | google.maps.GeocoderResult) {
        // address_components:
        // types > "route" > long_name > Street
        // types > "street_number" > long_name > Street
        // types > "sublocality_level_1" > long_name > Commune
        // types > "locality" > long_name > City
        // types -> "adminitrative_area_level_1" > long_name > Bundesland ("Schleswig-Holstein")
        // types -> "adminitrative_area_level_1" > short_name > Bundesland ("SH")
        // types -> "country" > long_name > Land ("Deutschland")
        // types -> "country" > short_name > Land ("DE")

        this.street.set(this.#getComponent(mapsResult, "route"))
        this.streetNumber.set(this.#getComponent(mapsResult, "street_number"))
        this.zipCode.set(this.#getComponent(mapsResult, "postal_code"))

        // NOTE: We can't use sublocalities for the Germany "Gemeinde" (commune).
        // The google maps result is not predictable here. It's something one can read,
        // and say "ok", but it has no defined meaning.
        //   sublocality indicates a first - order civil entity below a locality.
        //   For some locations may receive one of the additional types:
        //   sublocality_level_1 to sublocality_level_5.
        //   Each sublocality level is a civil entity.
        //   Larger numbers indicate a smaller geographic area.
        // this.set("Commune", this._get(place, "sublocality_level_1"));

        this.city.set(this.#getComponent(mapsResult, "locality"))

        this.countryStateCode.set(this.#getComponent(mapsResult, "administrative_area_level_1", true))
        this.countryStateName.set(this.#getComponent(mapsResult, "administrative_area_level_1"))
        this.countryCode.set(this.#getComponent(mapsResult, "country", true))
        this.countryName.set(this.#getComponent(mapsResult, "country"))

        const location = mapsResult.geometry?.location
        if (location) {
            this.latitude.set(location.lat())
            this.longitude.set(location.lng())

            const utm = convertLatLngToUTM({
                lat: location.lat(),
                lng: location.lng()
            })

            this.utmZone.set(utm ? utm.zone : "")
            this.utmEasting.set(utm ? utm.easting : null)
            this.utmNorthing.set(utm ? utm.northing : null)
        }
    }

    #getComponent(mapsResult: google.maps.places.PlaceResult | google.maps.GeocoderResult, componentType: string, returnShortValue?: boolean): string {
        // Get specific address component of Google Map place data.
        if (!mapsResult.address_components?.length) return ""

        for (const component of mapsResult.address_components) {
            if (component.types.some(x => x === componentType)) {
                return returnShortValue ? component.short_name : component.long_name
            }
        }

        return ""
    }
}
