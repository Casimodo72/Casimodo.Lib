
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"

import { FormProp } from "@lib/models/props"

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: "mat-error[ccProp]",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @for (error of ccProp.errors(); track error.id) {
            <span>{{error.message}}</span>
        }
    `
})
export class PropErrorsComponent {
    @Input({ required: true }) ccProp!: FormProp
}
