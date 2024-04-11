import { DateTime } from "luxon"
import { IFormPropCore, IFormItem, _getInternalValidation } from "./core"
import { FormPropRule, RuleResult, RuleValidationFn, ValidationContext } from "./propRule"

export interface ICustomRule {
    id?: string,
    sourceProps?: IFormPropCore[]
    validate: RuleValidationFn
}

export class FormPropRulesBuilder<TData = any> {
    readonly parent: IFormItem
    readonly prop: IFormPropCore<TData>
    /**
     * Used e.g. for differentiation of dates (date or time or date-time).
     */
    protected _dataKind?: string

    constructor(group: IFormItem, prop: IFormPropCore<TData>) {
        this.parent = group
        this.prop = prop

        this.init()
    }

    protected init(): void {
        // NOOP
    }

    /**
     * Adds a custom rule.
     */
    custom(customRule: ICustomRule): this {
        const rule = new FormPropRule(
            customRule.id ?? crypto.randomUUID(),
            this.prop,
            customRule.sourceProps ?? null,
            customRule.validate,
            undefined)

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }

    notRequired(): this {
        _getInternalValidation(this.parent).removeInstanceRuleById("#required#")

        return this
    }

    required(errorMessage?: string | undefined): this {
        const rule = new FormPropRule(
            "#required#",
            this.prop,
            null,
            (context: ValidationContext): RuleResult => {
                const value = context.rule.prop.value()

                if (this.isValueEmpty(value)) {
                    return errorMessage
                        ? errorMessage
                        : "Ein Wert wird benötigt." // `${prop.label}: Ein Wert wird benötigt.`
                }

                return null
            })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }

    protected isValueEmpty(value: any): boolean {
        return value === null ||
            value === undefined ||
            (typeof value === "string" && !value) ||
            (Array.isArray(value) && !value.length)
    }

    protected minimumStringOrNumberCore(minimum: any, errorMessage?: string): this {
        const rule = new FormPropRule(
            "#minimum#",
            this.prop,
            null,
            (context: ValidationContext): RuleResult => {
                const value = context.rule.prop.value() as string | number | Date | null | undefined

                if (this.isValueEmpty(value)) {
                    // This validation needs an actual value.
                    return null
                }

                if (typeof value === "string" && value.length < minimum) {
                    return errorMessage
                        ? errorMessage
                        : `Der Text muss mindestens ${minimum} Zeichen lang sein.`
                }
                else if (typeof value === "number" && value < minimum) {
                    return errorMessage
                        ? errorMessage
                        : `Die Zahl muss mindestens ${minimum} betragen.`
                }
                else if (DateTime.isDateTime(value)) {
                    const dateTime = value as DateTime
                    const minimumDateTime = minimum as DateTime
                    if (dateTime < minimumDateTime) {
                        if (errorMessage) {
                            return errorMessage
                        }

                        if (this._dataKind === "date-only") {
                            return "Das Datum darf nicht vor " +
                                `${minimumDateTime.toLocaleString(DateTime.DATE_SHORT)} liegen.`
                        }
                        else if (this._dataKind === "time-only") {
                            return "Die Uhrzeit darf nicht vor " +
                                `${minimumDateTime.toLocaleString(DateTime.TIME_SIMPLE)} ` +
                                `(${minimumDateTime.toLocaleString(DateTime.DATE_SHORT)}) liegen.`
                        }
                        else if (this._dataKind === "date-time") {
                            return "Das Datum und die Uhrzeit darf nicht vor " +
                                `${minimumDateTime.toLocaleString(DateTime.DATETIME_SHORT)} liegen.`
                        }
                    }
                }
                else if (value instanceof Date && (value as Date) < (minimum as Date)) {
                    if (errorMessage) {
                        return errorMessage
                    }

                    if (this._dataKind === "date-only") {
                        return `Das Datum darf nicht vor ${minimum} liegen.`
                    }
                    else if (this._dataKind === "time-only") {
                        return `Die Uhrzeit darf nicht vor ${minimum} liegen.`
                    }
                    else if (this._dataKind === "date-time") {
                        return `Das Datum und die Uhrzeit darf nicht vor ${minimum} liegen.`
                    }
                }

                return null
            })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }

    protected maximumStringOrNumberCore(maximum: any, errorMessage?: string): this {
        const rule = new FormPropRule(
            "#maximum#",
            this.prop,
            null,
            (context: ValidationContext): RuleResult => {
                const prop = context.rule.prop
                const value = context.rule.prop.value() as string | number | Date | null | undefined

                if (this.isValueEmpty(value)) {
                    // This validation needs an actual value.
                    return null
                }

                if (typeof value === "string" && value.length > maximum) {
                    return errorMessage
                        ? errorMessage
                        : `Der Text darf nicht länger als ${maximum} Zeichen lang sein.`
                }
                else if (typeof value === "number" && value > maximum) {
                    return errorMessage
                        ? errorMessage
                        : `Die Zahl darf nicht mehr als ${maximum} betragen.`
                }
                else if (DateTime.isDateTime(value)) {
                    const dateTime = value as DateTime
                    const maximumDateTime = maximum as DateTime
                    if (dateTime > maximumDateTime) {
                        if (errorMessage) {
                            return errorMessage
                        }

                        if (this._dataKind === "date-only") {
                            return "Das Datum darf nicht nach " +
                                `${maximumDateTime.toLocaleString(DateTime.DATE_SHORT)} liegen.`
                        }
                        else if (this._dataKind === "time-only") {
                            return "Die Uhrzeit darf nicht nach " +
                                `${maximumDateTime.toLocaleString(DateTime.TIME_SIMPLE)} ` +
                                `(${maximumDateTime.toLocaleString(DateTime.DATE_SHORT)}) liegen.`
                        }
                        else if (this._dataKind === "date-time") {
                            return "Das Datum und die Uhrzeit darf nicht nach " +
                                `${maximumDateTime.toLocaleString(DateTime.DATETIME_SHORT)} liegen.`
                        }
                    }
                }
                else if (value instanceof Date && (value as Date) > (maximum as Date)) {
                    if (errorMessage) {
                        return errorMessage
                    }

                    if (this._dataKind === "date-only") {
                        return `Das Datum darf nicht nach ${maximum} liegen.`
                    }
                    else if (this._dataKind === "time-only") {
                        return `Die Uhrzeit darf nicht nach ${maximum} liegen.`
                    }
                    else if (this._dataKind === "date-time") {
                        return `Das Datum und die Uhrzeit darf nicht nach ${maximum} liegen.`
                    }
                }

                return null
            })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }
}
