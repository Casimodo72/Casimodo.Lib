import { DateTime } from "luxon"

export type DataSortDirection = "asc" | "desc"

export type DataPathSelection<T> = StringKeys<T> | ((b: DataPathBuilder<T>) => DataPath)

export class DataPath {
    static createFromSelection<T>(selection: DataPathSelection<T>): DataPath {
        if (typeof selection === "string") {
            return new DataPath([selection])
        }
        else if (typeof selection === "function") {
            return selection(new DataPathBuilder<T>())
        }

        return new DataPath([])
    }

    readonly segments: string[]
    readonly path: string

    constructor(nameOrSegments: string | string[]) {
        if (typeof nameOrSegments === "string") {
            nameOrSegments = [nameOrSegments]
        }
        this.segments = nameOrSegments
        this.path = nameOrSegments.reduce((acc, next) => acc + "." + next)
    }

    getValueFrom(data: any): any | null | undefined {
        if (data == null || !this.segments.length) return undefined

        for (let i = 0; i < this.segments.length; i++) {
            if (typeof data !== "object") {
                return undefined
            }

            data = data[this.segments[i]]
        }

        return data
    }
}

abstract class AbstractDataPathBuilder {
    protected _depth = 0
    protected segments: string[] = []

    protected _select(name: string): void {
        if (this._depth >= this.segments.length) {
            this.segments.push(name)
        }
        else {
            this.segments[this._depth] = name
        }
    }

    protected _expand(prop: string) {
        this.segments.push(prop)
        this._depth++
    }
}

export class DataPathBuilder<T = any> extends AbstractDataPathBuilder {
    select(name: StringKeys<T>): DataPath {
        this._select(name)

        return new DataPath(this.segments)
    }

    expand<TExpandType>(name: StringKeys<T>): DataPathBuilder<TExpandType> {
        this._expand(name)

        return this as any as DataPathBuilder<TExpandType>
    }
}

export class OrderByDataPath extends DataPath {
    readonly direction: DataSortDirection

    constructor(nameOrSegments: string | string[], direction?: DataSortDirection) {
        super(nameOrSegments)

        this.direction = direction ?? "asc"
    }
}

export class OrderByDataPathBuilder<T = any> extends AbstractDataPathBuilder {
    select(name: StringKeys<T>, sortDirection?: DataSortDirection): OrderByDataPath {
        this._select(name)

        return new OrderByDataPath(this.segments, sortDirection)
    }

    expand<TExpandType>(name: StringKeys<T>): OrderByDataPathBuilder<TExpandType> {
        this._expand(name)

        return this as any as OrderByDataPathBuilder<TExpandType>
    }
}

// TODO: REMOVE: export type OrderByDataPathSelection<T> = StringKeys<T> | ((b: DataPathBuilder<T>) => string[])

export type DataFilterOperator = ODataComparisonOperator | "contains"

export class ActiveDataSort {
    readonly target: DataPath
    direction: DataSortDirection

    constructor(target: DataPath, sortDirection: DataSortDirection) {
        this.target = target
        this.direction = sortDirection
    }
}

export class ActiveDataFilter {
    readonly id: string
    readonly target: DataPath
    readonly operator: DataFilterOperator
    value?: any

    constructor(id: string, target: DataPath, operator: DataFilterOperator) {
        this.id = id
        this.target = target
        this.operator = operator
    }
}

export function toODataDateTime(value: Date | DateTime | null, encode = false): string | null {
    return toODataDateTimeCore(value, encode, false)
}

export function toODataDateOnly(value: Date | DateTime | null, encode = false): string | null {
    return toODataDateTimeCore(value, encode, true)
}

