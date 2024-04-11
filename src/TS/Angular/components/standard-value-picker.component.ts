
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatSelectModule } from "@angular/material/select"
import { CCPropDirective, PropErrorsComponent, PropLabelComponent } from "@lib/forms"
import { PickerFormProp } from "@lib/models"

@Component({
    selector: "app-standard-value-picker",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatSelectModule,
        PropLabelComponent, CCPropDirective, PropErrorsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-form-field [ngStyle]="customStyle()" appearance="outline">
        @if (model.label) {
            <mat-label [ccProp]="model" />
        }
        <mat-select [ccProp]="model"
            ngModel
            [disabled]="model.isReadOnly()"
            [ariaReadOnly]="model.isReadOnly()">
            <!-- <mat-select-trigger>
                @if (model.selectedItem(); as pickItem) {
                    {{pickItem.data}}
                }
            </mat-select-trigger> -->

            @if (model.hasEmptyItem()) {
            <mat-option [value]="null">
                {{model.emptyText()}}
            </mat-option>
            }

            @for (pickItem of model.pickableItems(); track pickItem.id) {
                <!-- TODO: Include null value. -->
                <mat-option [value]="pickItem">
                    {{pickItem.toDisplayText()}}
                </mat-option>
            }
        </mat-select>
        <mat-error [ccProp]="model" />
    </mat-form-field>
}
`
})
export class AppStandardValuePickerComponent {
    readonly emptyItem = PickerFormProp.EmptyItem
    readonly model = input.required<PickerFormProp>()
    readonly customStyle = input<any | undefined>(undefined)
}
