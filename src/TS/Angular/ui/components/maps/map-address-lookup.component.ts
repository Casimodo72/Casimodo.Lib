import {
    AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, NgZone,
    inject, signal, viewChild
} from "@angular/core"
import { CommonModule } from "@angular/common"
import { WritableSignal } from "@angular/core"
import { GoogleMapsModule } from "@angular/google-maps"

import { MatCard } from "@angular/material/card"

import { NotificationService } from "@lib/services"
import { DialogService, openInputOutputFormDialog } from "@lib/ui/dialogs"
import { EntityFormPartModel, InputOutputDialogForm } from "@lib/ui/forms"
import { ILookupAddress, numberInput, stringInput, textAreaInput } from "@lib/models/inputs"
import { ItemModel, ListModel, ValidationError } from "@lib/models"
import { StandardStringInputComponent } from "../standard-string-input.component"
import { StandardNumberInputComponent } from "../standard-number-input.component"
import { StandardTextAreaInputComponent } from "../standard-text-area-input.component"
import { StandardPickerComponent } from "../standard-picker.component"
import { FormColummComponent } from "../form-column.component"
import { fadeWidthAnimation } from "../animations"

import { MapAddressModel } from "./mapAddressModel"

// For Google Maps in Angular 17 see: https://medium.com/@selsa-pingardi/integrating-google-maps-in-angular-17-66487ed2238c

// See https://github.com/angular/components/blob/main/src/google-maps/README.md
// Geocoder: https://github.com/angular/components/blob/main/src/google-maps/map-geocoder/README.md
// https://developers.google.com/maps/documentation/javascript/libraries?hl=de#typescript
// https://timdeschryver.dev/blog/google-maps-as-an-angular-component
// https://medium.com/@selsa-pingardi/integrating-google-maps-in-angular-17-66487ed2238c

interface IMapMarker {
    title?: string
    label?: string
    position: google.maps.LatLng
    //  https://developers.google.com/maps/documentation/javascript/reference/advanced-markers#AdvancedMarkerElement
    options?: google.maps.marker.AdvancedMarkerElementOptions
    isVisible?: boolean
}

class MapMarker extends ItemModel {
    readonly title: string
    readonly label?: string
    readonly position: WritableSignal<google.maps.LatLng>
    readonly options: google.maps.marker.AdvancedMarkerElementOptions
    readonly isVisible = signal(true)

    constructor(config: IMapMarker) {
        super()

        this.title = config.title ?? ""
        this.label = config.label
        this.position = signal(config.position)
        this.options = config.options ?? {}

        if (config.isVisible === false) {
            this.isVisible.set(false)
        }
    }
}

class AddressForm extends EntityFormPartModel {
    readonly street = textAreaInput(this)
        .setLabel("Straße")
        .setRowCount(2).setMaxRowCount(2)

    readonly zipCode = stringInput(this)
        .setLabel("PLZ")

    readonly city = stringInput(this)
        .setLabel("Stadt")

    readonly countryStateName = stringInput(this)
    readonly countryStateCode = stringInput(this)

    readonly countryName = stringInput(this)
    readonly countryCode = stringInput(this)

    readonly latitude = numberInput(this)
        .setLabel("Breite")

    readonly longitude = numberInput(this)
        .setLabel("Länge")

    readonly utmZone = stringInput(this)
        .setLabel("UTM Z")

    readonly utmEasting = numberInput(this)
        .setLabel("Easting")

    readonly utmNorthing = numberInput(this)
        .setLabel("Northing")

    async clear() {
        await this._visitInputModelsAsync(async ctx => {
            ctx.input.setValue(null)
        })
    }

    assignFromConsumerAddress(address: ILookupAddress) {
        this.street.setValue(address.street?.trim() ?? "")
        this.zipCode.setValue(address.zipCode?.trim() ?? "")
        this.city.setValue(address.city?.trim() ?? "")

        this.countryName.setValue(address.countryName?.trim() ?? "")
        this.countryCode.setValue(address.countryCode?.trim() ?? "")
        this.countryStateName.setValue(address.countryStateName?.trim() ?? "")
        this.countryStateCode.setValue(address.countryStateCode?.trim() ?? "")

        this.latitude.setValue(address.latitude ?? null)
        this.longitude.setValue(address.longitude ?? null)
        this.utmZone.setValue(address.utmZone?.trim() ?? "")
        this.utmNorthing.setValue(address.utmNorthing ?? null)
        this.utmEasting.setValue(address.utmEasting ?? null)
    }

