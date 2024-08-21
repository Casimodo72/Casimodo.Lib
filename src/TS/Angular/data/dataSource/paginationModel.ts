import { signal, computed } from "@angular/core"
import { Subject } from "rxjs"

export interface IPaginationConfig {
    size?: number
    availableSizes?: number | number[]
}

export class PaginationModel {
    readonly #busyCounter = signal(0)
    readonly isBusy = computed(() => this.#busyCounter() > 0)

    protected readonly _index = signal(0)
    /** The index of the current page (zero based). */
    readonly index = this._index.asReadonly()
    /** The number of the current page (one based). */
    readonly pageNumber = computed(() => this.index() + 1)

    readonly #lastIndex = signal<number | undefined>(undefined)
    readonly lastIndex = this.#lastIndex.asReadonly()
    readonly lastPageNumber = computed(() => this.lastIndex() !== undefined ? this.lastIndex()! + 1 : undefined)

    readonly #changed = new Subject<void>()
    readonly changed = this.#changed.asObservable()

    protected readonly _size = signal(20)
    /** The page size. */
    readonly size = this._size.asReadonly()

    protected readonly _count = signal(0)
    /** The number of currently loaded data-items. */
    readonly count = this._count.asReadonly()

    protected readonly _totalCount = signal<number | undefined>(undefined)
    /** The total number of loadable data-items or undefined if unknown. */
    readonly totalCount = this._totalCount.asReadonly()

    readonly #availableSizes = signal([10, 20, 50])
    readonly availableSizes = this.#availableSizes.asReadonly()
    readonly #isSizeSelectable = signal(true)
    readonly isSizeSelectable = this.#isSizeSelectable.asReadonly()

    readonly #canMoveToFirst = signal(false)
    readonly canMoveToFirst = this.#canMoveToFirst.asReadonly()

    readonly #canMoveToPrevious = signal(false)
    readonly canMoveToPrevious = this.#canMoveToPrevious.asReadonly()

    readonly #canMoveToNext = signal(false)
    readonly canMoveToNext = this.#canMoveToNext.asReadonly()

    readonly isMoveToLastAvailable = computed(() => this.totalCount() !== undefined)
    readonly #canMoveToLast = signal(false)
    readonly canMoveToLast = this.#canMoveToLast.asReadonly()

    constructor(config?: IPaginationConfig) {
        if (config?.size) {
            this._size.set(config.size)
        }
        if (config?.availableSizes) {
            this.#availableSizes.set(
                typeof config?.availableSizes === "number"
                    ? [config?.availableSizes]
                    : config?.availableSizes
            )
        }

        // Ensure the current page-size is one of the available page-sizes.
        if (!this.availableSizes().includes(this.size())) {
            const fallbackSize = this.availableSizes()[0] ?? 5
            this._size.set(fallbackSize)
        }

        this.#updateStates()
    }

    _setLoadedCount(loadedCount: number) {
        const firstItemIndexAtCurrentPage = this.index() * this.size()
        this._count.set(firstItemIndexAtCurrentPage + Math.min(this.size(), loadedCount))

        this.#updateStates()
    }

    _setTotalCount(totalCount: number) {
        if (totalCount === this.totalCount()) return

        this._totalCount.set(totalCount)

        this.#lastIndex.set(Math.max(0, Math.ceil(totalCount / this.size()) - 1))

        if (totalCount < this.count()) {
            this.moveToFirst()
        }
        else {
            this.#updateStates()
        }
    }

    setSize(pageSize: number): boolean {
        if (pageSize < 1 || pageSize === this.size()) return false

        this._size.set(pageSize)

        this.#moveToIndexCore(0)
        this.#onChanged()

        return true
    }

    enterBusyState() {
        this.#busyCounter.update(x => x + 1)
    }

    leaveBusyState() {
        this.#busyCounter.update(x => x > 0 ? x - 1 : 0)
    }

    moveToFirst() {
        const changed = this.#moveToIndexCore(0)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToNext(): boolean {
        const changed = this.#moveToIndexCore(this.index() + 1)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToPrevious(): boolean {
        const changed = this.#moveToIndexCore(this.index() - 1)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    /** Move-to-last is only available if the totalCount was set. */
    moveToLast(): boolean {
        const totalCount = this.totalCount()
        const lastIndex = this.lastIndex()
        if (totalCount === undefined || lastIndex === undefined) return false

        const changed = this.moveToIndex(lastIndex)
        if (changed) {
            this.#onChanged()
        }

        return changed
    }

    moveToIndex(pageIndex: number): boolean {
        const changed = this.#moveToIndexCore(pageIndex)
        if (changed) {
            this.#onChanged()
        }

        return true
    }

    #onChanged() {
        this.#changed.next()
    }

    #moveToIndexCore(pageIndex: number): boolean {
        if (pageIndex < 0 || pageIndex === this.index()) {
            return false
        }

        const lastIndex = this.lastIndex()
        if (lastIndex !== undefined && pageIndex > lastIndex) {
            return false
        }

        // TODO: Restrict upper index if totalCount is known?

        this._index.set(Math.max(pageIndex, 0))
        this.#updateStates()

        return true
    }

    #updateStates() {
        const index = this.index()

        const canMoveToFirst = index > 0
        const canMoveToPrevious = index > 0

        const count = this.count()
        const totalCount = this.totalCount()
        const availableCountAtCurrentPage = this.size() * (this.index() + 1)

        const isEndReached = totalCount !== undefined
            // If total count is available then use that.
            ? count >= totalCount
            // Otherwise, if we loaded less data-items than would fill the pages
            // then we reached the end.
            : count < availableCountAtCurrentPage

        const canMoveToNext = !isEndReached
        const canMoveToLast = !isEndReached

        this.#canMoveToFirst.set(canMoveToFirst)
        this.#canMoveToPrevious.set(canMoveToPrevious)
        this.#canMoveToNext.set(canMoveToNext)
        this.#canMoveToLast.set(canMoveToLast && this.isMoveToLastAvailable())
    }
}
