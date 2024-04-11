
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"

import { FormProp } from "@lib/models"

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: "mat-label[ccProp]",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: "{{ccProp.label}}"
})
export class PropLabelComponent {
    @Input({ required: true }) ccProp!: FormProp
}
