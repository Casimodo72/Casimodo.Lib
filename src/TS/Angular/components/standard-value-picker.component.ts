import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatButtonModule } from "@angular/material/button"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatSelectModule } from "@angular/material/select"

import { CMatModel, CMatModelErrors, CMatModelLabel } from "@lib/forms"
import { PickerFormProp } from "@lib/models"

// TODO: Support on-demand loading of pick-items (e.g. for table column filters).
@Component({
    selector: "app-standard-value-picker",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatSelectModule, MatButtonModule,
        CMatModelLabel, CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [":host { display: block; }"],
    template: `
@if (model(); as model) {
    <mat-form-field [ngStyle]="groupStyle()" [ngClass]="groupClass()" appearance="outline">
        @if (model.label) {
            <mat-label [cmatModel]="model" />
        }
        <mat-select [cmatModel]="model"
            ngModel
            [disabled]="disabled() || model.isReadOnly()"
            [ariaReadOnly]="disabled() || model.isReadOnly()">

            <!-- <mat-select-trigger>
                @if (model.selectedItem(); as pickItem) {
                    {{pickItem.toDisplayText()}}
                }
            </mat-select-trigger> -->

            <!-- TODO: Think about providing a component to be used for display. -->
            <!-- <mat-select-trigger>
                @if (model.selectedItem(); as pickItem) {
                    ...
                }
            </mat-select-trigger> -->

            @if (model.hasEmptyItem() && model.value()) {
            <mat-option [value]="null">
                (Auswahl entfernen)
            </mat-option>
            <!-- {{model.emptyText()}} -->
            }

            <!-- TODO: Think about providing a component to be used for display. -->
            @for (pickItem of model.pickableItems(); track pickItem.id) {
                <mat-option [value]="pickItem">
                    {{pickItem.toDisplayText()}}
                </mat-option>
            }
        </mat-select>
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardValuePickerComponent {
    readonly model = input.required<PickerFormProp>()
    readonly disabled = input(false)
    readonly groupClass = input<any | undefined>(undefined)
    readonly groupStyle = input<any | undefined>(undefined)
}
