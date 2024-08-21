import { DateTime } from "luxon"

import type { IFormPart } from "../core"
import { BooleanInputModel, NumberInputModel } from "./miscInputModels"
import { StringInputModel, TextAreaInputModel } from "./textInputModels"
import { EntityLookupModel } from "./entityLookupModel"
import { EntityPickerModel } from "./entityPickerModel"
import { DateInputModel, TimeInputModel } from "./dateTimeInputModels"

export function numberInput(parent: IFormPart, initialValue?: number | null) {
    return new NumberInputModel(parent, initialValue)
}

export function stringInput(parent: IFormPart, initialValue?: string | null): StringInputModel {
    return new StringInputModel(parent, initialValue)
}

export function textAreaInput(parent: IFormPart) {
    return new TextAreaInputModel(parent)
}

export function booleanInput(parent: IFormPart, initialValue?: boolean) {
    return new BooleanInputModel(parent, initialValue ?? false)
}

export function timeInput(parent: IFormPart, initialValue?: DateTime | null) {
    return new TimeInputModel(parent, initialValue)
}

export function dateInput(parent: IFormPart, initialValue?: DateTime | null) {
    return new DateInputModel(parent, initialValue)
}

export function entityLookup<TEntity>(parent: IFormPart) {
    return new EntityLookupModel<Partial<TEntity>>(parent)
}

export function entityPicker<TEntity>(parent: IFormPart) {
    return new EntityPickerModel<Partial<TEntity>>(parent)
}
