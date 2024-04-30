import { Type, computed, signal } from "@angular/core"
import {
    ActiveDataFilter, ActiveDataSort, DataPath, DataPathSelection,
    DataSortDirection, DataFilterOperator
} from "@lib/data-utils"
import { DataItemModel, ItemModel, ListModel } from "@lib/models"
import { AbstractTableFilterComponent } from "./tableComponents"
import { Subject } from "rxjs"

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
    pagination?: IPaginationConfig
}

export type ClickType = "single" | "double"
type RowClickEvent<TData> = { row: TableRowModel<TData>, clickType: ClickType }
type RowClickEventHandlerFn<TData> = (event: RowClickEvent<TData>) => void

export class TableModel<TData = any> extends ItemModel {
    readonly source: AbstractTableDataSource<TData>

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

    readonly rows = computed(() => {
        return this.source.rows() ?? []
    })

    readonly #selectedRows = new ListModel<TableRowModel<TData>>()
    readonly selectedRows = this.#selectedRows.items

    #activeFilters: ActiveDataFilter[] = []

    readonly pagination: PaginationModel

    constructor(config: ITableModelConfig<TData>) {
        super()

        this.source = config.dataSource

        this.#setColumns(config.columns)

        this.pagination = new PaginationModel(config.pagination)

        // TODO: Dunno yet if pagination should be baked into the data-source,
        // so we currently glue them together explicitely.
        // NOTE that we currently have no way of detaching the pagination from
        // the data-source.
        attachPaginationToDataSource(this.source, this.pagination)
    }

    selectRow(row: TableRowModel<TData>) {
        // TODO: Support multi row selection.
        this.#selectedRows.clear()
        this.#selectedRows.add(row)
    }

    #onRowClicked?: RowClickEventHandlerFn<TData>

    setOnRowClicked(onRowClickedHandlerFn: RowClickEventHandlerFn<TData>): this {
        this.#onRowClicked = onRowClickedHandlerFn

        return this
    }

    onRowClicked(row: TableRowModel<TData>, type: ClickType) {
        const handler = this.#onRowClicked
        if (handler) {
            handler({ row: row, clickType: type })
        }
    }

    // setOnRowClickEvent(row: TableRowModel<TData>): this {

    // }

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

                activeFilter = new ActiveDataFilter(column.id, filter.target ?? column.select, operator)
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
    readonly toolId: string
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
            this.toolId = this.filter.id
        }
        else {
            // Unfortunately we always need a filter-ID for Angular material table's
            // column filter header row :-(
            this.toolId = crypto.randomUUID()
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
    value?: DataPath
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
    operator?: DataFilterOperator
    dataSource?: AbstractTableFilterDataSource<TData>
    source?: ITableFilterDataSource<any> | (() => ITableFilterDataSource<any>)
}

export class TableFilterModel<TData = any> extends ItemModel {
    readonly type?: TableFilterType
    readonly component?: AbstractTableFilterComponent
    readonly target?: DataPath
    readonly operator?: DataFilterOperator
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
    value?: DataPath
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

interface IPaginationConfig {
    size?: number
    availableSizes?: number | number[]
}

export class PaginationModel {
    readonly #busyCounter = signal(0)
    readonly isBusy = computed(() => this.#busyCounter() > 0)

    protected readonly _index = signal(0)
    /** The index of the current page. */
    readonly index = this._index.asReadonly()
    readonly pageNumber = computed(() => this.index() + 1)

    readonly #lastIndex = signal<number | undefined>(undefined)
    readonly lastIndex = this.#lastIndex.asReadonly()
    readonly lastPageNumber = computed(() => this.lastIndex() !== undefined ? this.lastIndex()! + 1 : undefined)

    readonly #changed = new Subject<void>()
    readonly changed = this.#changed.asObservable()

    protected readonly _size = signal(20)
    /** The page size. */
    readonly size = this._size.asReadonly()

    protected readonly _count = signal(0)
    /** The number of currently loaded data-items. */
    readonly count = this._count.asReadonly()

    protected readonly _totalCount = signal<number | undefined>(undefined)
    /** The total number of loadable data-items. */
    readonly totalCount = this._totalCount.asReadonly()

    readonly #availableSizes = signal([10, 20, 50])
    readonly availableSizes = this.#availableSizes.asReadonly()
    readonly #isSizeSelectable = signal(true)
    readonly isSizeSelectable = this.#isSizeSelectable.asReadonly()

    readonly #canMoveToFirst = signal(false)
    readonly canMoveToFirst = this.#canMoveToFirst.asReadonly()

    readonly #canMoveToPrevious = signal(false)
    readonly canMoveToPrevious = this.#canMoveToPrevious.asReadonly()

    readonly #canMoveToNext = signal(false)
    readonly canMoveToNext = this.#canMoveToNext.asReadonly()

    readonly isMoveToLastAvailable = computed(() => this.totalCount() !== undefined)
    readonly #canMoveToLast = signal(false)
    readonly canMoveToLast = this.#canMoveToLast.asReadonly()

    constructor(config?: IPaginationConfig) {
        if (config?.size) {
            this._size.set(config.size)
        }
        if (config?.availableSizes) {
            this.#availableSizes.set(
                typeof config?.availableSizes === "number"
                    ? [config?.availableSizes]
                    : config?.availableSizes
            )
        }

        if (!this.availableSizes().includes(this.size())) {
            const fallbackSize = this.availableSizes()[0] ?? 5
            this._size.set(fallbackSize)
        }

        this.#updateStates()
    }

