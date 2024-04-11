import { computed, signal } from "@angular/core"
import { IFormPropCore, _IInternalFormGroupValidationManager } from "./core"
import { FormPropRule } from "./propRule"

export type ValidationType = "full" | "intermediate"

/** @inheritdoc */
export class _InternalPropGroupValidationManager implements _IInternalFormGroupValidationManager {
    get instanceRules(): FormPropRule[] {
        return this.#instanceRules ??= []
    }
    #instanceRules?: FormPropRule[]

    get hasInstanceRules(): boolean {
        return !!this.#instanceRules?.length
    }

    addInstanceRule(rule: FormPropRule): void {
        this.instanceRules.push(rule)
    }

    removeInstanceRuleById(ruleId: string): void {
        if (!this.#instanceRules?.length) return

        const index = this.#instanceRules.findIndex(x => x.id === ruleId)
        if (index !== -1) {
            this.#instanceRules.splice(index, 1)
        }
    }

    get hasRules(): boolean {
        return this.hasInstanceRules
    }

    getRulesBySourceProp(sourceProp: IFormPropCore): FormPropRule[] | null {
        if (!this.#instanceRules?.length) return null

        let rules: FormPropRule[] | null = null

        for (const rule of this.#instanceRules) {
            if (rule.isSourceProp(sourceProp)) {
                if (rules === null) {
                    rules = [rule]
                } else {
                    rules.push(rule)
                }
            }
        }

        return rules
    }

    getRulesByTargetProp(sourceProp: IFormPropCore): FormPropRule[] | null {
        if (!this.#instanceRules?.length) return null

        let rules: FormPropRule[] | null = null

        for (const rule of this.#instanceRules) {
            if (rule.isTargetProp(sourceProp)) {
                if (rules === null) {
                    rules = [rule]
                } else {
                    rules.push(rule)
                }
            }
        }

        return rules
    }

    readonly #errorCounter = signal(0)
    readonly _hasErrors = computed(() => this.#errorCounter() > 0)

    increaseErrorCounter(): void {
        this.#errorCounter.update(x => x++)
    }

    decreaseErrorCounter(): void {
        this.#errorCounter.update(x => x--)
    }
}
