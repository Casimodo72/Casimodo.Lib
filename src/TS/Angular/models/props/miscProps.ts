import { DateTime } from "luxon"
import { IFormItem } from "./core"
import { FormProp } from "./prop"
import { FormPropRulesBuilder as FormPropRulesBuilder } from "./propRuleBuilder"
import { Signal, computed, signal } from "@angular/core"
import { StringFormProp as StringFormProp } from "./stringProp"
import { PickerItemModel, PickerFormProp } from "./selectableProp"
import { ListModel } from "../lists"

export class SearchableStringFormProp extends StringFormProp {
    readonly #pickableItemList = new ListModel<PickerItemModel<string>>()
    // readonly #searchText = signal("")
    // readonly searchText = this.#searchText.asReadonly()
    readonly isSearchOpen = signal(false)
    readonly isSearchMatch = computed(() => {
        const items = this.picker.pickableItems()
        const searchText = this.searchValue()?.trim().toLocaleLowerCase() ?? ""

        if (!searchText) {
            return true
        }

        return items.find(x => !!x.data && x.data.toLocaleLowerCase() === searchText) !== undefined
    })

    readonly picker = new PickerFormProp<string>(this)
        .setOnValueChanged(value => {
            this.setValue(value ?? "")
        })

    readonly searchItems = this.picker.pickableItems

    readonly searchValue = this.picker.filterValue

    override setValue(value: string, validate?: boolean): boolean {
        const result = super.setValue(value, validate)

        if (result) {
            this.picker.setFilterValue(value)
        }

        return result
    }

    addSearchValue(value: string) {
        this.picker.addPickValue(value)
    }

    sortSearchValues() {
        const items = this.picker.items().sort((a, b) => a.data.localeCompare(b.data))
        this.picker.setPickItems(items)
    }

    setSearchValue(searchText: string | null) {
        this.picker.setFilterValue(searchText ?? "")

        //this.#searchText.set(searchText ?? "")
    }

    setSearchValues(values: string[]): this {
        // const pickItems: PickerItemModel<string>[] = []
        // for (const value of values) {
        //     pickItems.push(new PickerItemModel<string>(value))
        // }
        // this.#pickableItemList.setItems(pickItems)

        this.picker.setPickValues(values)

        return this
    }
}

// TODO: Do we want/need to differentiate between nullables and non-nullables?
export class NumberFormProp extends FormProp<number | null> {
    protected override convertFromDomInputValueToData(value: any): any | null {
        if (!value) {
            return null
        }

        const numberValue = Number.parseInt(value)

        if (Number.isNaN(numberValue)) {
            return null
        }

        return numberValue
    }

