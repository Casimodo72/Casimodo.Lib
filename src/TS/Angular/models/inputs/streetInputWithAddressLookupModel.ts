import { StringInputModel } from "./textInputModels"

export interface ILookupAddress {
    street?: string | null,
    zipCode?: string | null,
    city?: string | null,
    countryStateName?: string | null
    countryStateCode?: string | null
    countryName?: string | null
    countryCode?: string | null
    latitude?: number | null
    longitude?: number | null
    utmZone?: string | null
    utmNorthing?: number | null
    utmEasting?: number | null
}

type AddressProvidingFn = (streetModel: StreetInputWithAddressLookupModel) => ILookupAddress

type OnAddressSelectedFn = (address: ILookupAddress) => void

export class StreetInputWithAddressLookupModel extends StringInputModel {
    getAddress(): ILookupAddress | undefined {
        let address = this.#addressProvider?.(this)
        if (!address?.street && this.value()) {
            address ??= {}
            address.street = this.value()
        }

        return address
    }

    setAddress(addressProvider: AddressProvidingFn): this {
        this.#addressProvider = addressProvider

        return this
    }
    #addressProvider?: AddressProvidingFn

    selectAddress(address: ILookupAddress) {
        this.#onAddressSelectedFn?.(address)
    }

    setOnAddressSelected(onAddressSelectedFn: OnAddressSelectedFn): this {
        this.#onAddressSelectedFn = onAddressSelectedFn

        return this
    }
    #onAddressSelectedFn?: OnAddressSelectedFn
}
