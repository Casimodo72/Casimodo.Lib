import { Signal, signal, computed } from "@angular/core"
import { DataItemModel } from "../items"
import { ListModel } from "../lists"
import { IFormItem } from "./core"
import { FormProp } from "./prop"
import { FormPropRulesBuilder } from "./propRuleBuilder"
import { StringFormProp } from "./stringProp"

export class PickerItemModel<T = any> extends DataItemModel<T> {
    _isEmpty?: boolean
    displayProp?: string

    setDisplayProp(displayProp: string): this {
        this.displayProp = displayProp

        return this
    }

    toDisplayText() {
        if (this.displayProp) {
            return (this.data as any)[this.displayProp]
        }

        return this.data
    }
}

type PickerFilterFn<TData> = (filterValue: string, item: TData) => boolean

type PickItemFn<TPickItem> = (pickItem: TPickItem) => void

export class PickerFormProp<TData = any, TPickItem extends PickerItemModel<TData> = PickerItemModel<TData>>
    extends FormProp<TData | null> {

    static readonly EmptyItem = new PickerItemModel({ id: "47a3ec00-5a07-4c1e-98b5-01a6cbf48038" })

    static {
        this.EmptyItem._isEmpty = true
    }

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

    readonly internalSelectedItem = computed(() => {
        return this.selectedItem() ?? PickerFormProp.EmptyItem
    })

    readonly filter = new StringFormProp(this)
    readonly #filterPropName = signal<keyof TData | undefined>(undefined)
    readonly filterValue = this.filter.value

    readonly hasEmptyItem = signal(true)
    readonly #emptyText = signal("(Keine Auswahl)")
    readonly emptyText = this.#emptyText.asReadonly()

    #filterFn?: PickerFilterFn<TData>

    readonly #filterChangedSignal = signal("")

    displayProp?: string

    /** Either all items or a filtered subset if a @filterValue is set. */
    readonly pickableItems = computed<TPickItem[]>(() => {
        const _ = this.#filterChangedSignal()
        let items = this.#itemList.items()
        const filterPropName = this.#filterPropName()
        let filterValue = this.filterValue()

        if (!filterValue) {
            return items
        }

        filterValue = filterValue.toLocaleLowerCase()

        if (filterPropName) {
            items = items.filter(x => {
                const itemValue = x.data[filterPropName] ?? ""
                return typeof itemValue === "string"
                    ? itemValue.indexOf(filterValue) >= 0
                    : false
            })
        }
        else {
            const filterFn = this.#filterFn ?? PickerFormProp.#filterDefault

            items = items.filter(x => filterFn(filterValue, x.data))
        }

        return items
    })

    constructor(group: IFormItem, initialValue?: TData | null) {
        super(group, initialValue ?? null)
    }

    setEmptyText(emptyText: string) {
        this.#emptyText.set(emptyText)
    }

    setHasNullValue(hasNullValue: boolean) {
        this.hasEmptyItem.set(hasNullValue)
    }

    includesValue(value: TData): boolean {
        return this.items().find(x => x.data === value) !== undefined
    }

    setDisplayProp(displayProp: string): this {
        this.displayProp = displayProp

        return this
    }

    setFilterProp(filterProp: keyof TData): this {
        this.#filterPropName.set(filterProp)

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
        if (this.displayProp) {
            for (const item of items) {
                if (!item.displayProp) {
                    item.displayProp = this.displayProp
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
        if (this.displayProp) {
            item.displayProp = this.displayProp
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
        try {
            const result = this.setValue(selectedItem?.data ?? null)
            if (result) {
                this.#itemList.setCurrent(selectedItem)
                this._controlAdapter?.setValue(selectedItem)
            }

            return result
        }
        finally {
            this.#isSelectingItem = false
            this.#onAfterPickItemSelectedFn?.(selectedItem)
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

    override async _onDomInput(ev: InputEvent): Promise<void> {
        // TODO: REMOVE?
        //const inputEvent = ev as InputEvent
        //const textInputValue = (inputEvent.currentTarget as any)?.value ?? ""

        this.#itemList.setCurrent(null)
        // TODO: REMOVE?
        //this.#filterValue.set(textInputValue)
        this.setValue(null)
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
