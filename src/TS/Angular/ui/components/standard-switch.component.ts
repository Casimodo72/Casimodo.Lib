import { ChangeDetectionStrategy, Component, } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatSlideToggleModule } from "@angular/material/slide-toggle"

import { BooleanInputModel } from "@lib/models/inputs/miscInputModels"

import { StandardInputComponent } from "./standardInputComponent"

// TODO: Add validation error list.
// NOTE that AM's mat-form-field is not intended for a switch :-(
// Thus we'll have to handle boolean inputs differently, which is unfortunate.
@Component({
    selector: "app-standard-switch",
    standalone: true,
    imports: [FormsModule, MatSlideToggleModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <!-- TODO: How to make mat-slide-toggle read-only? There is no [readonly]. -->
    <mat-slide-toggle
        [ngModel]="model.value()"
        (ngModelChange)="model.setValue($event)"
        [disabled]="effectiveIsDisabled()"
        [ariaReadOnly]="model.isReadOnly()">
        {{effectiveLabel()}}
    </mat-slide-toggle>
}
`
})
export class StandardSwitchComponent
    extends StandardInputComponent<BooleanInputModel> {
}
