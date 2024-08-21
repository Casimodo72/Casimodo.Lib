import { NgComponentOutlet } from "@angular/common"
import { Component, ChangeDetectionStrategy, AfterViewInit, input, signal, Type, Directive } from "@angular/core"

import { MatCheckbox } from "@angular/material/checkbox"

import { PropPath } from "@lib/data/utils"
import { FormPartComponent } from "@lib/ui/forms/formPartComponent"
import { PickerModel } from "@lib/models/inputs/pickerModel"
import { StringInputModel } from "@lib/models/inputs/textInputModels"

import { StandardTypeaheadPickerComponent } from "@lib/ui/components/standard-typeahead-picker.component"
import { StandardStringInputComponent } from "@lib/ui/components/standard-string-input.component"
import type { TableColumnModel, TableModel } from "./tableModels"
import { TableFilterType } from "./tablePrimitives"

@Directive()
export abstract class TableFilterComponent extends FormPartComponent {
    readonly column = input.required<TableColumnModel>()
}

@Component({
    selector: "app-table-column-filter-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgComponentOutlet],
    template: `
@if (column(); as column) {
    @if (filterComponentType(); as filterComponentType) {
        <ng-template *ngComponentOutlet="filterComponentType; inputs: { column: column }"></ng-template>
    }
}
`
})
export class TableFilterRendererComponent implements AfterViewInit {
    readonly column = input.required<TableColumnModel>()

    readonly filterComponentType = signal<Type<TableFilterComponent> | undefined>(undefined)

    ngAfterViewInit(): void {
        const column = this.column()
        if (!column) return

        if (column.filter?.type !== undefined) {
            this.filterComponentType.set(this.getFilterComponent(column.filter.type))
        }
    }

    // TODO: Move this to a service.
    getFilterComponent(type: TableFilterType): Type<TableFilterComponent> | undefined {
        if (type === TableFilterType.StringPickerWithTypeahead) {
            return TypeaheadPickerTableColumnFilterComponent
        }
        else if (type === TableFilterType.String) {
            return StringTableColumnFilterComponent
        }

        return undefined
    }
}

@Component({
    selector: "app-typeahead-picker-table-column-filter",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [StandardTypeaheadPickerComponent],
    template: `
<app-standard-typeahead-picker [model]="picker" />
`
})
export class TypeaheadPickerTableColumnFilterComponent
    extends TableFilterComponent
    implements AfterViewInit {
    // TODO: Assess deferred loading of filter picker values:
    // https://stackblitz.com/edit/angular-mat-select-deferred-loading?file=app%2Fselect-deferred-example.ts

    readonly picker = new PickerModel<any>(this)
        .setHasNullValue(true)
        .setOnValueChanged(value => this.#onValuePicked(value))

    async #onValuePicked(value: any) {
        const column = this.column()
        const filter = column?.filter
        if (!filter) return

        if (value && filter.source?.value) {
            value = filter.source?.value.getValue(value)
        }

        await column.applyFilterValue(value)
    }

    #displayProp?: PropPath

    async ngAfterViewInit() {
        const source = this.column()?.filter?.source
        if (!source) return

        if (source.text) {
            this.#displayProp = source.text
            this.picker.setDisplayFn(data => this.#displayProp!.getValue(data) ?? "")
            this.picker.setFilterProp(source.text)
        }

        const pickValues = await source.load()

        this.picker.setPickValues(pickValues)
    }
}

@Component({
    selector: "app-string-table-column-filter",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [StandardStringInputComponent],
    template: `
<app-standard-string-input [model]="stringInput" />
`
})
export class StringTableColumnFilterComponent extends TableFilterComponent {
    readonly stringInput = new StringInputModel(this)
        .setOnKeyUp((ev) => {
            if (ev.key === "Enter") {
                this.#onValueChanged()
            }
        })
        .setOnFullValueChanged(_ => this.#onValueChanged())

    #isApplyingFilterValue?: boolean

    async #onValueChanged() {
        if (this.#isApplyingFilterValue) return

        this.#isApplyingFilterValue = true
        try {
            const value = this.stringInput.value()?.trim()
            if (!value) {
                this.stringInput.setValue("")
            }

            await this.column().applyFilterValue(value)
        }
        finally {
            this.#isApplyingFilterValue = false
        }
    }
}

@Component({
    selector: "app-table-cell-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgComponentOutlet],
    template: `
@if (column() && row()) {
    @if (column().cellComponentType; as cellComponentType)     {
        <ng-template *ngComponentOutlet="cellComponentType; inputs: { table: table(), column: column(), row: row()}"></ng-template>
    }
    @else {
        {{column().getValue(row())}}
    }
}
`
})
export class TableCellRendererComponent {
    readonly table = input.required<TableModel>()
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Directive()
export abstract class TableCellComponent {
    readonly table = input.required<TableModel>()
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Component({
    selector: "app-row-selector-table-cell",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatCheckbox],
    styles: [" :host { display: inline-block; width: 48px; } "],
    template: `
        <mat-checkbox class="example-margin"
            [checked]="row().isSelected()"
            (click)="$event.stopPropagation()"
            (change)="table().setRowIsSelected(row(), $event.checked)" />
<!-- <mat-slide-toggle [ngModel]="row().isSelected()" (ngModelChange)="table().setRowIsSelected(row(), $event)" /> -->
`
})
export class RowSelectorTableCellComponent extends TableCellComponent {
}
