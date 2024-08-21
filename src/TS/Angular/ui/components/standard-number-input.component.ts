
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatError, MatFormField, MatHint, MatLabel } from "@angular/material/form-field"
import { MatInput } from "@angular/material/input"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms"
import { NumberInputModel } from "@lib/models"

import { StandardInputComponent } from "./standardInputComponent"
import { AppMatReadOnlyInputHintComponent } from "./mat-read-only-input-hint.component"

// TODO: Use an abstract base component for all standard input components.
@Component({
    selector: "app-standard-number-input",
    standalone: true,
    imports: [
        CommonModule, FormsModule,
        MatFormField, MatLabel, MatInput, MatHint, MatError,
        AppMatReadOnlyInputHintComponent,
        CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-form-field
        [ngClass]="effectiveGroupClass()"
        [class.app-mat-view-only-form-field]="effectiveIsViewOnly()"
        [ngStyle]="effectiveGroupStyle()"
        [appearance]="effectiveAppearance()"
        [floatLabel]="effectiveLabelMode()">
        @if (effectiveLabel()) {
            <mat-label>{{effectiveLabel()}}</mat-label>
        }
        <!-- TODO: Get rid of binding to ngModel and set value via cmatModel directive. -->
        <input matInput
            type="number"
            [ngModel]="model.value()"
            [cmatModel]="model"
            [required]="effectiveIsRequired()"
            [disabled]="effectiveIsDisabled()"
            [readonly]="effectiveIsReadOnly()"
            [ariaReadOnly]="effectiveIsReadOnly()"
            [attr.tabindex]="effectiveCanFocus() ? undefined : -1"
            autocomplete="off"/>
        <!--
            TODO: REMOVE: Displaying a hint for read-only inputs takes away too much
            space. The Material 3 spec is strange.
        @if (model.isReadOnly()) {
            <mat-hint class="app-mat-read-only-input-hint">Nicht bearbeitbar</mat-hint>
        } -->
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardNumberInputComponent
    extends StandardInputComponent<NumberInputModel> {
}