    setRules(rulesBuildFn: (rulesBuilder: NumberFormPropRulesBuilder) => void): this {
        const rulesBuilder = new NumberFormPropRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class NumberFormPropRulesBuilder extends FormPropRulesBuilder<number | null> {
    min(minimum: number, errorMessage?: string): this {
        return this.minimumStringOrNumberCore(minimum, errorMessage)
    }
}

export class BooleanFormProp extends FormProp<boolean> {
    protected override convertFromDomInputValueToData(value: any): any {
        if (!value) {
            return false
        }

        // TODO

        return true
    }
}

export abstract class AnyDateTimeFormProp<T = any> extends FormProp<T> {
    abstract readonly _type: string

    abstract readonly timeValueAsText: Signal<string>
    abstract readonly dateValueAsText: Signal<string>

    /** Not implemented; don't use */
    abstract readonly minValue: Signal<T | null>
    /** Not implemented; don't use */
    abstract readonly maxValue: Signal<T | null>
    /** Not implemented; don't use */
    abstract readonly minValueAsString: Signal<string>

    // setTimeValueAsText(inputString: string | null): void {
    //     const inputValue = this.convertFromDomInputValueToData(inputString)
    //     if (inputValue === null) {
    //         this.setValue(null)
    //     }
    //     else {
    //         // Preserve the date portion of the current value if applicable.
    //         const currentValue = this.value()
    //         if (currentValue !== null) {
    //             inputValue.set({
    //                 year: currentValue.year,
    //                 month: currentValue.month,
    //                 day: currentValue.day
    //             })
    //         }

    //         this.setValue(inputValue)
    //     }
    //}
}

// TODO: Use Date or Time models not DateTime models.
// TODO: Do we want/need to differentiate between nullables and non-nullables?
abstract class AnyLuxonDateTimeFormProp extends AnyDateTimeFormProp<DateTime | null> {
    readonly timeValueAsText = computed(() => {
        const time = this.value()
        return time
            ? time.toLocaleString(DateTime.TIME_SIMPLE)
            : ""
    })

    readonly dateValueAsText = computed(() => {
        const time = this.value()
        return time
            ? time.toLocaleString(DateTime.DATE_SHORT)
            : ""
    })

    /** @inheritdoc */
    override readonly minValue = signal<DateTime | null>(null)
    /** @inheritdoc */
    override readonly maxValue = signal<DateTime | null>(null)
    /** @inheritdoc */
    override readonly minValueAsString = computed(() => {
        return this.minValue()?.toLocaleString(DateTime.TIME_SIMPLE) ?? ""
    })

    constructor(parent: IFormItem, initialValue?: DateTime | null) {
        super(parent, initialValue ?? null)
    }

    setMin(minValue: DateTime) {
        this.minValue.set(minValue)
    }

    setMax(maxValue: DateTime) {
        this.maxValue.set(maxValue)
    }

    protected override convertFromDomInputValueToData(inputString: any): any | null {
        if (!inputString || typeof inputString !== "string") {
            return null
        }

        let parsedValue: DateTime | null = null

        if (this._type === "time-only") {
            parsedValue = this._parseTimeInput(inputString)
            if (parsedValue) {
                // Preserve the date portion of the current value if applicable.
                const currentValue = this.value()
                if (currentValue !== null) {
                    parsedValue = parsedValue.set({
                        year: currentValue.year,
                        month: currentValue.month,
                        day: currentValue.day
                    })
                }
            }
        }
        else if (this._type === "date-only") {
            // TODO: Do we want to preserve the time portion of the current value? We should.
            parsedValue = this._parseDateInput(inputString)
        }
        else {
            parsedValue = this._parseDateTimeInput(inputString)
        }

        return parsedValue
    }

    protected _parseTimeInput(inputValue: string | null): DateTime | null {
        try {
            const parseFormat = DateTime.parseFormatForOpts(DateTime.TIME_SIMPLE)!
            return inputValue
                ? DateTime.fromFormat(inputValue, parseFormat)
                : null
        } catch {
            return null
        }
    }

    protected _parseDateInput(inputValue: string | null): DateTime | null {
        try {
            const parseFormat = DateTime.parseFormatForOpts(DateTime.DATE_SHORT)!
            return inputValue
                ? DateTime.fromFormat(inputValue, parseFormat)
                : null
        } catch {
            return null
        }
    }

    protected _parseDateTimeInput(_inputValue: string | null): DateTime | null {
        // TODO: Not supported as burdening the user
        // with input using the correct format would be a bit strange.
        // Maybe with a masked input control; but we don't have such a control.
        return null
    }
}

abstract class AnyDateTimeFormPropRulesBuilder extends FormPropRulesBuilder<DateTime | null> {
    override init(): void {
        this._dataKind = "date-time"
    }

    min(minimum: DateTime, errorMessage?: string): this {
        return this.minimumStringOrNumberCore(minimum, errorMessage)
    }

    max(maximum: DateTime, errorMessage?: string): this {
        return this.maximumStringOrNumberCore(maximum, errorMessage)
    }
}

export class DateFormProp extends AnyLuxonDateTimeFormProp {
    override readonly _type = "date-only"

    setJsValue(value: Date | null): boolean {
        return this.setValue(value ? DateTime.fromJSDate(value) : null)
    }

    setRules(rulesBuildFn: (rulesBuilder: DateFormPropRulesBuilder) => void): this {
        const rulesBuilder = new DateFormPropRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class DateFormPropRulesBuilder extends AnyDateTimeFormPropRulesBuilder {
    override init(): void {
        this._dataKind = "date-only"
    }
}

export class TimeFromProp extends AnyLuxonDateTimeFormProp {
    override readonly _type = "time-only"
    #defaultDate?: DateTime | undefined

    // protected override _setInputValue(inputValue: DateTime) {
    //     // Preserve the date portion of the current value if applicable.
    //     const currentValue = this.value()
    //     if (currentValue !== null) {
    //         inputValue.set({
    //             year: currentValue.year,
    //             month: currentValue.month,
    //             day: currentValue.day
    //         })
    //     }
    //     this._value.set(inputValue)
    // }

    protected override convertFromDomInputValueToData(inputString: any): any | null {
        let value = super.convertFromDomInputValueToData(inputString)
        if (value && !this.value() && this.#defaultDate) {
            value = (value as DateTime).set({
                year: this.#defaultDate.year,
                month: this.#defaultDate.month,
                day: this.#defaultDate.day
            })
        }

        return value
    }

    setDefaultDate(defaultDate: DateTime | null | undefined) {
        this.#defaultDate = defaultDate ?? undefined
    }

    setJsValue(value: Date | null): boolean {
        return this.setValue(value ? DateTime.fromJSDate(value) : null)
    }

    setRules(rulesBuildFn: (rulesBuilder: TimeFormPropRulesBuilder) => void): this {
        const rulesBuilder = new TimeFormPropRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class TimeFormPropRulesBuilder extends AnyDateTimeFormPropRulesBuilder {
    override init(): void {
        this._dataKind = "time-only"
    }
}

// TODO: Use Date or Time models not DateTime models.
// TODO: Do we want/need to differentiate between nullables and non-nullables?
/*
class DateTimeProp extends AnyLuxonDateTimeProp {
    override readonly _type = "date-time"

    setRules(rulesBuildFn: (rulesBuilder: DateTimePropRulesBuilder) => void): this {
        const rulesBuilder = new DateTimePropRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}
*/

// TODO: REMOVE? We currently try to avoid using JS Date. Use Luxon DateTime instead.
/*
class JsDatePropRulesBuilder extends PropRulesBuilder<Date | null> {
    override init(): void {
        this._dataKind = "date-time"
    }

    min(minimum: Date, errorMessage?: string): this {
        return this.minimumStringOrNumberCore(minimum, errorMessage)
    }
}

class JsDateProp extends Prop<Date | null> {
    constructor(group: IPropItem, initialValue?: Date | null) {
        super(group, initialValue ?? null)
    }

    #parseTimeValue(value: string | null): Date | null {
        try {
            const dateTime = value
                ? DateTime.fromFormat(value, "HH:mm")
                : null

            return dateTime?.toJSDate() ?? null
        } catch {
            return null
        }
    }

    protected override convertFromDomValueToData(value: any): any | null {
        return value && typeof value === "string"
            ? this.#parseTimeValue(value as string)
            : null
    }

    setRules(rulesBuildFn: (rulesBuilder: JsDatePropRulesBuilder) => void): this {
        const rulesBuilder = new JsDatePropRulesBuilder(this.group, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}
*/
