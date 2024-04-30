
import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { FormProp } from "@lib/models/props"

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: "mat-error[cmatModel]",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@for (error of cmatModel().errors(); track error.id) {
    <span>{{error.message}}</span>
}
`
})
// eslint-disable-next-line @angular-eslint/component-class-suffix
export class CMatModelErrors {
    readonly cmatModel = input.required<FormProp>()
}
