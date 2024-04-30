import { ChangeDetectionStrategy, Component, Input, input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors, CMatModelLabel } from "@lib/forms"
import { AnyDateTimeFormProp } from "@lib/models"

@Component({
    selector: "app-standard-time-input",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormsModule,
        MatFormFieldModule, MatInputModule,
        CMatModelLabel, CMatModel, CMatModelErrors, MatIconModule
    ],
    styles: [`
        :host { display: contents; }
    `],
    template: `
@if (model(); as model) {
<mat-form-field>
    @if (model.label) {
        <mat-label>{{model.label}}</mat-label>
    }
    <input matInput type="time"
        [cmatModel]="model"
        [ngModel]="model.timeValueAsText()"
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
export class StandardTimeInputComponent {
    readonly model = input.required<AnyDateTimeFormProp>()
}
