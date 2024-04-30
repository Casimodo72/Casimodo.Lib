import { CommonModule } from "@angular/common"
import { AfterViewInit, ChangeDetectionStrategy, Component, computed, input } from "@angular/core"

import { MatIconModule } from "@angular/material/icon"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"

import { PaginationModel } from "./tableModels"

import { FormItemComponent } from "@lib/forms/form.component"
import { PickerFormProp } from "@lib/models"
import { StandardValuePickerComponent } from "@lib/components"
import { MatButtonModule } from "@angular/material/button"

@Component({
    selector: "app-paginator",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
        StandardValuePickerComponent
    ],
    styles: [":host { display: block; } "],
    template: `
@if (model(); as model) {
    <div class="w-full flex items-center gap-2" [attr.disabled]="isEffectivelyDisabled()">

        <div>
            Seite {{model.index() + 1}}
            @if (model.lastPageNumber(); as lastPageNumber) {
                von {{lastPageNumber}}
            }
        </div>

        <div class="flex gap-1">
            <button mat-icon-button [disabled]="isEffectivelyDisabled() || !model.canMoveToFirst()" (click)="model.moveToFirst()"><mat-icon>first_page</mat-icon></button>
            <button mat-icon-button [disabled]="isEffectivelyDisabled() || !model.canMoveToPrevious()" (click)="model.moveToPrevious()"><mat-icon>navigate_before</mat-icon></button>
            <button mat-icon-button [disabled]="isEffectivelyDisabled() ||!model.canMoveToNext()" (click)="model.moveToNext()"><mat-icon>navigate_next</mat-icon></button>
            @if (model.isMoveToLastAvailable()) {
                <button mat-icon-button [disabled]="isEffectivelyDisabled() ||!model.canMoveToLast()" (click)="model.moveToLast()"><mat-icon>last_page</mat-icon></button>
            }
        </div>

        @if (model.isSizeSelectable()) {
            <app-standard-value-picker [model]="pageSizePicker" [disabled]="isEffectivelyDisabled()" [groupStyle]="{ 'width': '80px'}"/>
            <div>je Seite</div>
        }

        <div class="ms-auto">
            {{model.count()}} von {{model.totalCount() ?? "-"}}
        </div>
    </div>
}
`
})
export class PaginatorComponent extends FormItemComponent
    implements AfterViewInit {
    readonly pageSizePicker = new PickerFormProp<number>(this)
    readonly isEffectivelyDisabled = computed(() => this.disabled() || this.model().isBusy())

    readonly model = input.required<PaginationModel>()
    readonly disabled = input(false)

    ngAfterViewInit(): void {
        this.pageSizePicker.setPickValues(this.model().availableSizes())
        this.pageSizePicker.setValue(this.model().size())
        this.pageSizePicker.setOnValueChanged(pageSize => {
            this.model().setSize(pageSize ?? this.model().availableSizes()[0] ?? 5)
        })
    }
}
