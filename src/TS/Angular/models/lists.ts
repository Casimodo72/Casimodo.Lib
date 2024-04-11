import { signal, Signal, computed } from "@angular/core"
import { SignalHelper } from "@lib/utils"
import { IItemModel } from "./items"
import { FormItem } from "./props"

type SelectionMode = "single" | "multiple"
type SelectionState = "empty" | "some" | "all"

export interface IListSelectionModel<T extends IItemModel> {
    readonly mode: SelectionMode
    readonly items: Signal<T[]>
    readonly state: Signal<SelectionState>
    setItem(item: T, isSelected: boolean): boolean
    setAll(areSelected: boolean): boolean
    readonly isEmpty: Signal<boolean>
    readonly hasSome: Signal<boolean>
    readonly hasAll: Signal<boolean>
}

// TODO: This may not be scalable. I tried to mimic the Angular Material selection model a bit,
// but for huge amounts of selected items this may not be the best approach.
// Alternative: Don't keep a list of selected items, but just keep the selected state.
// TODO: Fix ListModel: Selection contains duplicate items.
class ListSelectionModel<T extends IItemModel> implements IListSelectionModel<T> {
    readonly #list: ListModel<T>
    readonly mode: SelectionMode = "multiple"
    readonly #items = signal<T[]>([])
    readonly items = this.#items.asReadonly()

    readonly isEmpty = computed<boolean>(
        () => this.items().length === 0)

    readonly hasSome = computed<boolean>(() => {
        const selectedCount = this.items().length
        const listCount = this.#list.items().length

        return selectedCount !== 0 && selectedCount !== listCount
    })

