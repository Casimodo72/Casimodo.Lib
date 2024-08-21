/* eslint-disable @angular-eslint/no-host-metadata-property */

import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, ElementRef, viewChild } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms/fields"
import { StringInputModel } from "@lib/models/inputs/textInputModels"

import { StandardInputComponent } from "./standardInputComponent"

// TODO: Use an abstract base component for all standard input components.
@Component({
    selector: "app-standard-string-input",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatInputModule,
        CMatModel, CMatModelErrors],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "[class.full-flex-item]": "flexStretch()"
    },
    template: `
@if (model(); as model) {
    <mat-form-field
        [ngClass]="effectiveGroupClass()"
        [class.app-mat-view-only-form-field]="effectiveIsViewOnly()"
        [ngStyle]="effectiveGroupStyle()"
        [appearance]="effectiveAppearance()"
        [floatLabel]="effectiveLabelMode()"
        [subscriptSizing]="effectiveErrorSubscriptSizing()">
        @if (effectiveLabel()) {
            <mat-label>{{effectiveLabel()}}</mat-label>
        }
        <!-- TODO: Get rid of binding to ngModel and set value via cmatModel directive. -->
        <input matInput #input
            [ngModel]="model.value()"
            [cmatModel]="model"
            [required]="effectiveIsRequired()"
            [disabled]="effectiveIsDisabled()"
            [readonly]="effectiveIsReadOnly()"
            [ariaReadOnly]="model.isReadOnly()"
            [attr.tabindex]="effectiveCanFocus() ? undefined : -1"
            autocomplete="new-foo"
            spellcheck="false" />
        <!-- TODO: Angular Material still changes the autocomplete value
            from "new-foo" to "off" :-/
        -->

        <mat-error [cmatModel]="model" />

    </mat-form-field>
    <!-- TODO: REMOVE: @if (minErrorHeight() && !model.errors()?.length) {
        <div [style.min-height]="minErrorHeight()"></div>
    } -->
}
`
})
export class StandardStringInputComponent
    extends StandardInputComponent<StringInputModel> {

    readonly inputRef = viewChild<ElementRef<HTMLInputElement>>("input")

    findInputElement(): HTMLInputElement | undefined {
        return this.inputRef()?.nativeElement
    }
}
