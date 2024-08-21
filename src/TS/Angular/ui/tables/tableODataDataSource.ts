import { PropPath, PropPathSelection, ODataQueryBuilder, } from "@lib/data/utils"
import { IDataSourceWebService } from "@lib/data/web"

import { TableFilterDataSource } from "./tableModels"
import { ITableFilterDataSource } from "./tableTypes"

export interface ITableFilterODataDataSourceConfig<T> {
    readonly dataService: IDataSourceWebService
    readonly query: (q: ODataQueryBuilder<T>) => ODataQueryBuilder<T>
    readonly value?: PropPathSelection<T>
    readonly text?: PropPathSelection<T>
}

// TODO: IMPL a generalized ODataDataSource. We need such a data-source for e.g. cached
// entity data (e.g. for country-states).

export class TableFilterODataDataSource<T> extends TableFilterDataSource<T>
    implements ITableFilterDataSource<T> {
    readonly #dataService: IDataSourceWebService
    readonly query: ODataQueryBuilder<T>
    readonly #isSinglePropSource: boolean

    constructor(config: ITableFilterODataDataSourceConfig<T>) {
        super()

        this.#dataService = config.dataService
        this.query = config.query(new ODataQueryBuilder<T>())
        if (config.value) {
            this.value = PropPath.fromSelection<T>(config.value)
        }
        if (config.text) {
            this.text = PropPath.fromSelection<T>(config.text)
        }

        // If we queried only one property and didn't specify the source value/text selectors
        // then we're safe to ditch the complex object and just use its single property value.
        this.#isSinglePropSource = !this.value && !this.text && this.query.isSinglePropSelection()
    }

    #queryUrl?: string
    #singleProp?: string

    override async load(): Promise<any[]> {
        if (this.#queryUrl === undefined) {
            this.#queryUrl = this.query.toString()
        }

        if (!this.#queryUrl) {
            return []
        }

        const dataItems = await this.#dataService.get<T[]>(this.#queryUrl)
        if (this.#isSinglePropSource && !this.#singleProp && dataItems.length) {
            this.#singleProp = Object.keys(dataItems[0]!)[0]!
        }

        const effectiveDataItems = !!this.#singleProp && dataItems.length
            ? dataItems.map(x => (x as any)[this.#singleProp!])
            : dataItems

        return effectiveDataItems
    }
}

export function createTableFilterODataSource<T>(odataSourceConfig: ITableFilterODataDataSourceConfig<T>): ITableFilterDataSource<T> {
    return new TableFilterODataDataSource<T>(odataSourceConfig)
}
