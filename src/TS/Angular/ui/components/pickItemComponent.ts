import { Directive, input } from "@angular/core"

import { PickerItemModel } from "@lib/models"

@Directive()
export abstract class PickItemComponent<TPickerItem extends PickerItemModel = PickerItemModel> {
    readonly model = input.required<TPickerItem>()
}
