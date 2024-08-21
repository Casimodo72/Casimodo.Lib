import { Signal, computed, signal } from "@angular/core"
import { DateTime } from "luxon"

import { StringInputModel } from "./textInputModels"
import { PickerModel } from "./pickerModel"
import { FormInputModel, IFormPart, FormRulesBuilder } from "../core"

export class TypeaheadStringInputModel extends StringInputModel {
    readonly isSearchOpen = signal(false)
    readonly isSearchMatch = computed(() => {
        const items = this.picker.pickableItems()
        const searchText = this.searchValue()?.trim().toLocaleLowerCase() ?? ""

        if (!searchText) {
            return true
        }

        return items.find(x => !!x.data && x.data.toLocaleLowerCase() === searchText) !== undefined
    })

    readonly picker = new PickerModel<string>(this)
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
    }

    setSearchValues(values: string[]): this {
        this.picker.setPickValues(values)

        return this
    }
}

// TODO: Do we want/need to differentiate between nullables and non-nullables?
export class NumberInputModel extends FormInputModel<number | null> {
    constructor(parent: IFormPart, initialValue?: number | null) {
        super(parent, initialValue ?? null)
    }

    protected override _convertFromDomInputValueToData(value: any): any | null {
        if (!value) {
            return null
        }

        const numberValue = Number.parseInt(value)

        if (Number.isNaN(numberValue)) {
            return null
        }

        return numberValue
    }

    setRules(rulesBuildFn: (rulesBuilder: NumberRulesBuilder) => void): this {
        const rulesBuilder = new NumberRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class NumberRulesBuilder extends FormRulesBuilder<number | null> {
    min(minimum: number, errorMessage?: string): this {
        return this.addMinimumRuleCore(minimum, errorMessage)
    }
}

export class BooleanInputModel extends FormInputModel<boolean> {
    protected override _convertFromDomInputValueToData(value: any): any {
        if (!value) {
            return false
        }

        // TODO

        return true
    }

    setRules(rulesBuildFn: (rulesBuilder: BooleanRulesBuilder) => void): this {
        const rulesBuilder = new BooleanRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class BooleanRulesBuilder extends FormRulesBuilder<boolean> {
}
