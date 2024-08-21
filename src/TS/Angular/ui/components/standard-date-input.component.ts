import { ChangeDetectionStrategy, Component } from "@angular/core"
import { CommonModule } from "@angular/common"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModelErrors } from "@lib/ui/forms"
import { AnyLuxonDateTimeInputModel } from "@lib/models"
import { StandardInputComponent } from "./standardInputComponent"
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from "@angular/material/datepicker"

@Component({
    selector: "app-standard-date-input",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatFormFieldModule, MatInputModule,
        MatDatepicker, MatDatepickerInput, MatDatepickerToggle,
        CMatModelErrors
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
    <!-- TODO: If we specific type="date" then a duplicate (native) date-picker icon
       appears in the input. -->
    <input  matInput
        [matDatepicker]="picker"

        [ngModel]="model.value()"
        (ngModelChange)="model.setValue($event)"
        [required]="effectiveIsRequired()"
        [disabled]="effectiveIsDisabled()"
        [readonly]="effectiveIsReadOnly()"
        [ariaReadOnly]="effectiveIsReadOnly()"
        [attr.title]="model.label"
        [attr.aria-label]="model.label">

    <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>

    <mat-error [cmatModel]="model" />
    <!-- TODO: Can't really use min/max because that does not work accross day boundaries.
         Plus, I see no visual effect in the time-picker when using min/max :-( Hey, it's Angular material.
    -->
    <mat-datepicker #picker></mat-datepicker>
</mat-form-field>
}
`
})
export class StandardDateInputComponent
    extends StandardInputComponent<AnyLuxonDateTimeInputModel> {
}
