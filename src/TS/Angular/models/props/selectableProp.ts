import { Signal, signal, computed } from "@angular/core"
import { DataItemModel } from "../items"
import { ListModel } from "../lists"
import { IFormItem } from "./core"
import { FormProp } from "./prop"
import { FormPropRulesBuilder } from "./propRuleBuilder"
import { StringFormProp } from "./stringProp"
import { DataPath, DataPathSelection } from "@lib/data-utils"

type StringKeys<T> = Extract<keyof T, string>

export class PickerItemModel<T = any> extends DataItemModel<T> {
    _isEmpty?: boolean
    _displayPath?: DataPath
    _displayFn?: (data: any) => string

    toDisplayText(): string {
        if (this._displayPath) {
            return this._displayPath.getValueFrom(this.data)?.toString() ?? ""
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

export class PickerFormProp<TData = any, TPickItem extends PickerItemModel<TData> = PickerItemModel<TData>>
    extends FormProp<TData | null> {

    // TODO: REMOVE? Not used
    // static readonly EmptyItem = new PickerItemModel({ id: "47a3ec00-5a07-4c1e-98b5-01a6cbf48038" })
    // static {
    //     this.EmptyItem._isEmpty = true
    // }

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

    readonly filter = new StringFormProp(this)
    readonly #filterPath = signal<DataPath | undefined>(undefined)
    readonly filterValue = this.filter.value

    readonly hasEmptyItem = signal(false)
    readonly #emptyText = signal("(Keine Auswahl)")
    readonly emptyText = this.#emptyText.asReadonly()

    // TODO: Since Angular material can't virtualize the displayed items,
    // we need to restrict the number of displayable items for now.
    // Think about implementing a pagination strategy in the picker UI.
    readonly maxPickableItemCount = signal(50)

    #filterFn?: PickerFilterFn<TData>

    readonly #filterChangedSignal = signal("")

    #displayFn?: (data: TData) => string
    #displayPath = signal<DataPath | undefined>(undefined)

    /** Either all items or a filtered subset if a @filterValue is set. */
    readonly #filteredItems = computed<TPickItem[]>(() => {
        const _ = this.#filterChangedSignal()
        let items = this.#itemList.items()

        const filterValue = this.filterValue()?.toLocaleLowerCase()

        if (filterValue) {
            const filterPath = this.#filterPath() ?? this.#displayPath()
            if (filterPath) {
                const filterRegex = new RegExp(filterValue, "i")

                items = items.filter(x => {
                    // TODO: Do we want to support numbers?
                    const itemValue = filterPath.getValueFrom(x.data)?.toString() ?? ""
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
                const filterFn = this.#filterFn ?? PickerFormProp.#filterDefault

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

    constructor(group: IFormItem, initialValue?: TData | null) {
        super(group, initialValue ?? null)
    }

    setEmptyText(emptyText: string): this {
        this.#emptyText.set(emptyText)

        return this
    }

    setHasNullValue(hasNullValue: boolean): this {
        this.hasEmptyItem.set(hasNullValue)

        return this
    }

    includesValue(value: TData): boolean {
        return this.items().find(x => x.data === value) !== undefined
    }

    setDisplayProp(displayProp: DataPathSelection<TData> | undefined): this {
        if (displayProp) {
            this.#displayPath.set(DataPath.createFromSelection<TData>(displayProp))
        }
        else {
            this.#displayPath.set(undefined)
        }

        return this
    }

    setDisplayFn(displayFn: ((data: TData) => string) | undefined): this {
        this.#displayFn = displayFn

        return this
    }

    setFilterProp(filterProp: DataPathSelection<TData> | undefined): this {
        if (filterProp) {
            this.#filterPath.set(DataPath.createFromSelection<TData>(filterProp))
        }
        else {
            this.#filterPath.set(undefined)
        }

        return this
    }

    setFilterValue(filterValue: string | null) {
        this.filter.setValue(filterValue ?? "")
    }

    setFilterFunction(filterFn: PickerFilterFn<TData>): this {
        this.#filterFn = filterFn

        return this
    }

    updateFilter() {
        this.#filterChangedSignal.set("")
    }

    setPickValues(values: TData[]): this {
        this.#itemList.setItems(values.map(x => this.#createItem(x)))

        return this
    }

    setPickItems(items: TPickItem[]): this {
        const displayPath = this.#displayPath()
        const displayFn = this.#displayFn

        if (displayPath || displayFn) {
            for (const item of items) {
                if (displayPath && !item._displayPath) {
                    item._displayPath = displayPath
                }

                if (displayFn && !item._displayFn) {
                    item._displayFn = displayFn
                }
            }
        }

        this.#itemList.setItems(items)

        return this
    }

    addPickValue(value: TData): this {
        this.#itemList.add(this.#createItem(value))

        return this
    }

    #createItem(value: TData): TPickItem {
        const item = this.#itemFactory(value)
        if (this.#displayPath()) {
            item._displayPath = this.#displayPath()
        }
        if (this.#displayFn) {
            item._displayFn = this.#displayFn
        }

        return item
    }

    #itemFactory: ((value: TData) => TPickItem) = (value: TData) => new PickerItemModel<TData>(value) as TPickItem

    setItemFactory(itemFactory: (value: TData) => TPickItem): this {
        this.#itemFactory = itemFactory

        return this
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
    setSelectedValue(value: TData | null): boolean {
        return this.setValue(value)
    }

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

    setRules(rulesBuildFn: (rulesBuilder: PickerFormPropRulesBuilder<TData>) => void): this {
        const rulesBuilder = new PickerFormPropRulesBuilder<TData>(this.parent, this)
        rulesBuildFn(rulesBuilder)

        return this
    }

    protected override convertFromDomInputValueToData(value: any): any {
        return value ?? ""
    }
}

export class PickerFormPropRulesBuilder<T> extends FormPropRulesBuilder<T | null> {
}
