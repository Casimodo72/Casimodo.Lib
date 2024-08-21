import { Signal, computed, signal } from "@angular/core"
import { DateTime } from "luxon"

import { FormInputModel, IFormPart, FormRulesBuilder } from "../core"

type DateTimeKind = "date-only" | "time-only"

export abstract class AnyDateTimeInputModel<T = any> extends FormInputModel<T> {
    abstract readonly _type: DateTimeKind

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
export abstract class AnyLuxonDateTimeInputModel extends AnyDateTimeInputModel<DateTime | null> {
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

    constructor(parent: IFormPart, initialValue?: DateTime | null) {
        super(parent, initialValue ?? null)
    }

    protected override _isValueEqual(value: DateTime | null | undefined, value2: DateTime | null | undefined): boolean {
        if (super._isValueEqual(value, value2)) return true

        if (value == null || value2 == null) return false

        if (DateTime.isDateTime(value) && DateTime.isDateTime(value2)) {
            return value.toMillis() === value2.toMillis()
        }

        return false
    }

    setMin(minValue: DateTime) {
        this.minValue.set(minValue)
    }

    setMax(maxValue: DateTime) {
        this.maxValue.set(maxValue)
    }

    protected override _convertFromDomInputValueToData(inputString: any): any | null {
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
            const parsedValue = inputValue
                ? DateTime.fromFormat(inputValue, parseFormat)
                : null

            return parsedValue?.isValid
                ? parsedValue
                : null
        } catch {
            return null
        }
    }

    protected _parseDateInput(inputValue: string | null): DateTime | null {
        try {
            const parseFormat = DateTime.parseFormatForOpts(DateTime.DATE_SHORT)!
            const parsedValue = inputValue
                ? DateTime.fromFormat(inputValue, parseFormat)
                : null

            return parsedValue?.isValid
                ? parsedValue
                : null
        } catch {
            return null
        }
    }

    protected _parseDateTimeInput(_inputValue: string | null): DateTime | null {
        // TODO: Not supported as burdening the user
        // with input of a date *and* time using the correct format would be a bit strange.
        // Maybe with a masked input control; but we don't have such a control.
        return null
    }
}

abstract class AnyDateTimeRulesBuilder extends FormRulesBuilder<DateTime | null> {
    override init(): void {
        this._dataKind = "date-time"
    }

    min(minimum: DateTime, errorMessage?: string): this {
        return this.addMinimumRuleCore(minimum, errorMessage)
    }

    max(maximum: DateTime, errorMessage?: string): this {
        return this.addMaximumRuleCore(maximum, errorMessage)
    }
}

export class DateInputModel extends AnyLuxonDateTimeInputModel {
    override readonly _type = "date-only"

    setJsValue(value: Date | null): boolean {
        return this.setValue(value ? DateTime.fromJSDate(value) : null)
    }

    setRules(rulesBuildFn: (rulesBuilder: DateRulesBuilder) => void): this {
        const rulesBuilder = new DateRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class DateRulesBuilder extends AnyDateTimeRulesBuilder {
    override init(): void {
        this._dataKind = "date-only"
    }
}

export class TimeInputModel extends AnyLuxonDateTimeInputModel {
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

    protected override _convertFromDomInputValueToData(inputString: any): any | null {
        let value = super._convertFromDomInputValueToData(inputString)
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

    setRules(rulesBuildFn: (rulesBuilder: TimeRulesBuilder) => void): this {
        const rulesBuilder = new TimeRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class TimeRulesBuilder extends AnyDateTimeRulesBuilder {
    override init(): void {
        this._dataKind = "time-only"
    }
}

// TODO: Use Date or Time models not DateTime models.
// TODO: Do we want/need to differentiate between nullables and non-nullables?
/*
class DateTimeInputModel extends AnyLuxonDateTimeInputModel {
    override readonly _type = "date-time"

    setRules(rulesBuildFn: (rulesBuilder: DateTimeRulesBuilder) => void): this {
        const rulesBuilder = new DateTimeRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}
*/

// TODO: REMOVE? We currently try to avoid using JS Date. Use Luxon DateTime instead.
/*
class JsDateRulesBuilder extends FormRulesBuilder<Date | null> {
    override init(): void {
        this._dataKind = "date-time"
    }

    min(minimum: Date, errorMessage?: string): this {
        return this.minimumStringOrNumberCore(minimum, errorMessage)
    }
}

class JsDateInputModel extends InputModel<Date | null> {
    constructor(parent: IFormPart, initialValue?: Date | null) {
        super(parent, initialValue ?? null)
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

    setRules(rulesBuildFn: (rulesBuilder: JsDateRulesBuilder) => void): this {
        const rulesBuilder = new JsDateRulesBuilder(this.group, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}
*/
