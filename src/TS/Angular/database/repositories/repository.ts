import { IDatabaseCore } from "../database"
import Dexie from "dexie"

export abstract class RepositoryBase {
    tableName!: string
    tableKeys!: string

    _setTable(_table: any) {
        // NOOP
    }

    async _deleteTable(): Promise<void> {
        // NOOP
    }
}

export abstract class Repository<TData = any, TKey = any> extends RepositoryBase {
    dbcore!: IDatabaseCore

    table!: Dexie.Table<TData, TKey>
    tablePrimaryKey!: string

    entityTypeName!: string
    entityTypeId!: string
    displayName!: string

    hasCompanyScope = false
    hasUserScope = false

    hasEntityState = false
    hasDependents = false

    isCached = false
    #cachedItems: TData[] | undefined

    override _setTable(table: any) {
        this.table = table
    }

    _setCache(items: Partial<TData>[]) {
        this.#cachedItems = items as TData[]
    }

    async clearCache(): Promise<void> {
        if (!this.isCached || this.#cachedItems?.length) return

        this.#cachedItems = undefined
    }
}
