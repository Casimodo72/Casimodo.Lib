import { Signal } from "@angular/core"
import { FormPropRule as FormPropRule } from "./propRule"
import { ValidationError } from "./prop"

export interface IFormItem {
    addPropGroupChild(child: IFormItem): void
    isModified(): boolean
    validate(): Promise<boolean>
    onPropValueChanged(prop: IFormPropCore): void
}

/**
 * For internal use only.
 */
export interface _IInternalFormItem {
    /**
    * For internal use only.
    */
    get _validation(): _IInternalFormGroupValidationManager
    /**
    * For internal use only.
    */
    get _hasValidation(): boolean
}

export interface IFormPropCore<T = any> {
    value: Signal<T>
    label?: string | null
    addRuleError(rule: FormPropRule, message: string): ValidationError | null
    removeRuleError(rule: FormPropRule): ValidationError | null
}

/**
 * For internal use only.
 */
export interface _IInternalFormGroupValidationManager {
    get instanceRules(): FormPropRule[]

    get hasInstanceRules(): boolean

    addInstanceRule(rule: FormPropRule): void

    removeInstanceRuleById(ruleId: string): void

    get hasRules(): boolean

    getRulesBySourceProp(sourceProp: IFormPropCore): FormPropRule[] | null

    getRulesByTargetProp(sourceProp: IFormPropCore): FormPropRule[] | null

    readonly _hasErrors: Signal<boolean>

    increaseErrorCounter(): void

    decreaseErrorCounter(): void
}

export function _getInternalValidation(group: IFormItem): _IInternalFormGroupValidationManager {
    return (group as unknown as _IInternalFormItem)._validation
}

export function _getHasInternalValidation(group: IFormItem): boolean {
    return (group as unknown as _IInternalFormItem)._hasValidation
}