function toODataDateTimeCore(value: Date | DateTime | null, encode: boolean, isDateOnly: boolean): string | null {
    if (value === null) return null

    let dateTime: DateTime

    if (DateTime.isDateTime(value)) {
        dateTime = value as any
    }
    else if (value instanceof Date) {
        dateTime = DateTime.fromJSDate(value as Date)
    }
    else {
        throw new Error("Failed to convert date-time to ISO 8601 string. " +
            "Invalid argument: The date-time value must be of type Date or luxon.DateTime.")
    }

    // See https://en.wikipedia.org/wiki/ISO_8601
    const iso8601DateTime = isDateOnly
        // Format: ISO 8601: yyyy-MM-dd
        ? dateTime.toISODate()
        // Format: ISO 8601: yyyy-MM-ddTHH:mm:ss.fffffffZ
        : dateTime.toISO()

    if (!iso8601DateTime) {
        throw new Error("Failed to convert a date-time to ISO 8601. The date-time value is invalid.")
    }

    return encode
        ? encodeURIComponent(iso8601DateTime)
        : iso8601DateTime
}

class Container {
    params = ""
    select = ""
    filter = ""
    orderby = ""
    skip: number | null = null
    top: number | null = null
    expansions: ExpandContainer[] = []
    apply = ""

    protected _copyTo(target: any) {
        target.params = this.params
        target.select = this.select
        target.filter = this.filter
        target.orderby = this.orderby
        target.skip = this.skip
        target.top = this.top
        target.expansions = this.expansions.map(x => x.clone())
    }

    clone(): Container {
        const clone = new Container()
        this._copyTo(clone)

        return clone
    }
}

class ExpandContainer extends Container {
    expandedProp = ""

    protected override _copyTo(target: any) {
        super._copyTo(target)
        target.expandedProp = this.expandedProp
    }

    override clone(): ExpandContainer {
        const clone = new ExpandContainer()
        this._copyTo(clone)

        return clone
    }
}

export class SimpleODataQueryBuilderOptions {
    usePascalCase = true
    url?: string
    _root?: Container
}

type SlotType = "params" | "select" | "orderby" | "filter"

export type ODataComparisonOperator = "eq" | "ne" | "gt" | "ge" | "lt" | "le"

export type ODataLogicalOperator = "and" | "or" | "not";

type StringKeys<T> = Extract<keyof T, string>

type ODataOrderByDirection = DataSortDirection

type ODataValueType = string | number | boolean | Date | DateTime | null | GuidValue | TextValue | DateOnlyValue

class GuidValue {
    constructor(public readonly guid: string) { }
}

export function toGuid(guid: string): ODataValueType {
    return new GuidValue(guid)
}

class TextValue {
    constructor(public readonly text: string) { }
}

export function toQueryText(text: string): TextValue {
    return new TextValue(text)
}

class DateOnlyValue {
    constructor(public readonly date: DateTime) { }
}

export function toDateOnly(date: DateTime): DateOnlyValue {
    return new DateOnlyValue(date)
}

// TODO: Add Date and DateTime value wrappers.

export class ODataCoreQueryBuilder<T> {
    protected readonly _root: Container
    #expandContainer?: ExpandContainer

    constructor(root: Container) {
        this._root = root
    }

    protected _copyTo(target: any) {
        target._root = this._root.clone()
    }

    get _effectiveContainer(): Container {
        return this.#expandContainer ? this.#expandContainer : this._root
    }

    select(selection: StringKeys<T>[] | string | ((builder: ODataCoreQueryBuilder<T>) => void)): this {
        if (typeof selection === "string") {
            this._appendToSlot("select", ",", selection)
        }
        else if (typeof selection === "function") {
            selection(this)
        }
        else if (Array.isArray(selection)) {
            this._appendToSlot("select", ",", selection.join(","))
        }

        return this
    }

    expand<TExpandType = any>(
        expandedProp: StringKeys<T>,
        selection?: StringKeys<TExpandType>[] | string | ((builder: ODataCoreQueryBuilder<TExpandType>) => void)
    ): this {
        const expandContainer = new ExpandContainer()
        expandContainer.expandedProp = expandedProp
        this._effectiveContainer.expansions.push(expandContainer)

        const prev = this.#expandContainer
        this.#expandContainer = expandContainer

        // TODO: Assess whether we can reuse the
        // current builder instance and just return an interface.
        const builder = this as any as ODataCoreQueryBuilder<TExpandType> //
        // new ODataCoreQueryBuilder<TExpandType>(expandContainer)

        if (selection) {
            builder.select(selection)
        }

        this.#expandContainer = prev

        return this
    }

