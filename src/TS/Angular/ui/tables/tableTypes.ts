import { DataFilterOperator, DataSortDirection, PropPath, PropPathSelection } from "@lib/data/utils"
import type { TableFilterComponent } from "./tableComponents"
import type { TableFilterDataSource } from "./tableModels"
import { TableFilterType } from "./tablePrimitives"

export interface ITableSortDefinition {
    path: string | string[]
    direction: DataSortDirection
}

export type TableRowSelectionMode = "none" | "single" | "multiple"

export interface ITableFilterDataSource<T> {
    type?: TableFilterType
    value?: PropPath
    text?: PropPath
    load(): Promise<T[]>
}

export interface ITableFilterConfig<TData = any> {
    type?: TableFilterType
    component?: TableFilterComponent
    target?: PropPathSelection<TData>
    /**
     * Default operator: "contains"
     * or "eq" if an "id" was specified via the source definition.
     **/
    operator?: DataFilterOperator
    dataSource?: TableFilterDataSource<TData>
    source?: ITableFilterDataSource<any> | (() => ITableFilterDataSource<any>)
}
