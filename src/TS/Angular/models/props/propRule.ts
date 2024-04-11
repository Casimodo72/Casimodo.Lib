import { IFormPropCore, _IInternalFormGroupValidationManager } from "./core"
import { FormProp } from "./prop"

export type RuleResult = string | null
export type RuleValidationFn = (context: ValidationContext) => RuleResult
export type RuleAsyncValidationFn = (context: ValidationContext) => Promise<RuleResult>
const noopValidationFn: RuleValidationFn = () => null
const noopAsyncValidationFn: RuleAsyncValidationFn = () => Promise.resolve(null)
const emptyPropArray: IFormPropCore[] = []
// TODO: REMOVE? const emptyPropRuleArray: PropRule[] = []

export class ValidationContext {
    prop!: FormProp
    rule!: FormPropRule
}

// TODO: We are relying on the group and prop instances which
//   will make implementation of static rules (per type) impossible :-(
//   Maybe static rules are actually not implementable in TS/JS.
export class FormPropRule {
    readonly id: string
    readonly prop: IFormPropCore
    readonly additionalSourceProps: IFormPropCore[]
    readonly validate: RuleValidationFn | undefined
    readonly validateAsync: RuleAsyncValidationFn | undefined

    constructor(
        id: string,
        targetProp: IFormPropCore,
        additionalSourceProps: IFormPropCore[] | null,
        validateFn?: RuleValidationFn,
        validateAsync?: RuleAsyncValidationFn
    ) {
        this.id = id
        this.prop = targetProp
        this.additionalSourceProps = additionalSourceProps ?? emptyPropArray
        this.validate = validateFn
        this.validateAsync = validateAsync
    }

    isSourceProp(prop: IFormPropCore): boolean {
        return this.prop === prop || this.additionalSourceProps.includes(prop)
    }

    isTargetProp(prop: IFormPropCore): boolean {
        return this.prop === prop
    }
}

export class FormPropRules {
    readonly items: FormPropRule[] = []

    add(rule: FormPropRule): void {
        this.items.push(rule)
    }
}

export class PropRuleDefinition {
    constructor(
        id: string,
        validateFn?: RuleValidationFn,
        validateAsync?: RuleAsyncValidationFn
    ) {
        this.id = id
        this.validate = validateFn ?? noopValidationFn
        this.validateAsync = validateAsync ?? noopAsyncValidationFn
    }

    readonly id: string
    readonly validate: RuleValidationFn
    readonly validateAsync: RuleAsyncValidationFn
}

export function createIbanRule(opts?: { countryCode?: string }): PropRuleDefinition {
    return new PropRuleDefinition(
        "#iban#",
        (context: ValidationContext): RuleResult => {
            const value = context.rule.prop.value()

            if (!value || typeof value !== "string") {
                return null
            }

            // TODO: How to use i18n in validation functions?

            if (opts?.countryCode && !value.startsWith(opts.countryCode)) {
                return `Diese IBAN ist nicht gültig. Bitte geben Sie eine IBAN ein, welche mit ${opts.countryCode} beginnt.`
            }

            // TODO: Validation
            return "Diese IBAN ist nicht gültig. Bitte geben Sie eine gültige IBAN ein."
        })
}