    filter(filterBuildFn: (filterBuilder: ODataFilterBuilder<T>) => void): this {
        if (filterBuildFn) {
            const fb = new ODataFilterBuilder<T>()

            filterBuildFn(fb)

            this._effectiveContainer.filter = fb.toString()
        }

        return this
    }

    assignFromFilterBuilder(filterBuilder: ODataFilterBuilder<T>) {
        this._root.filter = filterBuilder.toString()
    }

    _appendToSlot(slot: SlotType, separator: string | null, textToAppend: string): this {
        let text = this._effectiveContainer[slot]
        if (separator && text) {
            text += separator
        }

        this._effectiveContainer[slot] = text + textToAppend

        return this
    }
}

function joinDataPathSegments(separator: string, pathSegments: string[]): string {
    return pathSegments.reduce((acc, next) => acc + separator + next)
}

type DataPathBuildFn<T> = (b: DataPathBuilder<T>) => DataPath

export class ODataApplyBuilder<T> {
    #filter = ""
    #groupByProps = ""
    // TODO: Check out OData aggregation: https://devblogs.microsoft.com/odata/aggregation-extensions-in-odata-asp-net-core/

    filter(BuildFilterFn: (filterBuilder: ODataFilterBuilder<T>) => void): this {
        if (BuildFilterFn) {
            const fb = new ODataFilterBuilder<T>()

            BuildFilterFn(fb)

            this.#filter = fb.toString()
        }

        return this
    }

    // TODO: Continue with OData "aggregate".
    // TODO: Support plain strings.
    groupby(buildGroupBy: DataPathBuildFn<T> | DataPathBuildFn<T>[]): this {
        if (typeof buildGroupBy === "function") {
            buildGroupBy = [buildGroupBy]
        }

        if (Array.isArray(buildGroupBy) && buildGroupBy.length) {
            for (const [index, buildFn] of buildGroupBy.entries()) {
                const dataPath = buildFn(new DataPathBuilder<T>())
                const path = joinDataPathSegments("/", dataPath.segments)

                if (index > 0) {
                    this.#groupByProps += ","
                }

                this.#groupByProps += `${path}`
            }
        }

        return this
    }

    toString() {
        const expressions: string[] = []

        if (this.#filter) {
            expressions.push(`filter(${this.#filter})`)
        }

        if (this.#groupByProps) {
            expressions.push(`groupby((${this.#groupByProps}))`)
        }

        return expressions.length
            ? expressions.reduce((acc, next) => acc + "/" + next)
            : ""
    }
}

export class ODataQueryBuilder<T> extends ODataCoreQueryBuilder<T> {
    #url = ""
    #options: SimpleODataQueryBuilderOptions

    constructor(options?: SimpleODataQueryBuilderOptions) {
        super(options?._root ?? new Container())

        this.#options = options ?? new SimpleODataQueryBuilderOptions()
    }

    clone(): ODataQueryBuilder<T> {
        const clone = new ODataQueryBuilder<T>()
        this._copyTo(clone)

        return clone
    }

    protected override _copyTo(target: any) {
        super._copyTo(target)

        target.#url = this.#url
        target.#options = this.#options
    }

    url(url: string): this {
        this.#url = url

        return this
    }

    param(paramName: string, value: ODataValueType): this {
        let effectiveValue: string
        if (value === null) {
            effectiveValue = "null"
        } else if (typeof value === "string") {
            effectiveValue = value
        } else if (typeof value === "number") {
            effectiveValue = value.toString()
        } else if (typeof value === "boolean") {
            effectiveValue = value.toString()
        } else if (value instanceof Date || DateTime.isDateTime(value)) {
            effectiveValue = toODataDateTime(value, false) ?? ""
        } else if (value instanceof DateOnlyValue) {
            effectiveValue = toODataDateOnly(value.date, false) ?? ""
        } else if (value instanceof TextValue) {
            effectiveValue = `'${value.text}'`
        } else if (value instanceof GuidValue) {
            effectiveValue = value.guid
        } else {
            throw new Error(`Unexpected odata query parameter value type '${typeof value}'.`)
        }

        this._appendToSlot("params", "&", `${paramName}=${encodeURIComponent(effectiveValue)}`)

        return this
    }

