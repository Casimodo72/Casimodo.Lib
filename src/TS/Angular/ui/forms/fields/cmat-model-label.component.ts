
import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { FormInputModel } from "@lib/models/core"

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: "mat-label[cmatModel]",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: "{{cmatModel().label}}"
})
// eslint-disable-next-line @angular-eslint/component-class-suffix
export class CMatModelLabel {
    readonly cmatModel = input.required<FormInputModel>()
}
