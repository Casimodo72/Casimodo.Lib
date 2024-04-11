
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { FormProp } from "@lib/models"

@Component({
    selector: "app-readonly-text-field",
    standalone: true,
    imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <mat-form-field>
        @if (ccProp) {
            <mat-label>{{ccProp.label}}</mat-label>
        } @else {
            <mat-label>{{label}}</mat-label>
        }

        <!-- @if (matIcon) {
            <mat-icon matPrefix>{{matIcon}}</mat-icon>
        } -->

        @if (ccProp) {
            <input matInput readonly [value]="ccProp.value()"  />
        } @else {
            <input matInput readonly [value]="value"  />
        }
    </mat-form-field>
    `
})
export class ReadOnlyTextFieldComponent {
    @Input() label!: string
    @Input() value!: string
    @Input() ccProp: FormProp | undefined = undefined
    @Input() matIcon: string | undefined = undefined
}
