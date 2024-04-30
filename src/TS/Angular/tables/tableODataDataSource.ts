import { signal } from "@angular/core"
import { ActiveDataFilter, ActiveDataSort, DataPath, DataPathSelection, ODataComparisonOperator, ODataFilterBuilder, ODataQueryBuilder, OrderByDataPath, OrderByDataPathBuilder } from "@lib/data-utils"
import { IDataSourceWebService } from "@lib/data/web"
import {
    AbstractTableDataSource, TableRowModel, AbstractTableFilterDataSource,
    ITableFilterDataSource
} from "./tableModels"

interface ITableODataDataSourceConfig<TData> {
    readonly webService: IDataSourceWebService
    readonly query: (q: ODataQueryBuilder<TData>) => void
    /**
     * The initial fiter which will always be active.
     * Temporary active filters will be added to this initial filter.
     */
    readonly filter?: (f: ODataFilterBuilder<TData>) => void
    /**
     * The initial sort which will be removed when an other sort becomes active.
     */
    readonly orderby?: ((o: OrderByDataPathBuilder<TData>) => OrderByDataPath) | ((o: OrderByDataPathBuilder<TData>) => OrderByDataPath)[]
}

export class TableODataDataSource<TData> extends AbstractTableDataSource<TData> {
    readonly #initialQuery = new ODataQueryBuilder<TData>()
    readonly #initialFilter = new ODataFilterBuilder<TData>()
    #activeFilters?: ActiveDataFilter[]
    #activeSortList?: ActiveDataSort[]
    readonly #readUrl = signal("")
    readonly readUrl = this.#readUrl.asReadonly()
    readonly #webService: IDataSourceWebService

    constructor(config: ITableODataDataSourceConfig<TData>) {
        super()

        this.#webService = config.webService

        config.query(this.#initialQuery)

        if (config.filter) {
            config.filter(this.#initialFilter)
        }

        if (config.orderby) {
            const orderByList = Array.isArray(config.orderby)
                ? config.orderby
                : [config.orderby]

            this.#activeSortList = []

            for (const orderBy of orderByList) {
                const orderbyDataPath = orderBy(new OrderByDataPathBuilder<TData>())
                this.#activeSortList.push(new ActiveDataSort(orderbyDataPath, orderbyDataPath.direction))
            }
        }

        this.#updateQuery()
    }

    async moveToNextPage() {

    }

    #skip?: number
    #top?: number

    override async _applyPaging(pageIndex: number, pageSize: number): Promise<void> {
        this.#skip = pageIndex * pageSize
        this.#top = pageSize

        this.#updateQuery()
    }

    override async _setSortList(activeSortList: ActiveDataSort[]): Promise<void> {
        this.#activeSortList = activeSortList

        this.#updateQuery()
    }

    override async _setFilters(activeFilters: ActiveDataFilter[]): Promise<void> {
        this.#activeFilters = activeFilters

        this.#updateQuery()
    }

    #updateQuery() {
        const q = this.#initialQuery.clone()

        if (this.#skip) {
            q.skip(this.#skip)
        }

        if (this.#top) {
            q.top(this.#top)
        }

        const f = this.#initialFilter.clone()

        if (this.#activeFilters?.length) {
            for (const filter of this.#activeFilters) {
                if (!filter.value) continue

                if (filter.operator === "contains") {
                    if (typeof filter.value === "string") {
                        f.and().contains(filter.target, filter.value)
                    }
                }
                else {
                    f.and().where(
                        filter.target,
                        filter.operator as ODataComparisonOperator,
                        filter.value)
                }
            }
        }

        q.assignFromFilterBuilder(f)

        if (this.#activeSortList?.length) {
            for (const sort of this.#activeSortList) {
                q.orderby(sort.target, sort.direction)
            }
        }

        this.#readUrl.set(q.toString())
    }

    async #load(url: string) {
        const readUrl = this.readUrl()
        if (!readUrl) {
            this._rowList.clear()

            return
        }

        try {
            this.enterBusyState()
            const dataItems = await this.#webService.get<TData[]>(url)

            this._rowList.setItems(dataItems.map(x => new TableRowModel<TData>(x)))
        }
        finally {
            this.leaveBusyState()
            this._loaded.next()
        }
    }

    async load() {
        await this.#load(this.readUrl())
    }
}

export interface ITableFilterODataDataSourceConfig<T> {
    readonly webService: IDataSourceWebService
    readonly query: (q: ODataQueryBuilder<T>) => ODataQueryBuilder<T>
    readonly value?: DataPathSelection<T>
    readonly text?: DataPathSelection<T>
}

export class TableFilterODataDataSource<T> extends AbstractTableFilterDataSource<T>
    implements ITableFilterDataSource<T> {
    readonly #webService: IDataSourceWebService
    readonly query: ODataQueryBuilder<T>
    readonly #isSingleDataPropSource: boolean

    constructor(config: ITableFilterODataDataSourceConfig<T>) {
        super()

        this.#webService = config.webService
        this.query = config.query(new ODataQueryBuilder<T>())
        if (config.value) {
            this.value = DataPath.createFromSelection<T>(config.value)
        }
        if (config.text) {
            this.text = DataPath.createFromSelection<T>(config.text)
        }

        // If we queried only one property and didn't specify the source value/text selectors
        // then we're safe to ditch the complex object and just use its single value property.
        this.#isSingleDataPropSource = !this.value && !this.text && this.query.isSinglePropSelection()
    }

    #queryUrl?: string
    #singleDataProp?: string

    override async read(): Promise<any[]> {
        if (this.#queryUrl === undefined) {
            this.#queryUrl = this.query.toString()
        }

        if (!this.#queryUrl) {
            return []
        }

        const dataItems = await this.#webService.get<T[]>(this.#queryUrl)
        if (this.#isSingleDataPropSource && !this.#singleDataProp && dataItems.length) {
            this.#singleDataProp = Object.keys(dataItems[0]!)[0]!
        }

        const effectiveDataItems = !!this.#singleDataProp && dataItems.length
            ? dataItems.map(x => (x as any)[this.#singleDataProp!])
            : dataItems

        return effectiveDataItems
    }
}

export function createTableFilterODataSource<T>(odataSourceConfig: ITableFilterODataDataSourceConfig<T>): ITableFilterDataSource<T> {
    return new TableFilterODataDataSource<T>(odataSourceConfig)
}
