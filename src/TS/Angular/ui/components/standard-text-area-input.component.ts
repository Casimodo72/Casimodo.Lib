
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms/fields"
import { TextAreaInputModel } from "@lib/models/inputs/textInputModels"

import { StandardInputComponent } from "./standardInputComponent"

// TODO: Use an abstract base component for all standard input components.
@Component({
    selector: "app-standard-text-area-input",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule,
        CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-form-field
        [ngClass]="effectiveGroupClass()"
        [ngStyle]="effectiveGroupStyle()"
        [appearance]="effectiveAppearance()"
        [floatLabel]="effectiveLabelMode()">
        @if (effectiveLabel()) {
            <mat-label>{{effectiveLabel()}}</mat-label>
        }
        <!-- TODO: Get rid of binding to ngModel and set value via cmatModel directive. -->
        <textarea matInput
            [cmatModel]="model"
            [ngModel]="model.value()"
            (ngModelChange)="model.setValue($event)"
            [required]="effectiveIsRequired()"
            [disabled]="effectiveIsDisabled()"
            [readonly]="effectiveIsReadOnly()"
            [ariaReadOnly]="effectiveIsReadOnly()"
            [attr.tabindex]="effectiveCanFocus() ? undefined : -1"
            cdkTextareaAutosize
            [cdkAutosizeMinRows]="model.rowCount()"
            [cdkAutosizeMaxRows]="model.maxRowCount()"
            autocomplete="new-foo"
            spellcheck="false"></textarea>
        <!--
            TODO: ? cdkFocusInitial
        -->
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardTextAreaInputComponent
    extends StandardInputComponent<TextAreaInputModel> {
}