    assignFrom(address: MapAddressModel) {
        this.street.setValue(`${address.street()} ${address.streetNumber()}`.trim())
        this.zipCode.setValue(address.zipCode())
        this.city.setValue(address.city())

        this.countryName.setValue(address.countryName())
        this.countryCode.setValue(address.countryCode())
        this.countryStateName.setValue(address.countryStateName())
        this.countryStateCode.setValue(address.countryStateCode())

        this.latitude.setValue(address.latitude())
        this.longitude.setValue(address.longitude())
        this.utmZone.setValue(address.utmZone())
        this.utmNorthing.setValue(address.utmNorthing())
        this.utmEasting.setValue(address.utmEasting())
    }
}

@Component({
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatCard,
        GoogleMapsModule,
        StandardTextAreaInputComponent, StandardStringInputComponent, StandardNumberInputComponent,
        StandardPickerComponent, FormColummComponent
    ],
    animations: [fadeWidthAnimation],
    host: {
        "class": "flex-column h-full"
    },
    template: `
<div class="pt-3">
    <!-- NOTE: Google maps will set the placeholder text to "Geben Sie einen Standort ein". -->
    <app-standard-string-input #searchTextInput
        [model]="searchText"
        errorSubscriptSizing="fixed"
        width="100%" />
</div>

<div class="full-flex-col-item w-full h-full">
    <div class="flex gap-1">
        <!-- Side panel with address -->
        @if (contextAddress(); as address) {
            <mat-card class="p-1" @fadeWidth>
                <app-form-column class="w-80">
                    <app-standard-text-area-input [model]="address.street" viewOnly stretch />

                    <div class="flex gap-2">
                        <app-standard-string-input [model]="address.zipCode" viewOnly width="78px" />
                        <app-standard-string-input [model]="address.city" viewOnly flexStretch />
                    </div>

                    <app-standard-string-input [model]="address.countryStateName" viewOnly stretch />
                    <app-standard-string-input [model]="address.countryName" viewOnly stretch />

                    <div class="flex gap-2">
                        <app-standard-number-input [model]="address.latitude" viewOnly width="86px"  />
                        <app-standard-number-input [model]="address.longitude" viewOnly width="86px"  />
                    </div>

                    <div class="flex gap-2">
                        <app-standard-string-input [model]="address.utmZone" viewOnly width="68px" />
                        <app-standard-number-input [model]="address.utmNorthing" viewOnly width="110px"  />
                        <app-standard-number-input [model]="address.utmEasting" viewOnly width="110px"  />
                    </div>
                </app-form-column>
            </mat-card>
        }

        <mat-card class="p-3">
            <google-map width="500px" height="500px"
                [options]="mapOptions()"
                (mapInitialized)="onMapReady($event)">

                @if (contextPlaceMarker(); as marker) {
                    @if (marker.isVisible()) {
                        <map-advanced-marker
                            [position]="marker.position()"
                            [title]="marker.title"
                            [options]="marker.options" >
                            {{marker.label}}
                        </map-advanced-marker>
                    }
                }


                @for (marker of markers(); track marker) {
                    @if (marker.isVisible()) {
                        <map-advanced-marker
                            [position]="marker.position()"
                            [title]="marker.title"
                            [options]="marker.options" >
                            {{marker.label}}
                        </map-advanced-marker>
                    }
                }
            </google-map>
        </mat-card>
    </div>
</div>
`
})
export class MapAddressLookupDialog
    extends InputOutputDialogForm<ILookupAddress | undefined, ILookupAddress>
    implements AfterViewInit {

    static async openAsDialog(dialogService: DialogService, context: ILookupAddress | undefined) {
        return await openInputOutputFormDialog<ILookupAddress | undefined, ILookupAddress>(
            dialogService,
            MapAddressLookupDialog,
            context,
            "select")
    }

    readonly #changeDetectorRef = inject(ChangeDetectorRef)
    readonly #ngZone = inject(NgZone)
    readonly #notifier = inject(NotificationService)
    readonly #markerList = new ListModel<MapMarker>()
    readonly markers = this.#markerList.items
    readonly contextPlaceMarker = signal<MapMarker | undefined>(undefined)
    readonly contextAddress = signal(new AddressForm(this))
    readonly isMapReady = signal(false)
    readonly standardZoom = 12
    // MapOptions: https://developers.google.com/maps/documentation/javascript/reference/map#MapOptions
    readonly mapOptions = signal<google.maps.MapOptions>({
        mapId: "my-address-lookup-map",
        // Hamburg
        center: { lat: 53.551086, lng: 9.993682 },
        zoom: 7,
        // TODO: If we want to restrict the map to Germany then we need to
        // provide the north-east and south-west coordinates.
        // restriction: {
        //     latLngBounds: ?,
        //     strictBounds: true
        // }
    })
    #map?: google.maps.Map

    readonly searchText = stringInput(this)
        .setLabel("Suchen")
        .setRequired()
        .setHint("Geben Sie den Straßennamen und Nummer gefolgt von PLZ (optional) und Stadt ein.")
        .setOnValueChanged(_ => {
            this.searchText.removeError(this.#placeNotFoundError)
        })
    //.setOnFullValueChanged(_ => this.search())
    // .addCommand({
    //     icon: "search",
    //     onTriggered: () => this.search()
    // })

    readonly #placeNotFoundError = new ValidationError(
        "#map-place-not-found#",
        "Der Ort wurde nicht gefunden."
    )

    readonly searchTextInput = viewChild("searchTextInput", { read: StandardStringInputComponent })

    constructor() {
        super()

        this.dialogSettings.title.set("Addresse suchen")
    }

    override async validate(): Promise<boolean> {
        const isValid = !!this.contextAddress().street.value() && !this.searchText.hasErrorById(this.#placeNotFoundError.id)
        if (!isValid) {
            this.settings.validationErrorMessage = "Es wurde noch keine Adresse gefunden. \n" +
                "Drücken Sie auf 'Abbrechen' falls Sie die Eingabe verwerfen und das Formular schließen wollen."
        }

        return isValid
    }

    override async submit(): Promise<void> {
        const address = this.contextAddress()
        this.outputData.set({
            street: address.street?.value(),
            zipCode: address.zipCode?.value(),
            city: address.city?.value(),
            countryName: address.countryName?.value(),
            countryCode: address.countryCode?.value(),
            countryStateName: address.countryStateName?.value(),
            countryStateCode: address.countryStateCode?.value(),

            latitude: address.latitude?.value(),
            longitude: address.longitude?.value(),
            utmZone: address.utmZone?.value(),
            utmNorthing: address.utmNorthing?.value(),
            utmEasting: address.utmEasting?.value()
        })
    }

    #wasViewInitialized?: boolean

    ngAfterViewInit() {
        this.#wasViewInitialized = true
        this.#initializeSearch(this.#map)
    }

    async onMapReady(map: google.maps.Map) {
        this.#map = map
        this.isMapReady.set(true)

        // TODO: I'm getting a compiler error when using the recommended way.
        // const { Autocomplete } = await google.maps.importLibrary("places")

        if (!google.maps.places) {
            await this.#ngZone.runOutsideAngular(async () =>
                await google.maps.importLibrary("places")
            )
        }

        await this.#initializeSearch(this.#map)
    }

    #wasSearchInitialized?: boolean

    /**
     * NOTE: When the map is initially loaded then onMapReady is called
     * *after* ngAfterViewInit.
     * When the map was already loaded before in the app, then onMapReady is called
     * *before* ngAfterViewInit.
     */
    async #initializeSearch(map: google.maps.Map | undefined) {
        if (!map || !this.#wasViewInitialized || this.#wasSearchInitialized) return

        if (this.#wasSearchInitialized) return
        this.#wasSearchInitialized = true

        const autocompleteInputElement = this.searchTextInput()?.findInputElement()
        if (autocompleteInputElement) {
            // https://developers.google.com/maps/documentation/javascript/place-autocomplete
            const autocomplete = new google.maps.places.Autocomplete(
                autocompleteInputElement,
                {
                    componentRestrictions: { country: "DE" },
                    // https://developers.google.com/places/supported_types#table3
                    types: ["geocode"]
                })
            // "Bind the map's bounds (viewport) property to the autocomplete object,
            // so that the autocomplete requests use the current map bounds for the
            // bounds option in the request.""
            autocomplete.bindTo("bounds", map)
            autocomplete.addListener("place_changed", async () => {
                this.searchText.removeError(this.#placeNotFoundError)
                const place = autocomplete.getPlace()
                if (!place.geometry) {
                    this.searchText.addError(this.#placeNotFoundError)
                }
                await this.#activatePlaceByMapsResult(place)

                this.#changeDetectorRef.detectChanges()
            })
        }

        const inputAddress = this.inputData()
        if (inputAddress) {
            await this.#activatePlaceByInputAddress(inputAddress)
        }
    }

    async #activatePlaceByInputAddress(inputAddress: ILookupAddress) {
        const latLng: google.maps.LatLng | undefined = inputAddress.latitude && inputAddress.longitude
            ? new google.maps.LatLng({ lat: inputAddress.latitude, lng: inputAddress.longitude })
            : undefined

        await this.contextAddress().clear()
        this.#setOrClearPlacePosition(latLng)
        try {
            this.contextAddress().assignFromConsumerAddress(inputAddress)

            // Also pre-fill the search-text input.
            if (inputAddress.street || inputAddress.zipCode || inputAddress.city) {
                const addressParts = [inputAddress.street?.trim() ?? ""]
                addressParts.push(`${inputAddress.zipCode?.trim() ?? ""} ${inputAddress.city?.trim() ?? ""}`.trim())
                addressParts.push(inputAddress.countryStateName?.trim() ?? "")
                // TODO: REMOVE? addressParts.push(inputAddress.countryName?.trim() ?? "")
                const formattedAddress = addressParts.filter(x => !!x).join(", ")

                this.searchText.setValue(formattedAddress)
            }
        }
        catch (error) {
            this.#notifier.showError(error)
        }
    }

    async findPlaceByPosition(position: google.maps.LatLngLiteral) {
        await this.#findPlaceWithGeocoder({
            location: position
        })
    }

    async #findPlaceByAddress(address: string): Promise<void> {
        await this.#findPlaceWithGeocoder({
            address: address
        })
    }

    async #findPlaceWithGeocoder(geocoderRequest: google.maps.GeocoderRequest): Promise<void> {
        // GeocoderRequest: https://developers.google.com/maps/documentation/javascript/reference/geocoder#GeocoderRequest

        this.searchText.removeError(this.#placeNotFoundError)

        // Geocoder: https://developers.google.com/maps/documentation/javascript/reference/geocoder
        const geocoder = new google.maps.Geocoder()

        if (!geocoderRequest.componentRestrictions) {
            // GeocoderComponentRestrictions: https://developers.google.com/maps/documentation/javascript/reference/geocoder#GeocoderComponentRestrictions
            geocoderRequest.componentRestrictions = {
                // Restrict to Germany.
                country: "DE"
            }
        }
        const geocoderResponse: google.maps.GeocoderResponse = await geocoder.geocode(geocoderRequest)

        const geocoderResults: google.maps.GeocoderResult[] = geocoderResponse.results
        if (geocoderResults?.length) {
            await this.#activatePlaceByMapsResult(geocoderResults[0])
        }
        else {
            this.searchText.addError(this.#placeNotFoundError)
        }
    }

    async #activatePlaceByMapsResult(mapsResult: google.maps.places.PlaceResult | google.maps.GeocoderResult) {
        this.#setOrClearPlacePosition(mapsResult.geometry?.location)
        await this.contextAddress().clear()
        try {
            const addressModel = MapAddressModel.fromGoogleMapsResult(mapsResult)
            this.contextAddress().assignFrom(addressModel)
        }
        catch (error) {
            this.#notifier.showError(error)
        }
    }

    #setMapZoom(value: number): void {
        this.#map?.setZoom(value)
    }

    #setMapCenter(position: google.maps.LatLng): void {
        this.#map?.setCenter(position)
    }

    #setOrClearPlacePosition(position: google.maps.LatLng | undefined) {
        if (position) {
            this.#setMapCenter(position)
            this.#setMapZoom(this.standardZoom)
            this.contextPlaceMarker.set(new MapMarker({
                position: position,
                title: "",
                options: {}
            }))
        }
        else {
            this.contextPlaceMarker.set(undefined)
        }
    }

    #setMarker(positionLatLng: google.maps.LatLng) {
        // let glyphLabel = document.createElement("div");
        // // set style and classes as needed
        // glyphLabel.style = 'color: white; font-size: 17px;';
        // glyphLabel.classList.add('classname');
        // glyphLabel.innerText = length.toFixed(2) + ' m';
        // let iconImage = new google.maps.marker.PinElement({
        //   glyph: glyphLabel,
        // });

        // NOTE: AdvancedMarkerElement is an HTMLElement now.
        // TODO: Label and animation is not available on AdvancedMarkerElement
        //   - Label can be created as content HTML?
        //   - Animation needs to be done using Angular animations.

        //  google.maps.marker.PinElement

        const marker = new MapMarker({
            position: positionLatLng,
            title: "!!!ICH BIN HIER!!!"
        })

        this.#markerList.add(marker)
    }

    #clearMarkers() {
        this.#markerList.clear()
    }
}
