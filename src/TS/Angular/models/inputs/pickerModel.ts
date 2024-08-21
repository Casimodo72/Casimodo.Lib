import { Signal, signal, computed, Injector } from "@angular/core"

import { PropPath, PropPathSelection } from "@lib/data/utils"

import { FormInputModel, FormRulesBuilder, IFormPart, DataItemModel, ListModel } from "../core"
import { StringInputModel } from "./textInputModels"

export class PickerItemModel<T = any> extends DataItemModel<T> {
    _isEmpty?: boolean
    _displayProp?: PropPath
    _displayFn?: (data: any) => string

    toDisplayText(): string {
        if (this._displayProp) {
            return this._displayProp.getValue(this.data)?.toString() ?? ""
        }
        else if (this._displayFn) {
            return this._displayFn(this.data)
        }
        else {
            return this.data?.toString() ?? ""
        }
    }
}

type PickerFilterFn<TData> = (filterValue: string, item: TData) => boolean

type PickItemFn<TPickItem> = (pickItem: TPickItem) => void

interface IPickValuesProviderFnContext {
    readonly injector: Injector
}

type PickValuesProviderFn<TEntity> = (context: IPickValuesProviderFnContext) => Promise<TEntity[]>

export class PickerModel<TData = any, TPickItem extends PickerItemModel<TData> = PickerItemModel<TData>>
    extends FormInputModel<TData | null> {

    /** @filterValue will always be non empty here. */
    static #filterDefault(filterValue: string, data: any): boolean {
        if (typeof data === "string") {
            if (!data) return false

            return data.toLocaleLowerCase().indexOf(filterValue) !== -1
        }

        return false
    }
    readonly #itemList = new ListModel<TPickItem>()
    readonly items: Signal<TPickItem[]> = this.#itemList.items
    readonly selectedItem: Signal<TPickItem | null> = this.#itemList.current

    readonly filter = new StringInputModel(this)
    readonly #filterPath = signal<PropPath | undefined>(undefined)
    readonly filterValue = this.filter.value
    #isClearFilterValueOnFocusOutEnabled = false

    readonly hasNullValue = signal(false)
    // readonly #emptyText = signal("(Keine Auswahl)")
    // readonly emptyText = this.#emptyText.asReadonly()

    // TODO: Since Angular material can't virtualize the displayed items,
    // we need to restrict the number of displayable items for now.
    // Think about implementing a pagination strategy in the picker UI.
    readonly maxPickableItemCount = signal(50)

    #filterFn?: PickerFilterFn<TData>

    readonly #filterChangedSignal = signal("")

    #displayFn?: (data: TData) => string
    #displayProp = signal<PropPath | undefined>(undefined)

    /** Either all items or a filtered subset if a @filterValue is set. */
    readonly #filteredItems = computed<TPickItem[]>(() => {
        const _ = this.#filterChangedSignal()
        let items = this.#itemList.items()

        const filterValue = this.filterValue()?.toLocaleLowerCase()

        if (filterValue) {
            const filterPath = this.#filterPath() ?? this.#displayProp()
            if (filterPath) {
                const filterRegex = new RegExp(filterValue, "i")

                items = items.filter(x => {
                    // TODO: Do we want to support numbers?
                    const itemValue = filterPath.getValue(x.data)?.toString() ?? ""
                    if (typeof itemValue !== "string") {
                        return false
                    }

                    //  itemValue.toLocaleLowerCase().indexOf(filterValue) >= 0

                    const result = itemValue.search(filterRegex) !== -1

                    return result
                })
            }
            else {
                // Use filter function.
                const filterFn = this.#filterFn ?? PickerModel.#filterDefault

                items = items.filter(x => filterFn(filterValue, x.data))
            }
        }

        return items
    })

    /**
     * Either all items or a filtered subset if a @filterValue is set.
     * Restricted by maxPickableItemCount.
    */
    readonly pickableItems = computed<TPickItem[]>(() => {
        // TODO: Had to separate #filteredItems and pickableItems
        // because we can't set isMaxPickableItemCountExceeded inside
        // the compute function. I.e. we need two array and compare them in
        // the isMaxPickableItemCountExceeded compute function :-()
        let items = this.#filteredItems()
        if (items.length > this.maxPickableItemCount()) {
            items = items.slice(0, this.maxPickableItemCount() - 1)
        }

        return items
    })

    readonly isMaxPickableItemCountExceeded = computed(() => {
        return this.#filteredItems().length > this.pickableItems().length
    })

    constructor(group: IFormPart, initialValue?: TData | null) {
        super(group, initialValue ?? null)
    }

    setHasNullValue(hasNullValue: boolean): this {
        this.hasNullValue.set(hasNullValue)

        return this
    }

    includesValue(value: TData): boolean {
        return this.items().find(x => x.data === value) !== undefined
    }

    get valueIdProp() {
        return this._valueIdProp
    }
    setValueIdProp(valueProp: PropPath | PropPathSelection<TData>): this {
        if (valueProp instanceof PropPath) {
            this._valueIdProp = valueProp
        }
        else {
            this._valueIdProp = PropPath.fromSelection<TData>(valueProp)
        }

        return this
    }
    protected _valueIdProp?: PropPath

    /** This method is async because it will load pick-values on demand if applicable. */
    async setValueById(valueId: string) {
        this.setValue(null)

        if (!this._valueIdProp) return

        if (this._getIsPickValuesLoadingPending()) {
            await this._loadPickValues()
        }

        const matchingPickItem = this.#itemList.items().find(pickItem => this._valueIdProp?.getValue(pickItem.data) === valueId)
        if (matchingPickItem) {
            this.selectItem(matchingPickItem)
        }
    }

    setDisplayProp(displayProp: PropPath | PropPathSelection<TData> | undefined): this {
        if (displayProp) {
            if (displayProp instanceof PropPath) {
                this.#displayProp.set(displayProp)
            }
            else {
                this.#displayProp.set(PropPath.fromSelection<TData>(displayProp))
            }
        }
        else {
            this.#displayProp.set(undefined)
        }

        return this
    }

    setDisplayFn(displayFn: ((data: TData) => string) | undefined): this {
        this.#displayFn = displayFn

        return this
    }

    setFilterProp(filterProp: PropPath | PropPathSelection<TData> | undefined): this {
        if (filterProp) {
            if (filterProp instanceof PropPath) {
                this.#filterPath.set(filterProp)
            }
            else {
                this.#filterPath.set(PropPath.fromSelection<TData>(filterProp))
            }
        }
        else {
            this.#filterPath.set(undefined)
        }

        return this
    }

    setFilterValue(filterValue: string | null) {
        this.filter.setValue(filterValue ?? "")
    }

    setIsClearFilterValueOnFocusOutEnabled(enabled: boolean) {
        this.#isClearFilterValueOnFocusOutEnabled = enabled
    }

    override async _onDomInputFocusOut(ev: FocusEvent): Promise<void> {
        await super._onDomInputFocusOut(ev)

        if (this.#isClearFilterValueOnFocusOutEnabled) {
            this.setFilterValue(null)
        }
    }

    setFilterFunction(filterFn: PickerFilterFn<TData>): this {
        this.#filterFn = filterFn

        return this
    }

    updateFilter() {
        this.#filterChangedSignal.set("")
    }

    #pickValuesProviderFn?: PickValuesProviderFn<TData>

    setPickValues(values: TData[] | PickValuesProviderFn<TData>, options?: { initialIndex?: number }): this {
        this.selectItem(null)
        this.#itemList.clear()
        this.#arePickValuesLoaded = false

        if (typeof values === "function") {
            this.#pickValuesProviderFn = values
        }
        else {
            this.#itemList.setItems(values.map(x => this.#createItem(x)))

            const initialIndex = options?.initialIndex
            if (initialIndex !== undefined &&
                initialIndex >= 0 &&
                initialIndex < this.pickableItems().length
            ) {
                this.selectItem(this.pickableItems()[initialIndex] ?? null)
            }
        }

        return this
    }

    setPickItems(items: TPickItem[]): this {
        const displayPath = this.#displayProp()
        const displayFn = this.#displayFn

        if (displayPath || displayFn) {
            for (const item of items) {
                if (displayPath && !item._displayProp) {
                    item._displayProp = displayPath
                }

                if (displayFn && !item._displayFn) {
                    item._displayFn = displayFn
                }
            }
        }

        this.#itemList.setItems(items)

        return this
    }

    insertFirstPickValue(value: TData): this {
        this.#itemList.insertFirst(this.#createItem(value))

        return this
    }

    addPickValue(value: TData): this {
        this.#itemList.add(this.#createItem(value))

        return this
    }

    addPickValueRange(values: TData[]): this {
        this.#itemList.addRange(values.map(x => this.#createItem(x)))

        return this
    }

    #createItem(value: TData): TPickItem {
        const item = this.#itemFactory
            ? this.#itemFactory(value)
            : new PickerItemModel<TData>(value) as TPickItem

        if (this.#displayProp()) {
            item._displayProp = this.#displayProp()
        }
        if (this.#displayFn) {
            item._displayFn = this.#displayFn
        }

        return item
    }

    #itemFactory?: ((value: TData) => TPickItem)

    setItemFactory(itemFactory: (value: TData) => TPickItem): this {
        this.#itemFactory = itemFactory

        return this
    }

    _getIsPickValuesLoadingPending(): boolean {
        return !this.#arePickValuesLoaded && !!this.#pickValuesProviderFn && !!this._injector
    }

    // TODO: Clear this value if the pick-item list is cleared/emptied.
    #arePickValuesLoaded?: boolean

    async _loadPickValues(): Promise<void> {
        if (this.#arePickValuesLoaded) return

        if (this.#pickValuesProviderFn && this._injector) {
            const pickValues = await this.#pickValuesProviderFn({ injector: this._injector })
            if (pickValues) {
                this.setPickValues(pickValues)
            }
        }

        this.#arePickValuesLoaded = true
    }

    #isSelectingItem = false

    selectItem(selectedItem: TPickItem | null): boolean {
        if (this.#isSelectingItem) return false

        this.#isSelectingItem = true
        let result = false
        try {
            result = this.setValue(selectedItem?.data ?? null)
            if (result) {
                this.#itemList.setCurrent(selectedItem)
                this._controlAdapter?.setValue(selectedItem)
            }

            return result
        }
        finally {
            this.#isSelectingItem = false
            if (result) {
                this.#onAfterPickItemSelectedFn?.(selectedItem)
            }
        }
    }

    #onAfterPickItemSelectedFn?: PickItemFn<TPickItem | null>

    setOnAfterItemSelected(afterPickItemSelectedFn: PickItemFn<TPickItem | null>): this {
        this.#onAfterPickItemSelectedFn = afterPickItemSelectedFn

        return this
    }

    // TODO: REMOVE?
    // setSelectedValue(value: TData | null): boolean {
    //     return this.setValue(value)
    // }

    override setValue(value: TData | null): boolean {
        if (!this.#isSelectingItem) {

            if (value == null) {
                return this.selectItem(null)
            }
            else {
                const item = this.items().find(x => x.data === value)

                return item
                    ? this.selectItem(item)
                    : false
            }
        }

        if (!super.setValue(value, false)) {
            return false
        }

        // TODO: BUG: This has to run bevore validation in super.setValue.
        // TODO: Remove. The filter has now its own model.
        // if (value) {

        //     let handled = false

        //     const filterPropName = this.#filterPropName()
        //     if (filterPropName) {
        //         const filterValue = this.#ilterValue()
        //         const selectedFilterValue = value[filterPropName]

        //         if (typeof selectedFilterValue === "string" && selectedFilterValue !== filterValue) {
        //             this.#filterValue.set(selectedFilterValue)
        //             handled = true
        //         }
        //     }

        //     if (!handled) {
        //         this.#filterValue.set(value.toString())
        //     }
        // }

        return true
    }

    override validate(): Promise<boolean> {
        return super.validate()
    }

    override async _onDomInput(_ev: InputEvent): Promise<void> {
        // An input event is raised when the user types into a typeahead input.
        this.selectItem(null)
    }

    setRules(rulesBuildFn: (rulesBuilder: PickerRulesBuilder<TData>) => void): this {
        const rulesBuilder = new PickerRulesBuilder<TData>(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }

    protected override _convertFromDomInputValueToData(value: any): any {
        return value ?? ""
    }
}

export class PickerRulesBuilder<T> extends FormRulesBuilder<T | null> {
}
