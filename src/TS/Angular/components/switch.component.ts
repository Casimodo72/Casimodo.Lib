import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { MatSlideToggleModule } from "@angular/material/slide-toggle"

import { BooleanFormProp } from "@lib/models/props"

// TODO: Add validation error list.
// NOTE that AM's mat-form-field is not intended for a switch :-(
// Thus we'll have to handle boolean props a bit differently, which is unfortunate.
@Component({
    selector: "app-switch",
    standalone: true,
    imports: [FormsModule, MatSlideToggleModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-slide-toggle [ngModel]="model.value()" (ngModelChange)="model.setValue($event)">
        {{model.label}}
    </mat-slide-toggle>
}
`
})
export class AppSwitchComponent {
    readonly model = input.required<BooleanFormProp>()
}
