import { computed, signal } from "@angular/core"
import { Subject } from "rxjs"

import { ActiveDataFilter, ActiveDataSort } from "@lib/data/utils"

import { PaginationModel } from "./paginationModel"

export abstract class DataSource<TData = any> {
    protected readonly _dataItems = signal<TData[]>([])
    readonly dataItems = this._dataItems.asReadonly()

    protected readonly _loaded = new Subject<void>()
    readonly loaded = this._loaded.asObservable()

    readonly #busyCounter = signal(0)
    readonly isBusy = computed(() => this.#busyCounter() > 0)

    enterBusyState() {
        this.#busyCounter.update(x => x + 1)
    }

    leaveBusyState() {
        this.#busyCounter.update(x => x > 0 ? x - 1 : 0)
    }

    abstract readonly paginator: PaginationModel
    abstract reset(): void
    abstract _setFilters(activeFilters: ActiveDataFilter[]): void
    abstract _setSortList(activeSortList: ActiveDataSort[]): void
    abstract load(): Promise<TData[]>
    //abstract loadSingleById(entityId: string): Promise<TData | undefined>
}
