import { Type, computed, signal } from "@angular/core"
import {
    ActiveDataFilter, ActiveDataSort, DataPath, DataPathSelection,
    DataSortDirection, TableFilterOperator
} from "@lib/data-utils"
import { DataItemModel, ItemModel, ListModel } from "@lib/models"
import { AbstractTableFilterComponent } from "./tableComponents"

export type StringKeys<T> = Extract<keyof T, string>

export enum TableColumnDataType {
    STRING,
    BOOLEAN,
    DATE_TIME
}

export enum TableFilterType {
    STRING,
    STRING_PICKER_WITH_TYPEAHEAD,
    BOOLEAN,
    DATE_TIME
}

export interface ITableSortDefinition {
    path: string | string[]
    direction: DataSortDirection
}

interface ITableModelConfig<TData> {
    dataSource: AbstractTableDataSource<TData>
    columns: TableColumnModel<TData>[]
}

export class TableModel<TData> extends ItemModel {
    readonly dataSource: AbstractTableDataSource<TData>

    // readonly sortPropName = signal<string>("")
    // readonly sortDirection = signal<"asc" | "desc">("asc")

    readonly #columnList = new ListModel<TableColumnModel<TData>>()
    readonly columns = this.#columnList.items
    readonly visibleColumnIds = computed(() => {
        return this.columns().filter(x => x.isVisible()).map(x => x.id)
    })

    /**
     * Unfortunately this is needed for Angular material table's column filter header row :-(
     * This returns filter-IDs for all visible columns - even if a column dos not have a filter.
    */
    readonly visibleColumnFilterIds = computed(() => {
        return this.columns().filter(x => x.isVisible()).map(x => x.filterId)
    })

    readonly rows = computed(() => {
        return this.dataSource.rows() ?? []
    })

    #activeFilterList: ActiveDataFilter[] = []

    readonly pagination = new TablePaginationManager()

    constructor(config: ITableModelConfig<TData>) {
        super()

        this.dataSource = config.dataSource

        this.#setColumns(config.columns)
    }

    #setColumns(columns: TableColumnModel<TData>[]): this {
        for (const column of columns) {
            column._table = this
        }

        this.#columnList.addRange(columns)

        return this
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

        const activeSortList = [new ActiveDataSort(column.id, column.select, sortDirection)]

        await this.dataSource._setActiveSortList(activeSortList)
        await this.dataSource.load()
    }

    async applyColumnFilterValue(column: TableColumnModel<TData>, filterValue: any) {
        const filter = column.filter
        if (!filter) return

        const activeFilterIndex = this.#activeFilterList.findIndex(x => x.id === column.id)

        let activeFilter = activeFilterIndex !== -1
            ? this.#activeFilterList[activeFilterIndex]
            : undefined

        if (filterValue) {
            if (!activeFilter) {
                let operator: TableFilterOperator | undefined = filter.operator
                // When filtering by complex objects: filter by ID of the complex object
                // if not specified explicitly.
                if (!operator && filter.source?.id) {
                    operator = "eq"
                }
                if (!operator) {
                    operator = "contains"
                }

                activeFilter = new ActiveDataFilter(column.id, filter.target ?? column.select, operator)
                this.#activeFilterList.push(activeFilter)
            }

            // TODO: Compare filter value in order to avoid filtering
            // when the value did not really change.
            activeFilter.value = filterValue
        }
        else if (activeFilterIndex !== -1) {
            this.#activeFilterList.splice(activeFilterIndex, 1)
        }

        await this.dataSource._setActiveFilterList(this.#activeFilterList)

        await this.dataSource.load()
    }
}

//#region Rows

export class TableRowModel<TData> extends DataItemModel<Partial<TData>> {

}

//#endregion Rows

//#region Columns

export interface ITableColumnConfig<TData> {
    select: DataPathSelection<TData>
    title?: string
    type?: TableColumnDataType
    isSortable?: boolean
    filter?: ITableFilterConfig<TData>
    cellComponent?: Type<any>
}

