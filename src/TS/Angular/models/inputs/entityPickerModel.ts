import { IFormPart } from "../core"
import { _standardEntityIdProp } from "./entityPrimitives"
import { PickerModel } from "./pickerModel"

/**
 * The entity picker is used for small data sets.
 * E.g. we don't need a full lookup-selector to select a country-state since it is a small data set.
 */
export class EntityPickerModel<TEntity extends { Id?: string | null } = any> extends PickerModel<Partial<TEntity>> {
    // TODO: Magic "Id" prop in "TEntity extends { Id: string }".

    constructor(parent: IFormPart, initialEntity?: Partial<TEntity>) {
        super(parent, initialEntity)

        this._valueIdProp = _standardEntityIdProp
    }
}
