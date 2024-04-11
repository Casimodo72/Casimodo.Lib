import { Signal, WritableSignal, signal } from "@angular/core"
import { FormItem } from "./props/prop"

export interface IItemModel {
    readonly id: string
    readonly isSelected: Signal<boolean>
    readonly canChangeSelection: Signal<boolean>
    /**
     * Tries to set the selection state.
     * @returns whether the selection state changed.
     */
    setIsSelected(isSelected: boolean): boolean
    readonly isCurrent: WritableSignal<boolean>
}

export abstract class ItemModel extends FormItem implements IItemModel {
    readonly id: string
    protected readonly _isSelected = signal(false)
    readonly isSelected = this._isSelected.asReadonly()
    readonly canChangeSelection = signal(true)
    readonly isCurrent = signal(false)

    constructor(id?: string) {
        super()

        this.id = id ?? `item${crypto.randomUUID()}`
    }

    /**
     * @inheritdoc
     */
    setIsSelected(isSelected: boolean): boolean {
        if (!this.canChangeSelection() || this._isSelected() === isSelected) {
            return false
        }

        this._isSelected.set(isSelected)

        return true
    }
}

export interface IDataItemModel<T = any> extends IItemModel {
    readonly data: T
}

export abstract class DataItemModel<T = any> extends ItemModel
    implements IDataItemModel<T>
{
    readonly data: T

    constructor(data: T) {
        super((data as any).Id ?? (data as any).id)

        this.data = data
    }
}

export interface IMutableDataItemModel<T = any> extends IItemModel {
    readonly data: Signal<T>
    mutateData(delta: Partial<T>): void
}

export class MutableDataItemModel<T = any> extends ItemModel
    implements IMutableDataItemModel<T>
{
    readonly #data: WritableSignal<T>
    readonly data: Signal<T>

    constructor(data: T) {
        super((data as any).Id ?? (data as any).id)

        this.#data = signal(data)
        this.data = this.#data.asReadonly()
    }

    mutateData(delta: Partial<T>): void {
        const data = Object.assign({}, this.#data(), delta)
        this.#data.set(data)
    }
}
