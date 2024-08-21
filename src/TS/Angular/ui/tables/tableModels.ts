import { Signal, Type, WritableSignal, computed, signal } from "@angular/core"

import {
    ActiveDataFilter, ActiveDataSort, PropPath, PropPathSelection,
    DataSource, DataSortDirection, DataFilterOperator, PaginationModel
} from "@lib/data"
import { DataItemModel, ItemModel, ListModel } from "@lib/models"

import {
    TableCellComponent, TableFilterComponent, RowSelectorTableCellComponent
} from "./tableComponents"
import type {
    ITableFilterConfig, ITableFilterDataSource, TableRowSelectionMode
} from "./tableTypes"
import { TableColumnDataType, TableColumnRole, TableFilterType } from "./tablePrimitives"

export class ActiveTableDataFilter extends ActiveDataFilter {
    readonly id: string

    constructor(id: string, targetProp: PropPath, operator: DataFilterOperator, value?: any) {
        super(targetProp, operator, value)

        this.id = id
    }
}

function isRowSelectorColumn(column: TableColumnModel): boolean {
    return column.role === TableColumnRole.RowSelector
}

interface ITableModelConfig<TData> {
    source: DataSource<TData>
    columns: TableColumnModel<TData>[]
    selectable?: TableRowSelectionMode
}

export type ClickType = "single" | "double"
interface IRowClickEvent<TData> {
    readonly row: TableRowModel<TData>
    readonly clickEvent: MouseEvent
    readonly clickType: ClickType
}
type RowClickEventHandlerFn<TData> = (event: IRowClickEvent<TData>) => void

export class TableModel<TData = any> extends ItemModel {
    readonly source: DataSource<TData>

    readonly #columnList = new ListModel<TableColumnModel<TData>>()
    readonly columns = this.#columnList.items
    // TODO: I think this won't work as a computed signal.
    readonly visibleColumnIds = computed(() => {
        return this.columns().filter(x => x.isVisible()).map(x => x.id)
    })

    /**
     * Unfortunately this is needed for Angular material table's column tools (filter for now) header row :-(
     * This returns tool-IDs for all visible columns - even if a column does not have a tool (e.g. a filter).
    */
    readonly visibleColumnToolIds = computed(() => {
        return this.columns().filter(x => x.isVisible()).map(x => x.toolId)
    })

    #onRowClickedFn?: RowClickEventHandlerFn<TData>

    readonly rows = computed(() => {
        return this.source.dataItems().map(x => new TableRowModel<TData>(x))
    })

    #isSelectable = false

    get selectionMode() {
        return this.#selectionMode
    }
    #selectionMode: TableRowSelectionMode = "none"

    readonly #selectedRows = new ListModel<TableRowModel<TData>>()
    readonly selectedRows = this.#selectedRows.items

    #activeFilters: ActiveTableDataFilter[] = []

    readonly paginator: PaginationModel

    constructor(config: ITableModelConfig<TData>) {
        super()

        this.source = config.source
        this.paginator = this.source.paginator

        const columns = this.#isSelectable
            ? [this.#getOrCreateSelectionColumn(), ...config.columns]
            : config.columns

        this.#setColumns(columns)

        if (config.selectable) {
            this.setSelectionMode(config.selectable)
        }
    }

    reset() {
        this.#clearRowSelection()
        this.source.reset()
    }

    #clearRowSelection() {
        for (const row of this.selectedRows()) {
            row.setIsSelected(false)
        }
        this.#selectedRows.clear()
    }

    toggleRowIsSelected(row: TableRowModel<TData>) {
        this.setRowIsSelected(row, !row.isSelected())
    }

    setRowIsSelected(row: TableRowModel<TData>, isSelected: boolean) {
        if (!this.#isSelectable) return

        if (isSelected && !row.isSelected()) {
            this.selectRow(row)
        }
        else if (!isSelected && row.isSelected()) {
            this.deselectRow(row)
        }
    }

    selectRow(row: TableRowModel<TData>): boolean {
        if (this.#selectionMode === "none") return false

        if (this.#selectionMode !== "multiple") {
            this.#clearRowSelection()
        }

        if (row.setIsSelected(true)) {
            this.#selectedRows.add(row)
            return true
        }
        else {
            return false
        }
    }

    deselectRow(row: TableRowModel<TData>) {
        if (this.#selectionMode !== "multiple") {
            this.#clearRowSelection()
        }

        row.setIsSelected(false)
    }

    setOnRowClicked(onRowClickedFn: RowClickEventHandlerFn<TData>): this {
        this.#onRowClickedFn = onRowClickedFn

        return this
    }

    onRowClicked(row: TableRowModel<TData>, clickEvent: MouseEvent, type?: ClickType) {
        this.#onRowClickedFn?.({
            row: row,
            clickEvent: clickEvent,
            clickType: type ?? "single"
        })
    }

    #setColumns(columns: TableColumnModel<TData>[]): this {
        for (const column of columns) {
            column._table = this
        }

        this.#columnList.addRange(columns)

