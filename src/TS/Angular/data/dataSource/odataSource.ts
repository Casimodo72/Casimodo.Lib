import { signal } from "@angular/core"

import {
    ODataComparisonOperator, ODataFilterBuilder, ODataQueryBuilder,
    OrderByPropPath, OrderByPropPathBuilder, ActiveDataFilter, ActiveDataSort,
} from "@lib/data/utils"
import { IDataSourceWebService } from "@lib/data/web"

import { IPaginationConfig, PaginationModel } from "./paginationModel"
import { DataSource } from "./dataSource"

export interface IODataDataSourceConfig<TData> {
    readonly webService: IDataSourceWebService
    readonly pageable?: IPaginationConfig
    readonly query: (q: ODataQueryBuilder<TData>) => void
    /**
     * The initial fiter which will always be active.
     * Temporary active filters will be added to this initial filter.
     */
    readonly filter?: (f: ODataFilterBuilder<TData>) => void
    /**
     * The initial sort which will be removed when an other sort becomes active.
     */
    readonly orderby?: ((o: OrderByPropPathBuilder<TData>) => OrderByPropPath) | ((o: OrderByPropPathBuilder<TData>) => OrderByPropPath)[]
}

export class ODataDataSource<TData> extends DataSource<TData> {
    readonly #initialQuery = new ODataQueryBuilder<TData>()
    readonly #initialFilter = new ODataFilterBuilder<TData>()
    #skip?: number
    #top?: number
    #filters?: { [key: string]: ActiveDataFilter[] | null | undefined }
    #orderByList?: ActiveDataSort[]
    readonly #readUrl = signal("")
    readonly readUrl = this.#readUrl.asReadonly()
    readonly #webService: IDataSourceWebService

    readonly paginator: PaginationModel
    #isUpdatingPagination?: boolean

    constructor(config: IODataDataSourceConfig<TData>) {
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

            this.#orderByList = []

            for (const orderBy of orderByList) {
                const orderbyDataPath = orderBy(new OrderByPropPathBuilder<TData>())
                this.#orderByList.push(new ActiveDataSort(orderbyDataPath, orderbyDataPath.direction))
            }
        }

        this.paginator = new PaginationModel(config.pageable)
        this.#setPaging()

        this.paginator.changed.subscribe(async () => {
            this.#setPaging()

            if (!this.#isUpdatingPagination) {
                this.#updateQuery()
                this.load()
            }
        })

        this.#updateQuery()
    }

    reset() {
        this.#readUrl.set("")
        this._dataItems.set([])
        this.#resetPagingSilently()
        this.#filters = undefined
        this.#orderByList = undefined
        this.#skip = undefined
        this.#updateQuery()
    }

    #setPaging() {
        this.#skip = this.paginator.index() * this.paginator.size()
        this.#top = this.paginator.size()
    }

    #resetPagingSilently() {
        this.#isUpdatingPagination = true
        try {
            this.paginator.moveToFirst()
        }
        finally {
            this.#isUpdatingPagination = false
        }
    }

    override _setSortList(activeSortList: ActiveDataSort[]) {
        this.#orderByList = activeSortList
        this.#resetPagingSilently()
        this.#updateQuery()
    }

    override _setFilters(activeFilters: ActiveDataFilter[] | undefined, slot?: string) {
        slot ??= "#default-slot#"
        if (activeFilters?.length) {
            this.#filters ??= {}
            this.#filters[slot] = activeFilters
        }
        else if (this.#filters) {
            delete this.#filters[slot]
        }
        this.#resetPagingSilently()
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

        const flatFilters: ActiveDataFilter[] = []
        if (this.#filters) {
            for (const key in this.#filters) {
                const slotFilters = (this.#filters as any)[key]

                if (slotFilters?.length) {
                    flatFilters.push(...slotFilters)
                }
                else {
                    delete (this.#filters as any)[key]
                }
            }
        }

        for (const filter of flatFilters) {
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

        q.assignFromFilterBuilder(f)

        if (this.#orderByList?.length) {
            for (const sort of this.#orderByList) {
                q.orderby(sort.target, sort.direction)
            }
        }

        this.#readUrl.set(q.toString())
    }

    async #load(url: string) {
        this._dataItems.set([])

        const readUrl = this.readUrl()
        if (!readUrl) {
            return this._dataItems()
        }

        try {
            this.enterBusyState()
            this._dataItems.set(await this.#webService.get<TData[]>(url))

            return this._dataItems()
        }
        finally {
            this.paginator._setLoadedCount(this._dataItems.length ?? 0)
            this.leaveBusyState()
            this._loaded.next()
        }
    }

    async load(): Promise<TData[]> {
        return await this.#load(this.readUrl())
    }

    // async loadSingleById(dataId: string): Promise<TData | undefined> {

    // }
}
