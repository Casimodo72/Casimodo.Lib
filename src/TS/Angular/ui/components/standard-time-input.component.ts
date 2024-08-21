import { ChangeDetectionStrategy, Component } from "@angular/core"
import { CommonModule } from "@angular/common"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms/fields"
import { AnyLuxonDateTimeInputModel } from "@lib/models/inputs/dateTimeInputModels"

import { StandardInputComponent } from "./standardInputComponent"

@Component({
    selector: "app-standard-time-input",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatFormFieldModule, MatInputModule,
        CMatModel, CMatModelErrors, MatIconModule
    ],
    styles: [`
        :host { display: contents; }
    `],
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
    <input type="time" matInput
        [cmatModel]="model"
        [ngModel]="model.timeValueAsText()"
        [required]="effectiveIsRequired()"
        [disabled]="effectiveIsDisabled()"
        [readonly]="effectiveIsReadOnly()"
        [ariaReadOnly]="effectiveIsReadOnly()"
        [attr.title]="model.label"
        [attr.aria-label]="model.label" />
    <mat-error [cmatModel]="model" />
    <!-- TODO: Can't really use min/max because that does not work accross day boundaries.
         Plus, I see no visual effect in the time-picker when using min/max :-( Hey, it's Angular material.
    -->
</mat-form-field>
}
`
})
export class StandardTimeInputComponent
    extends StandardInputComponent<AnyLuxonDateTimeInputModel> {
}
