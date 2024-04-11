import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"

import { CCPropDirective, PropErrorsComponent, PropLabelComponent } from "@lib/forms"
import { AnyDateTimeFormProp } from "@lib/models"

@Component({
    selector: "app-time-input",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormsModule,
        MatFormFieldModule, MatInputModule,
        PropLabelComponent, CCPropDirective, PropErrorsComponent, MatIconModule
    ],
    styles: [`
        :host { display: contents; }
    `],
    template: `
<mat-form-field>
    @if (prop.label) {
        <mat-label>{{prop.label}}</mat-label>
    }
    <input matInput type="time" [ccProp]="prop"
        [ngModel]="prop.timeValueAsText()"
        [attr.title]="prop.label"
        [attr.aria-label]="prop.label" />
    <mat-error [ccProp]="prop" />
    <!-- TODO: Can't really use min/max because that does not work accross day boundaries.
         Plus, I see no visual effect in the time-picker when using min/max :-/ Hey, it's Angular.
    -->
</mat-form-field>
    `
})
export class TimeInputComponent {
    @Input({ required: true }) prop!: AnyDateTimeFormProp
}
