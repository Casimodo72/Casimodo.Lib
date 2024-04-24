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
    readonly filter?: (f: TableODataFilterBuilder<TData>) => void
    readonly orderby?: ((o: OrderByDataPathBuilder<TData>) => OrderByDataPath) | ((o: OrderByDataPathBuilder<TData>) => OrderByDataPath)[]
}

export class TableODataDataSource<TData> extends AbstractTableDataSource<TData> {
    readonly #initialQuery = new ODataQueryBuilder<TData>()
    readonly #initialFilter = new TableODataFilterBuilder<TData>()
    // #currentQuery: ODataQueryBuilder<TData>
    // #currentFilter: TableODataFilterBuilder<TData>
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
                this.#activeSortList.push(new ActiveDataSort(crypto.randomUUID(), orderbyDataPath, orderbyDataPath.direction))
            }
        }

        // this.#currentQuery = this.#initialQuery.clone()
        // this.#currentFilter = this.#initialFilter.clone()
        // this.#currentQuery._setFilterOfBuilder(this.#currentFilter)

        // this.#updateCurrentQuery(this.#initialQuery, this.#initialFilter)

        this.#updateQuery()
    }

    override async _setActiveSortList(activeSortList: ActiveDataSort[]): Promise<void> {
        this.#activeSortList = activeSortList

        this.#updateQuery()
    }

    override async _setActiveFilterList(activeFilters: ActiveDataFilter[]): Promise<void> {
        this.#activeFilters = activeFilters

        this.#updateQuery()
    }

    #updateQuery() {
        const q = this.#initialQuery.clone()
        const f = this.#initialFilter.clone()

        if (this.#activeFilters?.length) {
            for (const filter of this.#activeFilters) {
                if (!filter.value) continue

                if (filter.operator === "contains") {
                    if (typeof filter.value === "string") {
                        f.and().containsWithDataPath(filter.target, filter.value)
                    }
                }
                else {
                    f.and().whereWithDataPath(
                        filter.target,
                        filter.operator as ODataComparisonOperator,
                        filter.value)
                }
            }
        }

        q._setFilterOfBuilder(f)

        if (this.#activeSortList?.length) {
            for (const sort of this.#activeSortList) {
                q.orderby(sort.target, sort.direction)
            }
        }

        this.#readUrl.set(q.toString())
    }

    // #updateCurrentQuery(q: ODataQueryBuilder<TData>, f: TableODataFilterBuilder<TData>) {
    //     this.#currentQuery = q.clone()
    //     this.#currentFilter = f.clone()
    //     this.#currentQuery._setFilterOfBuilder(this.#currentFilter)

    //     this.#readUrl.set(this.#currentQuery.toString())
    // }

    async #load(url: string) {
        const readUrl = this.readUrl()
        if (!readUrl) {
            this._rowList.clear()

            return
        }

        const dataItems = await this.#webService.get<TData[]>(url)

        this._rowList.setItems(dataItems.map(x => new TableRowModel<TData>(x)))
    }

    async load() {
        await this.#load(this.readUrl())
    }
}

class TableODataFilterBuilder<T = any> extends ODataFilterBuilder<T> {
    override clone(): TableODataFilterBuilder<T> {
        const clone = new TableODataFilterBuilder<T>()
        this._copyTo(clone)

        return clone
    }

    whereWithDataPath(propPath: DataPath, comparisonOp: ODataComparisonOperator, value: any): this {
        // TODO: How to convert the value to an OData value here?
        return this.where(this.#toODataPath(propPath) as any, comparisonOp, value)
    }

    containsWithDataPath(propPath: DataPath, value: string): this {
        return this._append(` contains(${this.#toODataPath(propPath)},'${value}')`)
    }

    #toODataPath(propPath: DataPath): string {
        return propPath.segments.reduce((acc, next) => acc + "/" + next)
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
            this.id = DataPath.createFromSelection<T>(config.value)
        }
        if (config.text) {
            this.text = DataPath.createFromSelection<T>(config.text)
        }

        // If we queried only one property and didn't specify the source value/text selectors
        // then we're safe to ditch the complex object and just use its single value property.
        this.#isSingleDataPropSource = !this.id && !this.text && this.query.isSinglePropSelection()
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

// export class TableFilterODataDataSourceBuilder<T> {
//     from<T>(odataSourceConfig: ITableFilterODataDataSourceConfig<T>): ITableFilterDataSource<T> {

//         return new TableFilterODataDataSource<T>(odataSourceConfig)
//     }
// }
