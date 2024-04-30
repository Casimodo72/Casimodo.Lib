
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors, CMatModelLabel } from "@lib/forms"
import { StringFormProp } from "@lib/models"

// TODO: Use an abstract base component for all standard input components.
@Component({
    selector: "app-standard-string-input",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule,
        CMatModelLabel, CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-form-field [ngStyle]="groupStyle()" appearance="outline">
        @if (model.label) {
            <mat-label [cmatModel]="model" />
        }
        <!-- TODO: Get rid of binding to ngModel and set value via cmatModel directive. -->
        <input matInput
            [ngModel]="model.value()"
            [cmatModel]="model"
            [disabled]="model.isReadOnly()"
            [ariaReadOnly]="model.isReadOnly()"
            autocomplete="off"
            spellcheck="false" />
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardStringInputComponent {
    readonly model = input.required<StringFormProp>()
    readonly groupStyle = input<any | undefined>(undefined)
}
