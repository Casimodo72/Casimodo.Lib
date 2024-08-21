
import { Component } from "@angular/core"
import { HttpClient } from "@angular/common/http"
import { GoogleMapsModule } from "@angular/google-maps"
import { Observable, of } from "rxjs"
import { catchError, map } from "rxjs/operators"
import { CommonModule } from "@angular/common"

// See https://github.com/angular/components/blob/main/src/google-maps/README.md
// Geocoder: https://github.com/angular/components/blob/main/src/google-maps/map-geocoder/README.md

@Component({
    selector: "app-google-maps-demo",
    standalone: true,
    imports: [CommonModule, GoogleMapsModule],
    template: `
        @if (apiLoaded | async) {
            <google-map />
        }
    `
})
export class GoogleMapsDemoComponent {
    readonly apiLoaded: Observable<boolean>

    constructor(httpClient: HttpClient) {
        // If you're using the `<map-heatmap-layer>` directive, you also have to include the `visualization` library
        // when loading the Google Maps API. To do so, you can add `&libraries=visualization` to the script URL:
        // https://maps.googleapis.com/maps/api/js?key=YOUR_API_KEY&libraries=visualization

        this.apiLoaded = httpClient.jsonp("https://maps.googleapis.com/maps/api/js?key=AIzaSyBdrDyNrkrGZ8-pp5SWzn63SytcqeUosC4", "callback")
            .pipe(
                map(() => true),
                catchError(() => of(false)),
            )
    }

    async #process() {
        const location = await this.#getLocation()
        if (!location) return

        const request: google.maps.GeocoderRequest = {
            location: new google.maps.LatLng(location.coords.latitude, location.coords.longitude)
        }

        const geocoder = new google.maps.Geocoder()
        geocoder.geocode(
            request,
            (results: google.maps.GeocoderResult[] | null, status: google.maps.GeocoderStatus) => {
                if (status !== google.maps.GeocoderStatus.OK || !results?.length) {
                    // TODO: IMPL
                    return
                }

                const result = results[0]
                // google.maps.GeocoderAddressComponent
                //result.address_components
            })
    }

    async #getLocation(): Promise<GeolocationPosition | null> {
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