export class TableColumnModel<TData = any> extends ItemModel {
    _table?: TableModel<TData>
    readonly title = signal("")
    readonly type?: TableColumnDataType
    readonly select: DataPath
    readonly isSortable: boolean
    readonly sortState = signal<DataSortDirection | undefined>(undefined)
    readonly filter?: TableFilterModel<TData>
    readonly filterId: string
    readonly cellType?: Type<any>
    readonly isVisible = signal(true)
    readonly isFiltered = signal(false)

    constructor(config: ITableColumnConfig<TData>) {
        super()

        this.select = DataPath.createFromSelection(config.select)

        if (config.title) {
            this.title.set(config.title)
        }

        if (config.type) {
            this.type = config.type
        }

        this.cellType = config.cellComponent

        this.isSortable = config.isSortable ?? false

        if (config.filter) {
            this.filter = new TableFilterModel<TData>(config.filter)
            this.filterId = this.filter.id
        }
        else {
            // Unfortunately we always need a filter-ID for Angular material table's
            // column filter header row :-(
            this.filterId = crypto.randomUUID()
        }
    }

    toggleSearchDirection() {
        if (!this.isSortable) return

        this._table?.toggleColumnSortState(this)
    }

    getRow(row: any): TableRowModel<TData> {
        return row as TableRowModel<TData>
    }

    getValue(row: any): any | null | undefined {
        return this.select.getValueFrom((row as TableRowModel<TData>).data)
    }

    async applyFilterValue(value: any) {
        const table = this._table
        if (!table) return

        await table.applyColumnFilterValue(this, value)
    }
}

export interface ITableFilterDataSource<T> {
    type?: TableFilterType
    id?: DataPath
    text?: DataPath
    read(): Promise<T[]>
}

export interface ITableFilterConfig<TData = any> {
    type?: TableFilterType
    component?: AbstractTableFilterComponent
    target?: DataPathSelection<TData>
    /**
     * Default operator: "contains"
     * or "eq" if an "id" was specified via the source definition.
     **/
    operator?: TableFilterOperator
    dataSource?: AbstractTableFilterDataSource<TData>
    source?: ITableFilterDataSource<any> | (() => ITableFilterDataSource<any>)
}

export class TableFilterModel<TData = any> extends ItemModel {
    readonly type?: TableFilterType
    readonly component?: AbstractTableFilterComponent
    readonly target?: DataPath
    readonly operator?: TableFilterOperator
    readonly source?: ITableFilterDataSource<any>

    constructor(config: ITableFilterConfig<TData>) {
        super()

        this.type = config.type
        this.component = config.component
        if (config.target) {
            this.target = DataPath.createFromSelection(config.target)
        }
        this.operator = config.operator
        this.source = typeof config.source === "function"
            ? config.source?.()
            : config.source
    }
}

export abstract class AbstractTableFilterDataSource<T> implements ITableFilterDataSource<T> {
    id?: DataPath
    text?: DataPath

    abstract read(): Promise<any[]>
}

//#endregion Columns

//#region Pagination

// TODO: Check out https://stackoverflow.com/questions/40629096/odata-paging-with-skip-and-top-how-to-know-that-there-is-no-more-data
export class PaginationRequest {
    readonly pageIndex: number
    readonly pageSize: number

    constructor(pageIndex: number, pageSize: number) {
        this.pageIndex = pageIndex
        this.pageSize = pageSize
    }
}

export class TablePaginationManager {
    readonly _pageIndex = signal(0)
    readonly pageIndex = this._pageIndex.asReadonly()

    readonly _pageSize = signal(20)
    readonly pageSize = this._pageIndex.asReadonly()

    readonly availablePageSizes = signal([20, 50])

    setPageSize(pageSize: number): this {
        this._pageSize.set(Math.max(pageSize, 1))

        return this
    }
}

//#endregion Pagination

//#region data-source

export abstract class AbstractTableDataSource<TData> {
    protected readonly _rowList = new ListModel<TableRowModel<TData>>()
    readonly rows = this._rowList.items

    abstract _setActiveFilterList(activeFilters: ActiveDataFilter[]): Promise<void>
    abstract _setActiveSortList(activeSortList: ActiveDataSort[]): Promise<void>
    abstract load(): Promise<void>
}

//#endregion
