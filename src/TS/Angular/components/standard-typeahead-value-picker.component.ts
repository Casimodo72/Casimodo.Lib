
import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { MatAutocompleteModule } from "@angular/material/autocomplete"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { CCPropDirective, PropErrorsComponent, PropLabelComponent } from "@lib/forms"
import { PickerFormProp, PickerItemModel } from "@lib/models"

@Component({
    selector: "app-standard-typeahead-value-picker",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule, MatAutocompleteModule,
        PropLabelComponent, CCPropDirective, PropErrorsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model(); as model) {
    <mat-form-field
        appearance="outline"
        [ngStyle]="customStyle()">

        @if (model.label) {
            <mat-label [ccProp]="model" />
        }

        <input matInput ngModel
            [ccProp]="model"
            [matAutocomplete]="myAutocomplete"
            [disabled]="model.isReadOnly()"
            [ariaReadOnly]="model.isReadOnly()">

        <!-- @if (something) {
        <app-button matSuffix type="add" (click)="..." />
        } -->

        <mat-autocomplete #myAutocomplete="matAutocomplete" [displayWith]="displayFn">
            @if (model.hasEmptyItem()) {
                <mat-option [value]="null">
                    {{model.emptyText()}}
                </mat-option>
            }

            @for (pickItem of model.pickableItems(); track pickItem.id) {
                <mat-option [value]="pickItem">
                    {{pickItem.toDisplayText()}}
                </mat-option>
            }
        </mat-autocomplete>

        <mat-error [ccProp]="model" />
    </mat-form-field>

    <div>{{model.filter.value()}}</div>
}
`
})
export class StandardTypeaheadValuePickerComponent {
    // TODO: REMOVE: readonly emptyItem = PickerFormProp.EmptyItem
    readonly model = input.required<PickerFormProp>()
    readonly customStyle = input<any | undefined>(undefined)

    displayFn(value: any): string {
        return value instanceof PickerItemModel
            ? value.toDisplayText() ?? ""
            : ""
    }
}
