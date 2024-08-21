import { signal } from "@angular/core"

import {
    FormInputModel, FormRulesBuilder, FormRule, FormRuleDefinition, IFormPart,
    _getInternalValidation
} from "../core"

export class StringInputModel extends FormInputModel<string> {
    #isTrimming = false

    constructor(group: IFormPart, initialValue?: string | null) {
        super(group, initialValue ?? "")
    }

    override setValue(value: string, validate?: boolean): boolean {
        return super.setValue(value ?? "", validate)
    }

    protected override _convertFromDomInputValueToData(value: any): any {
        return value ?? "" as string
    }

    protected override _normalizeCore(): void {
        // TODO: The validation errors after trimming do not end up in the UI :-(
        this.applyTrimming()
    }

    applyTrimming() {
        if (this.#isTrimming) {
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

    setRules(rulesBuildFn: (rulesBuilder: StringRulesBuilder) => void): this {
        const rulesBuilder = new StringRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

export class StringRulesBuilder extends FormRulesBuilder<string> {
    min(minimum: number, errorMessage?: string): this {
        return this.addMinimumRuleCore(minimum, errorMessage)
    }

    /**
     * Adds a well-known rule definition.
     * @param ruleDefinition
     * @returns
     */
    rule(ruleDefinition: FormRuleDefinition): this {
        const rule = new FormRule({
            id: ruleDefinition.id,
            target: this.target,
            validate: ruleDefinition.validate,
            validateAsync: ruleDefinition.validateAsync
        })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }
}

export class TextAreaInputModel extends StringInputModel {
    readonly #rowCount = signal(2)
    readonly rowCount = this.#rowCount.asReadonly()

    setRowCount(rowCount: number): this {
        this.#rowCount.set(rowCount)

        if (rowCount > this.maxRowCount()) {
            this.#maxRowCount.set(rowCount)
        }

        return this
    }

    readonly #maxRowCount = signal(10)
    readonly maxRowCount = this.#maxRowCount.asReadonly()

    setMaxRowCount(maxRowCount: number): this {
        this.#maxRowCount.set(maxRowCount)

        return this
    }
}
