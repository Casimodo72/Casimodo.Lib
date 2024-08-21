/* eslint-disable @angular-eslint/no-host-metadata-property */

import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, inject } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule, MatSuffix } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms"
import { StreetInputWithAddressLookupModel } from "@lib/models/inputs/streetInputWithAddressLookupModel"
import { DialogService } from "@lib/ui/dialogs/dialog.service"

import { StandardInputComponent } from "./standardInputComponent"
import { MapAddressLookupDialog } from "./maps/map-address-lookup.component"
import { ButtonComponent } from "./button.component"

// TODO: Use an abstract base component for all standard input components.
@Component({
    selector: "app-standard-street-input-with-address-lookup",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule, MatSuffix,
        CMatModel, CMatModelErrors,
        ButtonComponent
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "[class.full-flex-item]": "flexStretch()"
    },
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
        <!-- TODO: Get rid of binding to ngModel and set value via cmatModel directive. -->
        <input matInput
            [ngModel]="model.value()"
            [cmatModel]="model"
            [required]="effectiveIsRequired()"
            [disabled]="effectiveIsDisabled()"
            [readonly]="effectiveIsReadOnly()"
            [ariaReadOnly]="effectiveIsReadOnly()"
            [attr.tabindex]="effectiveIsReadOnly() ? -1 : undefined"
            autocomplete="off"
            spellcheck="false" />
        @if (!effectiveIsDisabled() && !effectiveIsReadOnly()) {
            <app-button type="search" matSuffix (click)="lookup()" />
        }
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardStreetInputWithAddressLookupComponent
    extends StandardInputComponent<StreetInputWithAddressLookupModel> {
    readonly #dialogService = inject(DialogService)

    async lookup() {
        const address = this.model().getAddress()
        const lookupResult = await MapAddressLookupDialog.openAsDialog(this.#dialogService, address)
        if (lookupResult.hasSucceeded && lookupResult.data) {
            this.model().selectAddress(lookupResult.data)
        }
    }
}
