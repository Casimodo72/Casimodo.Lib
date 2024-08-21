import { Injector, signal } from "@angular/core"

import type { IEntitySelectorService } from "@lib/ui/lookup/entityLookupService"
import { PropPathSelection, PropPath } from "@lib/data/utils"

import { IFormPart, FormInputModel, FormRulesBuilder } from "../core"
import { _standardEntityIdProp } from "./entityPrimitives"

interface ISelectFnContext {
    readonly input: EntityLookupModel
    readonly injector: Injector
}

type SelectFn<TData> = (context: ISelectFnContext) => Promise<Partial<TData> | undefined>

interface IDetailsProviderFnContext {
    readonly injector: Injector
    readonly id: string
}

type DetailsProviderFn<TData> = (context: IDetailsProviderFnContext) => Promise<Partial<TData> | undefined>

export class EntityLookupModel<TData = any> extends FormInputModel<Partial<TData> | null> {
    readonly #details = signal<Partial<TData> | null>(null)
    /**
     * Details of the picked object.
     * These details can be provided by setting a details providing function with setDetails(...).
     * If no such detail provider was set then the value() itself is used.
     */
    readonly details = this.#details.asReadonly()

    constructor(parent: IFormPart) {
        super(parent, null)

        this._valueIdProp = _standardEntityIdProp
    }

    /** This method is async because it will load pick-values on demand if applicable. */
    async setValueById(valueId: string) {
        this.setValue(null)

        if (!this._valueIdProp) return

        const detailsEntity = await this.#loadDetails(valueId)
        if (detailsEntity) {
            this.setValue(detailsEntity)
        }
    }

    protected override _onValueChanged(): void {
        super._onValueChanged()

        this.#loadDetails()
    }

    async #loadDetails(valueId?: string): Promise<Partial<TData> | null> {
        const value = this.value()
        const detailsEntityId = valueId
            ? valueId
            : value
                ? this._valueIdProp?.getValue(value)
                : undefined
        let detailsEntity: Partial<TData> | null = value

        if (detailsEntityId && this.#detailsProviderFn) {
            const injector = this.#getRequiredInjector()
            detailsEntity = await this.#detailsProviderFn(
                {
                    id: detailsEntityId,
                    injector: injector
                }) ?? null
        }

        this.#details.set(detailsEntity)

        return detailsEntity
    }

    setDetails(detailsProviderFn: DetailsProviderFn<TData>): this {
        this.#detailsProviderFn = detailsProviderFn

        return this
    }
    #detailsProviderFn?: DetailsProviderFn<TData>

    async lookup() {
        let lookupResult: Partial<TData> | undefined = undefined

        if (this.#selectFn) {
            lookupResult = await this.#selectFn({
                input: this,
                injector: this.#getRequiredInjector()
            })
        }

        if (!lookupResult) return

        this.setValue(lookupResult)
    }

    #getRequiredInjector() {
        if (!this._injector) throw new Error("Required injector not assigned for custom lookup.")

        return this._injector
    }

    setSelector(selectFn: SelectFn<TData>): this {
        this.#selectFn = selectFn

        return this
    }
    #selectFn?: SelectFn<TData>

    protected override _convertFromDomInputValueToData(_value: any): any | null {
        return null
    }

    get valueIdProp() {
        return this._valueIdProp
    }
    setValueIdProp(valueProp: PropPathSelection<TData>): this {
        if (valueProp instanceof PropPath) {
            this._valueIdProp = valueProp
        }
        else {
            this._valueIdProp = PropPath.fromSelection<TData>(valueProp)
        }

        return this
    }
    protected _valueIdProp?: PropPath

    setRules(rulesBuildFn: (rulesBuilder: LookupRulesBuilder) => void): this {
        const rulesBuilder = new LookupRulesBuilder(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }
}

class LookupRulesBuilder extends FormRulesBuilder<any | null> {
}