    // TODO: Support multiple order-by props.
    orderby(
        orderBy: StringKeys<T> | DataPathBuildFn<T> | DataPath,
        direction?: ODataOrderByDirection
    ): this {
        let orderByPath: string | undefined
        if (typeof orderBy === "string") {
            orderByPath = orderBy as any as string
        }
        else if (typeof orderBy === "function") {
            orderByPath = toODataPropPath(orderBy(new DataPathBuilder<T>()))
        }
        else if (orderBy instanceof DataPath) {
            orderByPath = toODataPropPath(orderBy)
        }

        if (orderByPath) {
            this._appendToSlot("orderby", ",", (orderByPath ?? "") + (direction ? `+${direction}` : ""))
        }

        return this
    }

    skip(count: number): this {
        this._effectiveContainer.skip = count

        return this
    }

    top(count: number): this {
        this._effectiveContainer.top = count

        return this
    }

    apply(buildApply: (b: ODataApplyBuilder<T>) => ODataApplyBuilder<T>): this {
        const applyBuilder = buildApply(new ODataApplyBuilder<T>())
        this._root.apply = applyBuilder.toString()

        return this
    }

    isSinglePropSelection(): boolean {
        let selectedPropCount = 0

        this.#visitContainers((container: Container) => {
            // TODO: Since we don't operate on an AST we have to hack with strings here :-(
            if (container.select) {
                if (container.select.indexOf(",") !== -1) {
                    selectedPropCount += 2
                    return false
                }

                selectedPropCount++
            }

            return selectedPropCount <= 1
        })

        return selectedPropCount === 1
    }

    #visitContainers(visit: (container: Container, level: number) => boolean | void): void {
        this.#traverseContainerTree(this._root, 0, visit)
    }

    #traverseContainerTree(item: Container, level: number, callbackFn: (container: Container, level: number) => boolean | void): boolean {
        const result = callbackFn(item, level)
        if (result === false) return false

        if (item.expansions) {
            for (const subContainer of item.expansions) {
                if (this.#traverseContainerTree(subContainer, level + 1, callbackFn) === false) {
                    return false
                }
            }
        }

        return true
    }

    override toString(): string {
        const query = this.#serializeContainer(this._root, 0)

        let url = this.#url ?? ""

        if (this._root.params) {
            if (!url.includes("?")) {
                url += "?"
            } else {
                url += "&"
            }

            url += this._root.params
        }

        if (!url.includes("?")) {
            url += "?"
        } else {
            url += "&"
        }

        url += query

        return url
    }

    toFilterString(): string {
        return this._root.filter ?? ""
    }

    #addSeparator(expr: string, sep: string, nextExpr: string): string {
        return expr ? sep + nextExpr : nextExpr
    }

    #processExpression(expression: string): string {
        if (!this.#options.usePascalCase) return expression

        const separator = ","
        return expression
            .split(separator)
            .filter(t => !!t && t.trim().length > 0)
            .map(t => {
                const token = t.trim()

                return token[0] + token.slice(1)
            })
            .join(separator)
    }

    #serializeContainer(item: Container, level: number): string {
        const separator = level === 0 ? "&" : ";"
        let result = ""

        if (item.select) {
            result += "$select=" + this.#processExpression(item.select)
        }

        if (item.filter) {
            result += this.#addSeparator(result, separator, "$filter=" + item.filter)
        }

        if (item.orderby) {
            result += this.#addSeparator(result, separator, "$orderby=" + item.orderby)
        }

        if (item.skip !== null && item.skip > 0) {
            result += this.#addSeparator(result, separator, "$skip=" + item.skip)
        }

        if (item.top !== null && item.top > -1) {
            result += this.#addSeparator(result, separator, "$top=" + item.top)
        }

        if (item.apply) {
            result += this.#addSeparator(result, separator, "$apply=" + item.apply)
        }

        if (item.expansions.length) {
            result += this.#addSeparator(result, separator, "$expand=")
            let expansion: ExpandContainer
            for (let i = 0; i < item.expansions.length; i++) {
                expansion = item.expansions[i]
                result += expansion.expandedProp
                if (expansion.select || expansion.expansions.length) {
                    result += "("
                    result += this.#serializeContainer(expansion, level + 1)
                    result += ")"
                }
                if (i + 1 < item.expansions.length) {
                    result += ","
                }
            }
        }

        return result
    }
}