        return this
    }

    #selectionColumn?: TableColumnModel

    #getOrCreateSelectionColumn() {
        if (!this.#selectionColumn) {
            this.#selectionColumn = new TableColumnModel({
                role: TableColumnRole.RowSelector,
                isVisible: true,
                select: "",
                cellComponent: RowSelectorTableCellComponent
            })
            this.#selectionColumn._table = this
        }

        return this.#selectionColumn
    }

    async setSelectionMode(selectable: TableRowSelectionMode) {
        if (this.#selectionMode === selectable) return

        if (selectable !== "none") {
            this.#isSelectable = true
            this.#selectionMode = selectable

            if (!this.#columnList.items().find(x => isRowSelectorColumn(x))) {
                this.#columnList.insertFirst(this.#getOrCreateSelectionColumn() as any)
            }
        }
        else {
            const selectionColumn = this.#columnList.items().find(x => isRowSelectorColumn(x))
            if (selectionColumn) {
                this.#columnList.remove(selectionColumn)
            }

            this.#clearRowSelection()

            this.#isSelectable = false
            this.#selectionMode = "none"
        }
    }

    async toggleColumnSortState(column: TableColumnModel<TData>) {
        const sortState = column.sortState()

        // NOTE: Currently only one active sort column is supported.

        for (const column of this.columns()) {
            if (column.sortState()) {
                column.sortState.set(undefined)
            }
        }

        const sortDirection = !sortState || sortState === "desc"
            ? "asc"
            : "desc"

        column.sortState.set(sortDirection)

        const activeSortList = [new ActiveDataSort(column.select, sortDirection)]

        await this.source._setSortList(activeSortList)
        await this.source.load()
    }

    async applyColumnFilterValue(column: TableColumnModel<TData>, filterValue: any) {
        const filter = column.filter
        if (!filter) return

        const activeFilterIndex = this.#activeFilters.findIndex(x => x.id === column.id)

        let activeFilter = activeFilterIndex !== -1
            ? this.#activeFilters[activeFilterIndex]
            : undefined

        if (filterValue) {
            if (!activeFilter) {
                let operator: DataFilterOperator | undefined = filter.operator
                // When filtering by complex objects: filter by ID of the complex object
                // if not specified explicitly.
                if (!operator && filter.source?.value) {
                    operator = "eq"
                }
                if (!operator) {
                    operator = "contains"
                }

                activeFilter = new ActiveTableDataFilter(column.id, filter.target ?? column.select, operator)
                this.#activeFilters.push(activeFilter)
            }

            // TODO: Compare filter value in order to avoid filtering
            // when the value did not really change.
            activeFilter.value = filterValue
        }
        else if (activeFilterIndex !== -1) {
            // Remove filter if there's no filter value.
            this.#activeFilters.splice(activeFilterIndex, 1)
        }

        await this.source._setFilters(this.#activeFilters)

        await this.source.load()
    }
}

//#region Rows

export class TableRowModel<TData = any> extends DataItemModel<Partial<TData>> {

}

//#endregion Rows

//#region Columns

export interface ITableColumnConfig<TData> {
    select: PropPathSelection<TData>
    title?: string
    type?: TableColumnDataType
    isSortable?: boolean
    filter?: ITableFilterConfig<TData>
    cellComponent?: Type<TableCellComponent>
    isVisible?: boolean
    role?: TableColumnRole
}

export class TableColumnModel<TData = any> extends ItemModel {
    _table?: TableModel<TData>
    readonly title = signal("")
    readonly type?: TableColumnDataType
    readonly role?: TableColumnRole
    readonly select: PropPath
    readonly isSortable: boolean
    readonly sortState = signal<DataSortDirection | undefined>(undefined)
    readonly filter?: TableFilterModel<TData>
    readonly toolId: string
    readonly cellComponentType?: Type<TableCellComponent>
    readonly #isVisible: WritableSignal<boolean>
    readonly isVisible: Signal<boolean>
    readonly isFiltered = signal(false)

    constructor(config: ITableColumnConfig<TData>) {
        super()

        this.role = config.role

        if (config.isVisible === undefined || !!config.isVisible) {
            this.#isVisible = signal(true)
        }
        else {
            this.#isVisible = signal(false)
        }
        this.isVisible = this.#isVisible.asReadonly()

        this.select = PropPath.fromSelection(config.select)

        if (config.title) {
            this.title.set(config.title)
        }

        if (config.type) {
            this.type = config.type
        }

        this.cellComponentType = config.cellComponent

        this.isSortable = config.isSortable ?? false

        if (config.filter) {
            this.filter = new TableFilterModel<TData>(config.filter)
            this.toolId = this.filter.id
        }
        else {
            // Unfortunately we always need a filter-ID for Angular material table's
            // column filter header row :-(
            this.toolId = crypto.randomUUID()
        }
    }

    toggleSortDirection() {
        if (!this.isSortable) return

        this._table?.toggleColumnSortState(this)
    }

    getRow(row: any): TableRowModel<TData> {
        return row as TableRowModel<TData>
    }

    getValue(row: any): any | null | undefined {
        return this.select.getValue((row as TableRowModel<TData>).data)
    }

    async applyFilterValue(value: any) {
        const table = this._table
        if (!table) return

        await table.applyColumnFilterValue(this, value)
    }
}

export class TableFilterModel<TData = any> extends ItemModel {
    readonly type?: TableFilterType
    readonly component?: TableFilterComponent
    readonly target?: PropPath
    readonly operator?: DataFilterOperator
    readonly source?: ITableFilterDataSource<any>

    constructor(config: ITableFilterConfig<TData>) {
        super()

        this.type = config.type
        this.component = config.component
        if (config.target) {
            this.target = PropPath.fromSelection(config.target)
        }
        this.operator = config.operator
        this.source = typeof config.source === "function"
            ? config.source?.()
            : config.source
    }
}

export abstract class TableFilterDataSource<T> implements ITableFilterDataSource<T> {
    value?: PropPath
    text?: PropPath

    abstract load(): Promise<any[]>
}

//#endregion Columns
