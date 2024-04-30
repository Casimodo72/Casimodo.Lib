import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatAutocompleteModule } from "@angular/material/autocomplete"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors, CMatModelLabel } from "@lib/forms"
import { PickerFormProp, PickerItemModel } from "@lib/models"

// TODO: Support on-demand loading of pick-items (e.g. for table column filters).
@Component({
    selector: "app-standard-typeahead-value-picker",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule, MatAutocompleteModule,
        CMatModelLabel, CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [":host { display: block; }"],
    template: `
@if (model(); as model) {
    <mat-form-field
        appearance="outline"
        [ngStyle]="groupStyle()">

        @if (model.label) {
            <mat-label [cmatModel]="model" />
        }

        <input matInput ngModel
            [cmatModel]="model"
            [matAutocomplete]="myAutocomplete"
            [disabled]="model.isReadOnly()"
            [ariaReadOnly]="model.isReadOnly()">

        <!-- @if (something) {
        <app-button matSuffix type="add" (click)="..." />
        } -->

        <!-- TODO: Does mat-autocomplete also have something similar to mat-select-trigger? -->

        <!-- @displayWith is needed because otherwise AM won't know
             how to render the selected item.
        -->
        <mat-autocomplete #myAutocomplete="matAutocomplete" [displayWith]="displayFn">
            @if (model.isMaxPickableItemCountExceeded()) {
                <!-- TODO: Since Angular material can't virtualize the displayed items,
                     we need to restrict the number of displayable items for now.
                     Think about implementing pagination in the picker UI. -->
                <div class="w-full py-2 px-4 app-info-xs">(Zu viele Ergebnisse)</div>
            }

            <!-- TODO: Maybe display the special - non-data - options with a differerent background. -->

            <!-- TODO: Maybe display "no match" if no pickable items match. -->

            @if (!model.value() && model.filter.value()) {
                <mat-option [value]="null" class="mat-small">
                    (Suchtext zurücksetzen)
                </mat-option>
            }
            @if (model.hasEmptyItem() && model.value()) {
                <mat-option [value]="null" class="mat-small">
                    (Auswahl entfernen)
                </mat-option>
            }

            <!-- TODO: Think about providing a component to be used for display. -->
            @for (pickItem of model.pickableItems(); track pickItem.id) {
                <mat-option [value]="pickItem">
                    {{pickItem.toDisplayText()}}
                </mat-option>
            }
        </mat-autocomplete>

        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardTypeaheadValuePickerComponent {
    readonly model = input.required<PickerFormProp>()
    readonly groupStyle = input<any | undefined>(undefined)

    displayFn(value: any): string {
        return value instanceof PickerItemModel
            ? value.toDisplayText() ?? ""
            : ""
    }
}