export class AnyODataQueryBuilder {
    #url?: string

    url(url: string): this {
        this.#url = url

        return this
    }

    from<T>(): ODataQueryBuilder<T> {
        const q = new ODataQueryBuilder<T>()
        if (this.#url) {
            q.url(this.#url)
        }

        return q
    }
}
type PropOrPathType<T> = StringKeys<T> | DataPath

export class ODataFilterBuilder<T = any> {
    protected _filter = ""

    protected _copyTo(target: any) {
        target._filter = this._filter
    }

    clone(): ODataFilterBuilder<T> {
        const clone = new ODataFilterBuilder<T>()
        this._copyTo(clone)

        return clone
    }

    where(prop: PropOrPathType<T>, comparisonOp: ODataComparisonOperator, value: ODataValueType): this {
        let effectiveProp = toODataPropPath(prop)

        let effectiveValue: string
        if (value === null) {
            effectiveValue = "null"
        } else if (typeof value === "string") {
            effectiveValue = value
        } else if (typeof value === "number") {
            effectiveValue = value.toString()
        } else if (typeof value === "boolean") {
            effectiveValue = value.toString()
        } else if (value instanceof Date || DateTime.isDateTime(value)) {
            effectiveValue = toODataDateTime(value, true) ?? ""
        } else if (value instanceof DateOnlyValue) {
            effectiveProp = `date(${prop})`
            effectiveValue = toODataDateOnly(value.date, true) ?? ""
        } else if (value instanceof TextValue) {
            effectiveValue = `'${value.text}'`
        } else if (value instanceof GuidValue) {
            effectiveValue = value.guid
        } else {
            throw new Error(`Unexpected odata query filter value type '${typeof value}'.`)
        }

        this._append(` ${effectiveProp} ${comparisonOp} ${effectiveValue}`)

        return this
    }

    eq = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "eq", value)

    ne = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "ne", value)

    gt = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "gt", value)

    ge = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "ge", value)

    lt = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "lt", value)

    le = (prop: PropOrPathType<T>, value: ODataValueType): this => this.where(prop, "le", value)

    #logicalOp(logicalOp: ODataLogicalOperator): this {
        if (!this._filter) return this

        if (this.#isSubStart) {
            this.#isSubStart = false

            return this
        }

        if (!this._filter) {
            return this
        }

        this._append(` ${logicalOp}`)

        return this
    }

    and = (): this => this.#logicalOp("and")

    or = (): this => this.#logicalOp("or")

    #isSubStart = false

    andSub(build: () => void) {
        this.and()
        this._append("(")
        this.#isSubStart = true
        build()
        this._append(")")

        return this
    }

    sub(build: () => void) {
        this._append("(")
        this.#isSubStart = true
        build()
        this._append(")")

        return this
    }

    not = (): this => this.#logicalOp("not")

    contains(prop: PropOrPathType<T>, value: string): this {
        return this._append(` contains(${toODataPropPath(prop)},'${value}')`)
    }

    anyExpression(prop: PropOrPathType<T>, odataLambdaExpression: string) {
        return this._append(`${toODataPropPath(prop)}/any(${odataLambdaExpression})`)
    }

    protected _append(text: string): this {
        if (!text) return this

        this.#isSubStart = false

        if (!this._filter) {
            text = text.trim()
        } else if (text[0] !== " " && this._filter[this._filter.length - 1] !== " ") {
            // Ensure space between conditions.
            text = " " + text
        }

        this._filter += text

        return this
    }

    toString(): string {
        return this._filter.trim()
    }
}

function toODataPropPath(prop: PropOrPathType<any>): string {
    return prop instanceof DataPath
        ? prop.segments.reduce((acc, next) => acc + "/" + next)
        : prop
}
