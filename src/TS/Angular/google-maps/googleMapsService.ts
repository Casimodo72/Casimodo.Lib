import { HttpClient } from "@angular/common/http"
import { Injectable, signal } from "@angular/core"
import { DialogService } from "@lib/dialogs"
import { ItemModel } from "@lib/models"
import { NotificationService } from "@lib/services"
import { firstValueFrom } from "rxjs"

export class GeoMapsAddress extends ItemModel {
    readonly locationType: google.maps.GeocoderLocationType

    readonly street?: IGeoMapsComponentValue
    readonly streetNumber?: IGeoMapsComponentValue
    readonly postalCode?: IGeoMapsComponentValue
    readonly city?: IGeoMapsComponentValue
    readonly countryState?: IGeoMapsComponentValue
    readonly country?: IGeoMapsComponentValue

    constructor(locationType: google.maps.GeocoderLocationType,
        street: IGeoMapsComponentValue, streetNumber: IGeoMapsComponentValue,
        postalCode: IGeoMapsComponentValue, city: IGeoMapsComponentValue,
        countryState: IGeoMapsComponentValue, country: IGeoMapsComponentValue
    ) {
        super()

        this.locationType = locationType

        this.street = street
        this.streetNumber = streetNumber
        this.postalCode = postalCode
        this.city = city
        this.countryState = countryState
        this.country = country
    }

    isComplete(): boolean {
        return !!this.street && !!this.streetNumber
            && !!this.postalCode && !!this.city
            && !!this.countryState && !!this.country
    }

    getStreetPart(): string {
        return `${this.street?.value} ${this.streetNumber?.value}`
    }

    getPostalCodeAndCity(): string {
        return `${this.postalCode?.value} ${this.city?.value}`
    }

    getCountryStateAndState(): string {
        return `${this.countryState?.shortValue}, ${this.country?.value}`
    }

    getAddress(): string {
        return `${this.getStreetPart()}, ${this.getPostalCodeAndCity()}, ${this.getCountryStateAndState()}`
    }
}

export interface IGeoMapsComponentValue {
    type: string
    value: string
    shortValue: string
}

export interface GeoMapsPositionInfo {
    position: GeolocationPosition | null
    addresses: GeoMapsAddress[]
}

@Injectable({ providedIn: "root" })
export class GeoMapsService {
    readonly #http: HttpClient
    readonly #notifications: NotificationService
    readonly #dialogService: DialogService
    readonly geocodedAddresses = signal<GeoMapsAddress[]>([])
    readonly addressResults = signal<google.maps.GeocoderResult[]>([])
    readonly apiLoaded = signal(false)
    #geocoder?: google.maps.Geocoder

    constructor(http: HttpClient, notifications: NotificationService, dialogService: DialogService) {
        this.#http = http
        this.#notifications = notifications
        this.#dialogService = dialogService
    }

    async ensureMapsApi() {
        if (this.apiLoaded()) return

        try {
            // TODO: Evaluate loading with importLibrary instead.
            // See https://developers.google.com/maps/documentation/javascript/libraries
            // See https://developers.google.com/maps/documentation/javascript/overview?hl=de#Loading_the_Maps_API
            await firstValueFrom(this.#http.jsonp("https://maps.googleapis.com/maps/api/js?key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4", "callback"))

            this.apiLoaded.set(true)
        }
        catch (error) {
            console.log(error)
            this.#notifications.showError(error)
        }
    }

    async getPositionInfo(): Promise<GeoMapsPositionInfo> {
        this.geocodedAddresses.set([])

        const result: GeoMapsPositionInfo = {
            position: null,
            addresses: []
        }

        result.position = await this.#tryGetGeolocationPosition()
        if (!result.position) {
            return result
        }

        // See https://developers.google.com/maps/documentation/javascript/geocoding?hl=de

        const latLng = new google.maps.LatLng(
            result.position.coords.latitude,
            result.position.coords.longitude)

        // TODO: When using the following restrictions then we get no results.
        // const componentRestrictions: google.maps.GeocoderComponentRestrictions = {
        //     // TODO: How to configure for our foreign programmers?
        //     country: "de",
        // }

        const request: google.maps.GeocoderRequest = {
            location: latLng,
            language: "de",
            componentRestrictions: null
        }

        this.#geocoder ??= new google.maps.Geocoder()
        const response: google.maps.GeocoderResponse = await this.#geocoder.geocode(request)

        result.addresses = this.#getAddressesFromGeocoderResultList(response.results)

        return result
    }

    #getAddressesFromGeocoderResultList(results: google.maps.GeocoderResult[]): GeoMapsAddress[] {
        const addresses: GeoMapsAddress[] = []

        const rooftopResults = results.filter(x =>
            x.geometry.location_type === google.maps.GeocoderLocationType.ROOFTOP)

        for (const result of rooftopResults) {
            const address = this.#getAddressFromGeocoderResult(result)
            if (address.isComplete()) {
                addresses.push(address)
            }
        }

        return addresses
    }

    #getAddressFromGeocoderResult(result: google.maps.GeocoderResult): GeoMapsAddress {
        const address = new GeoMapsAddress(result.geometry.location_type,
            this.#getComponentValue("route", result),
            this.#getComponentValue("street_number", result),
            this.#getComponentValue("postal_code", result),
            this.#getComponentValue("administrative_area_level_3", result),
            this.#getComponentValue("administrative_area_level_1", result),
            this.#getComponentValue("country", result))

        return address
    }

    #getComponentValue(type: string, result: google.maps.GeocoderResult): IGeoMapsComponentValue {
        const addressComponent = result.address_components.find(x => x.types[0] === type)

        return {
            type: type,
            value: addressComponent?.long_name?.trim() || "",
            shortValue: addressComponent?.short_name?.trim() || "",
        }
    }

    async #tryGetGeolocationPosition(): Promise<GeolocationPosition | null> {
        try {
            return await this.#getGeolocationPosition()
        }
        catch (error) {
            console.log(error)
            //  GeolocationPositionError {code: 1, message: 'User denied Geolocation'}
            if (error instanceof GeolocationPositionError && error.code === 1) {
                const confirmedGeolocationIsActivated = await this.#dialogService.confirm({
                    title: "Zugriff auf den Standort ist blockert",
                    message: "Sie haben in den Einstellungen Ihres Gerätes den Zugriff auf den Standort blockiert. " +
                        "Bitte erlauben Sie dieser App den Zugriff auf den Standort auf Ihrem Gerät. " +
                        "Klicken Sie auf 'Ok', wenn Sie den Standort freigegeben haben."
                })

                if (confirmedGeolocationIsActivated) {
                    return this.#tryGetGeolocationPosition()
                }
            }

            this.#notifications.showError(error)

            return null
        }
    }

    async #getGeolocationPosition(): Promise<GeolocationPosition | null> {
        return new Promise((resolve, reject) => {
            // TODO: How to check if geolocation is supported/allowed?
            if (!navigator.geolocation) {
                resolve(null)
            } else {
                navigator.geolocation.getCurrentPosition(
                    (position: GeolocationPosition) => resolve(position),
                    (error: GeolocationPositionError) => reject(error)
                )
            }
        })
    }
}
