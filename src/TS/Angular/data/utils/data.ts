import { PropPath, PropPathBuilderBase, PropPathSelection } from "./propPath"
import { StringKeys } from "./utils"

export type DataSortDirection = "asc" | "desc"

export class OrderByPropPath extends PropPath {
    readonly direction: DataSortDirection

    constructor(nameOrSegments: string | string[], direction?: DataSortDirection) {
        super(nameOrSegments)

        this.direction = direction ?? "asc"
    }
}

export class OrderByPropPathBuilder<T = any> extends PropPathBuilderBase {
    select(name: StringKeys<T>, sortDirection?: DataSortDirection): OrderByPropPath {
        this._select(name)

        return new OrderByPropPath(this.segments, sortDirection)
    }

    expand<TExpandType>(name: StringKeys<T>): OrderByPropPathBuilder<TExpandType> {
        this._expand(name)

        return this as any as OrderByPropPathBuilder<TExpandType>
    }
}

export type DataComparisonOperator = "eq" | "ne" | "gt" | "ge" | "lt" | "le"
export type DataFilterOperator = DataComparisonOperator | "contains"

export class ActiveDataSort {
    // TODO: RENAME to prop?
    readonly target: PropPath
    direction: DataSortDirection

    constructor(target: PropPath, sortDirection: DataSortDirection) {
        this.target = target
        this.direction = sortDirection
    }
}

export class ActiveDataFilter {
    static createFor<T>(selection: PropPathSelection<T>, operator: DataComparisonOperator, value: any | null): ActiveDataFilter {
        return new ActiveDataFilter(
            PropPath.fromSelection<T>(selection),
            operator,
            value)
    }

    // TODO: RENAME to prop?
    readonly target: PropPath
    readonly operator: DataFilterOperator
    value?: any

    constructor(target: PropPath, operator: DataFilterOperator, value?: any) {
        this.target = target
        this.operator = operator
        this.value = value
    }
}
