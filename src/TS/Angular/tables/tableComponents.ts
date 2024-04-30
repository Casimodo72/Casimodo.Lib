import { CommonModule, NgComponentOutlet } from "@angular/common"
import { Component, ChangeDetectionStrategy, AfterViewInit, input, signal, Type, Directive } from "@angular/core"

import { TableColumnModel, TableFilterType } from "./tableModels"
import { FormItemComponent } from "@lib/forms"
import { StandardStringInputComponent, StandardTypeaheadValuePickerComponent } from "@lib/components"
import { DataPath } from "@lib/data-utils"
import { PickerFormProp, StringFormProp } from "@lib/models"

@Directive()
export abstract class AbstractTableFilterComponent extends FormItemComponent {
    readonly column = input.required<TableColumnModel>()
}

@Component({
    selector: "app-table-column-filter-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        //CommonModule,
        NgComponentOutlet
    ],
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

    readonly filterComponentType = signal<Type<AbstractTableFilterComponent> | undefined>(undefined)

    ngAfterViewInit(): void {
        const column = this.column()
        if (!column) return

        if (column.filter?.type !== undefined) {
            this.filterComponentType.set(this.getFilterComponent(column.filter.type))
        }
    }

    // TODO: Move this to a service.
    getFilterComponent(type: TableFilterType): Type<AbstractTableFilterComponent> | undefined {
        if (type === TableFilterType.STRING_PICKER_WITH_TYPEAHEAD) {
            return TableTypeaheadPickerColumnFilterComponent
        }
        else if (type === TableFilterType.STRING) {
            return TableStringColumnFilterComponent
        }

        return undefined
    }
}

@Component({
    selector: "app-table-typeahead-picker-column-filter",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [StandardTypeaheadValuePickerComponent],
    template: `
<app-standard-typeahead-value-picker [model]="valuePicker" />
`
})
export class TableTypeaheadPickerColumnFilterComponent
    extends AbstractTableFilterComponent
    implements AfterViewInit {
    // TODO: Assess deferred loading of filter picker values:
    // https://stackblitz.com/edit/angular-mat-select-deferred-loading?file=app%2Fselect-deferred-example.ts

    readonly valuePicker = new PickerFormProp<any>(this)
        .setHasNullValue(true)
        .setOnValueChanged(value => this.#onValuePicked(value))

    async #onValuePicked(value: any) {
        const column = this.column()
        const filter = column?.filter
        if (!filter) return

        if (value && filter.source?.value) {
            value = filter.source?.value.getValueFrom(value)
        }

        await column.applyFilterValue(value)
    }

    #displayPropPath?: DataPath

    async ngAfterViewInit() {
        const filter = this.column()?.filter
        if (!filter) return

        const source = filter.source
        if (!source) return

        if (source.text) {
            this.#displayPropPath = source.text
            this.valuePicker.setDisplayFn(data => this.#displayPropPath!.getValueFrom(data) ?? "")
            this.valuePicker.setFilterProp(source.text)
        }

        const pickValues = await source.read()

        this.valuePicker.setPickValues(pickValues)
    }
}

@Component({
    selector: "app-table-string-column-filter",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [StandardStringInputComponent],
    template: `
<app-standard-string-input [model]="stringModel" />
`
})
export class TableStringColumnFilterComponent extends AbstractTableFilterComponent {
    readonly stringModel = new StringFormProp(this)
        .setOnKeyUp((ev) => {
            if (ev.key === "Enter") {
                this.#onValueChanged()
            }
        })
        .setOnFullValueChanged(_ => this.#onValueChanged())

    async #onValueChanged() {
        await this.column().applyFilterValue(this.stringModel.value())
    }
}

@Directive()
export abstract class AbstractTableCellComponent {
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Component({
    selector: "app-table-cell-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        NgComponentOutlet
    ],
    template: `
@if (column() && row()) {
    @if (column().cellType; as cellComponentType)     {
        <ng-template *ngComponentOutlet="cellComponentType; inputs: { column: column(), row: row()}"></ng-template>
    }
    @else {
        {{column().getValue(row())}}
    }
}
`
})
export class TableCellRendererComponent {
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Component({
    selector: "app-custom-table-cell-example",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule
    ],
    template: `
  `
})
export class CustomTableCellExampleComponent extends AbstractTableCellComponent {

}