    readonly hasAll = computed<boolean>(
        () => this.items().length === this.#list.items().length)

    readonly state = computed<SelectionState>(() => {
        const selectedCount = this.items().length
        const listCount = this.#list.items().length

        return selectedCount === 0
            ? "empty"
            : selectedCount === listCount
                ? "all"
                : "some"
    })

    constructor(list: ListModel<T>, mode: SelectionMode) {
        this.#list = list
        this.mode = mode
    }

    /**
    * Tries to select the given items.
    * If the selection mode is "single" then all other items will be deselected.
    * @returns whether the selection was changed - either on the selected list of items of the items itself.
    */
    select(...items: T[]): boolean {
        if (!items?.length) return false

        items = items.filter(x => this.#list.items().includes(x))

        let changed = false

        if (this.mode === "single") {
            // Select the first item only.
            const item = items[0]

            if (item.canChangeSelection()) {
                if (item.setIsSelected(true)) {
                    changed = true
                }

                if (this.#items().length !== 1 || this.#items()[0] !== item) {
                    this.#items.set([item])
                    changed = true
                }
            }

            return changed
        }

        for (const item of items) {
            if (item.canChangeSelection()) {
                if (item.setIsSelected(true)) {
                    changed = true
                }

                if (SignalHelper.push(this.#items, item, true)) {
                    changed = true
                }
            }
        }

        return changed
    }

    selectAll(): boolean {
        return this.select(...this.#list.items())
    }

    /**
     * Tries to deleselect the given items.
     * @returns whether the selection was changed - either on the selected list of items of the items itself.
     */
    deselect(...items: T[]): boolean {
        if (!items?.length) return false

        items = items.filter(x => this.#list.items().includes(x))

        let changed = false

        for (const item of items) {
            if (item.canChangeSelection()) {
                if (item.setIsSelected(false)) {
                    changed = true
                }

                if (SignalHelper.remove(this.#items, item)) {
                    changed = true
                }
            }
        }

        return changed
    }

    deselectAll(): boolean {
        return this.deselect(...this.#list.items())
    }

    clear(): boolean {
        return this.deselectAll()
    }

    /**
     * Tries to set the selection state of the given item.
     * If the selection mode is "single" then all other items will be deselected.
     * @param item
     * @param isSelected
     * @returns whether the selection was changed on any items involved.
     */
    setItem(item: T, isSelected: boolean): boolean {
        return isSelected
            ? this.select(item)
            : this.deselect(item)
    }

    setAll(areSelected: boolean): boolean {
        return areSelected
            ? this.selectAll()
            : this.deselectAll()
    }
}

type ListModelOptions = {
    selectionMode?: SelectionMode
}

export class ListModel<T extends IItemModel> extends FormItem {
    protected readonly _items = signal<T[]>([])
    readonly items = this._items.asReadonly()
    readonly count = computed(() => this._items().length)
    protected readonly _current = signal<T | null>(null)
    readonly current = this._current.asReadonly()
    readonly #selection: ListSelectionModel<T>
    readonly isEmpty = computed(() => this._items().length === 0)

    constructor(options?: ListModelOptions) {
        super()

        // TODO: Do we want to allow for dynamically changing the selection mode?
        this.#selection = new ListSelectionModel<T>(this, options?.selectionMode ?? "single")
    }

    get selection(): IListSelectionModel<T> {
        return this.#selection
    }

    clear() {
        this._items.set([])
        this._current.set(null)
        this.#selection.clear()
    }

    findPrevious(item: T): T | undefined {
        const index = this._items().indexOf(item)
        if (index <= 0) return undefined

        return this._items()[index - 1]
    }

    findNext(item: T): T | undefined {
        const index = this._items().indexOf(item)
        if (index < 0) return undefined

        return this._items()[index + 1]
    }

    last(): T | undefined {
        const items = this._items()

        return items.length > 0
            ? items[items.length - 1]
            : undefined
    }

    contains(item: T) {
        return this.items().find(x => x === item)
    }

    findById(id: string): T | undefined {
        return this.items().find(x => x.id === id)
    }

    setCurrent(item: T | null | undefined) {
        item ??= null

        if (this._current() === item) return

        for (const item2 of this._items()) {
            item2.isCurrent.set(false)
        }

        if (item) {
            item.isCurrent.set(true)
        }

        this._current.set(item)
    }

    setCurrentByIndex(index: number) {
        const item = this._items()[index]
        if (!item) return

        this.setCurrent(item)
    }

    setCurrentById(id: string) {
        const item = this._items().find(x => x.id === id)

        this.setCurrent(item)
    }

    setItems(items: T[]) {
        this.clear()
        this._items.set([...items])
    }

    insertFirst(item: T) {
        SignalHelper.insertFirst(this._items, item)
    }

    add(item: T) {
        this.addCore(item)
    }

    addRange(items: T[]) {
        this.addRangeCore(items)
    }

    protected addCore(item: T) {
        SignalHelper.push(this._items, item)
    }

    insertAfter(contextItem: T, item: T): boolean {
        const items = this._items()
        const contextIndex = items.indexOf(contextItem)
        // TODO: Should we throw errors or just return false?
        if (contextIndex === -1) return false

        if (items.includes(item)) return false

        if (contextIndex === items.length - 1) {
            SignalHelper.push(this._items, item)
        }
        else {
            SignalHelper.splice(this._items, contextIndex + 1, 0, item)
        }

        return true
    }

    insertBefore(contextItem: T, item: T): boolean {
        const items = this._items()
        const contextIndex = items.indexOf(contextItem)
        // TODO: Should we throw errors or just return false?
        if (contextIndex === -1) return false

        if (items.includes(item)) return false

        SignalHelper.splice(this._items, contextIndex, 0, item)

        return true
    }

    protected addRangeCore(items: T[]) {
        if (!items?.length) return

        SignalHelper.pushRange(this._items, ...items)
    }

    remove(item: T): boolean {
        return this.removeCore(item)
    }

    removeAndMoveCurrent(item: T, direction: "next" | "previous" = "next"): boolean {
        const index = this._items().indexOf(item)
        if (index === -1) return false

        if (this._current() === item) {
            this.setCurrentToAdjacentItem(index, direction)
        }

        return this.removeCore(item)
    }

    isLast(item: T): boolean {
        const items = this.items()
        const index = items.indexOf(item)
        return index === items.length - 1
    }


    setCurrentToAdjacentItem(contextIndex: number, direction: "next" | "previous") {
        if (contextIndex === -1) return

        // Deselect if no adjacent item exists.
        if (this._items().length === 1) {
            this._current.set(null)

            return
        }

        if (direction === "next") {
            if (contextIndex < this._items().length - 1) {
                this.setCurrentByIndex(contextIndex + 1)
            } else {
                direction = "previous"
            }
        }

        if (direction === "previous") {
            if (contextIndex > 0) {
                this.setCurrentByIndex(contextIndex - 1)
            }
        }
    }

    protected removeCore(item: T): boolean {
        const wasRemoved = SignalHelper.remove(this._items, item)

        if (wasRemoved && this._current() === item) {
            this._current.set(null)
        }

        return wasRemoved
    }

    replace(oldItem: T, newItem: T): boolean {
        return this.replaceCore(oldItem, newItem)
    }

    protected replaceCore(oldItem: T, newItem: T): boolean {
        const wasReplaced = SignalHelper.replace(this._items, oldItem, newItem)

        if (wasReplaced && this._current() === oldItem) {
            this._current.set(newItem)
        }

        return wasReplaced
    }
}