    _setLoadedCount(loadedCount: number) {
        const firstItemIndexAtCurrentPage = this.index() * this.size()
        this._count.set(firstItemIndexAtCurrentPage + Math.min(this.size(), loadedCount))

        this.#updateStates()
    }

    _setTotalCount(totalCount: number) {
        if (totalCount === this.totalCount()) return

        this._totalCount.set(totalCount)

        this.#lastIndex.set(Math.max(0, Math.ceil(totalCount / this.size()) - 1))

        if (totalCount < this.count()) {
            this.moveToFirst()
        }
        else {
            this.#updateStates()
        }
    }

    setSize(pageSize: number): boolean {
        if (pageSize < 1 || pageSize === this.size()) return false

        this._size.set(pageSize)

        this.#moveToIndexCore(0)
        this.#onChanged()

        return true
    }

    enterBusyState() {
        this.#busyCounter.update(x => x + 1)
    }

    leaveBusyState() {
        this.#busyCounter.update(x => x > 0 ? x - 1 : 0)
    }

    moveToFirst() {
        const changed = this.#moveToIndexCore(0)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToNext(): boolean {
        const changed = this.#moveToIndexCore(this.index() + 1)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToPrevious(): boolean {
        const changed = this.#moveToIndexCore(this.index() - 1)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    /** Move-to-last is only available if the totalCount was set. */
    moveToLast(): boolean {
        const totalCount = this.totalCount()
        const lastIndex = this.lastIndex()
        if (totalCount === undefined || lastIndex === undefined) return false

        const changed = this.moveToIndex(lastIndex)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToIndex(pageIndex: number): boolean {
        const changed = this.#moveToIndexCore(pageIndex)
        if (changed) {
            this.#onChanged()
        }

        return true
    }

    #onChanged() {
        this.#changed.next()
    }

    #moveToIndexCore(pageIndex: number): boolean {
        if (pageIndex < 0 || pageIndex === this.index()) {
            return false
        }

        const lastIndex = this.lastIndex()
        if (lastIndex !== undefined && pageIndex > lastIndex) {
            return false
        }

        // TODO: Restrict upper index?

        this._index.set(Math.max(pageIndex, 0))
        this.#updateStates()

        return true
    }

    #updateStates() {
        const index = this.index()

        const canMoveToFirst = index > 0
        const canMoveToPrevious = index > 0

        const count = this.count()
        const totalCount = this.totalCount()
        const availableCountAtCurrentPage = this.size() * (this.index() + 1)

        const isEndReached = totalCount !== undefined
            // If total count is available then use that.
            ? count >= totalCount
            // Otherwise, if we loaded less data-items than would fill the pages
            // then we reached the end.
            : count < availableCountAtCurrentPage

        const canMoveToNext = !isEndReached
        const canMoveToLast = !isEndReached

        this.#canMoveToFirst.set(canMoveToFirst)
        this.#canMoveToPrevious.set(canMoveToPrevious)
        this.#canMoveToNext.set(canMoveToNext)
        this.#canMoveToLast.set(canMoveToLast && this.isMoveToLastAvailable())
    }
}

function attachPaginationToDataSource(dataSource: AbstractTableDataSource, pagination: PaginationModel) {
    pagination.changed.subscribe(async () => {
        dataSource._applyPaging(pagination.index(), pagination.size())
        dataSource.load()
    })
    dataSource.loaded.subscribe(() => {
        pagination._setLoadedCount(dataSource.rows().length)
    })
    dataSource._applyPaging(pagination.index(), pagination.size())
}

//#endregion Pagination

//#region data-source

export abstract class AbstractTableDataSource<TData = any> {
    protected readonly _rowList = new ListModel<TableRowModel<TData>>()
    readonly rows = this._rowList.items
    protected readonly _loaded = new Subject<void>()
    readonly loaded = this._loaded.asObservable()

    readonly #busyCounter = signal(0)
    readonly isBusy = computed(() => this.#busyCounter() > 0)

    enterBusyState() {
        this.#busyCounter.update(x => x + 1)
    }

    leaveBusyState() {
        this.#busyCounter.update(x => x > 0 ? x - 1 : 0)
    }

    abstract _applyPaging(pageIndex: number, pageSize: number): Promise<void>
    abstract _setFilters(activeFilters: ActiveDataFilter[]): Promise<void>
    abstract _setSortList(activeSortList: ActiveDataSort[]): Promise<void>
    abstract load(): Promise<void>
}

//#endregion
