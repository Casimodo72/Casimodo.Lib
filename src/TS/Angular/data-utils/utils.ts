import { DateTime } from "luxon"

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
}

class ExpandContainer extends Container {
    expandedProp = ""
}

export class SimpleODataQueryBuilderOptions {
    usePascalCase = true
}

type SlotType = "params" | "select" | "orderby" | "filter"

export type ODataComparisonOperator = "eq" | "ne" | "gt" | "ge" | "lt" | "le"

export type ODataLogicalOperator = "and" | "or" | "not";

type StringKeys<T> = Extract<keyof T, string>

type ODataOrderByDirection = "asc" | "desc"

type ValueType = string | number | boolean | Date | DateTime | null | GuidValue | TextValue | DateOnlyValue

class GuidValue {
    constructor(public readonly guid: string) { }
}

export function toGuid(guid: string): ValueType {
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

        const builder = new ODataCoreQueryBuilder<TExpandType>(expandContainer)

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

    _appendToSlot(slot: SlotType, separator: string | null, appendText: string): this {
        let text = this._effectiveContainer[slot]
        if (separator && text) {
            text += separator
        }

        this._effectiveContainer[slot] = text + appendText

        return this
    }
}

export class ODataQueryBuilder<T> extends ODataCoreQueryBuilder<T> {
    #url = ""
    #options: SimpleODataQueryBuilderOptions

    constructor(options?: SimpleODataQueryBuilderOptions) {
        super(new Container())

        this.#options = options ?? new SimpleODataQueryBuilderOptions()
    }

    url(url: string): this {
        this.#url = url

        return this
    }

    param(paramName: string, value: ValueType): this {
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

    orderby(orderby: StringKeys<T>, direction?: ODataOrderByDirection): this {
        this._appendToSlot("orderby", ",", (orderby ?? "") + (direction ? `+${direction}` : ""))

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

export class ODataFilterBuilder<T = any> {
    #filter = ""

    get filter(): string {
        return this.#filter
    }

    where(prop: StringKeys<T>, comparisonOp: ODataComparisonOperator, value: ValueType): this {
        let effectiveProp: string = prop
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

        this.#append(` ${effectiveProp} ${comparisonOp} ${effectiveValue}`)

        return this
    }

    eq = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "eq", value)

    ne = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "ne", value)

    gt = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "gt", value)

    ge = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "ge", value)

    lt = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "lt", value)

    le = (prop: StringKeys<T>, value: ValueType): this => this.where(prop, "le", value)

    #logicalOp(logicalOp: ODataLogicalOperator): this {
        if (!this.#filter) return this

        if (this.#isSubStart) {
            this.#isSubStart = false

            return this
        }

        if (!this.#filter) {
            return this
        }

        this.#append(` ${logicalOp}`)

        return this
    }

    and = (): this => this.#logicalOp("and")

    or = (): this => this.#logicalOp("or")

    #isSubStart = false

    andSub(build: () => void) {
        this.and()
        this.#append("(")
        this.#isSubStart = true
        build()
        this.#append(")")

        return this
    }

    sub(build: () => void) {
        this.#append("(")
        this.#isSubStart = true
        build()
        this.#append(")")

        return this
    }

    not = (): this => this.#logicalOp("not")

    contains(prop: StringKeys<T>, value: string): this {
        return this.#append(` contains(${prop},'${value}')`)
    }

    anyExpression(prop: StringKeys<T>, odataLambdaExpression: string) {
        return this.#append(`${prop}/any(${odataLambdaExpression})`)
    }

    #append(text: string): this {
        if (!text) return this

        this.#isSubStart = false

        if (!this.#filter) {
            text = text.trim()
        } else if (text[0] !== " " && this.#filter[this.#filter.length - 1] !== " ") {
            // Ensure space between conditions.
            text = " " + text
        }

        this.#filter += text

        return this
    }

    toString(): string {
        return this.filter.trim()
    }
}
