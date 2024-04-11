import { IFormItem, _getInternalValidation } from "./core"
import { FormProp } from "./prop"
import { FormPropRule, PropRuleDefinition } from "./propRule"
import { FormPropRulesBuilder } from "./propRuleBuilder"

export class StringFormProp extends FormProp<string> {
    #isTrimming = false

    constructor(group: IFormItem, initialValue?: string) {
        super(group, initialValue ?? "")
    }

    protected override convertFromDomInputValueToData(value: any): any {
        return value ?? "" as string
    }

    protected override normalizeCore(): void {
        //console.debug("## prop: normalizeCore")
        // TODO: The validation errors after trimming do not end up in the UI :-(
        this.applyTrimming()
    }

    // setValueOrNone(value: string | null | undefined): boolean {
    //     return this.setValue(value ?? "")
    // }

    applyTrimming() {
        if (this.#isTrimming) {
            //console.debug("## prop: trimming")
            this.setValue(this.value().trim())
        }
    }

    setTrimming(): this {
        this.#isTrimming = true

        return this
    }

    getNormalizedValue(): string {
        let value = this.value()
        if (this.#isTrimming) {
            value = value.trim()
        }

        return value
    }

    setRules(rulesBuildFn: (rulesBuilder: StringFormPropRulesBuilder) => void): this {
        const rulesBuilder = new StringFormPropRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

export class StringFormPropRulesBuilder extends FormPropRulesBuilder<string> {
    min(minimum: number, errorMessage?: string): this {
        return this.minimumStringOrNumberCore(minimum, errorMessage)
    }

    /**
     * Adds a well-known rule definition.
     * @param ruleDefinition
     * @returns
     */
    rule(ruleDefinition: PropRuleDefinition): this {
        const rule = new FormPropRule(
            ruleDefinition.id,
            this.prop,
            null,
            ruleDefinition.validate,
            ruleDefinition.validateAsync)

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }
}
