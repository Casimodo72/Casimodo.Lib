import { IRepositoryDescriptor } from "../data"
import { Repository } from "./repository"

export abstract class DataRepository<TData extends { Id?: string | null } = any> extends Repository<TData, string> {
    static readonly ID_PROP = "Id"
    #cachedItems: TData[] | undefined

    constructor(descriptor: IRepositoryDescriptor) {
        super()

        this.tableName = descriptor.tableName
        this.tableKeys = descriptor.keys
        this.tablePrimaryKey = descriptor.keys.split(",").map(x => x.trim())[0]
        this.entityTypeName = descriptor.entityTypeName
        this.entityTypeId = descriptor.entityTypeId
        this.displayName = descriptor.displayName
        this.isCached = descriptor.isCached === true
    }

    async putRangeOnDownload(items: TData[]): Promise<void> {
        if (!items?.length) return

        for (const item of items) {
            await this.putOnDownload(item)
        }
    }

    async putOnDownload(entity: TData): Promise<void> {
        await this.put(entity)
    }

    async put(item: Partial<TData>): Promise<Partial<TData>> {
        if (!item) return item

        await this.table.put(item as TData)

        return item
    }

    async get(id: string): Promise<TData> {
        const item = await this.table.get(id)
        if (!item) {
            throw new Error(`Object of type '${this.entityTypeName ?? "unknown"}' not found in local database (by object-ID: '${id}').`)
        }

        return item
    }

    async find(id: string | null | undefined): Promise<TData | undefined> {
        if (!id) return undefined

        return await this.table.get(id)
    }

    async items(): Promise<TData[]> {
        if (this.isCached) {
            if (!this.#cachedItems?.length) {
                this.#cachedItems = await this.table.toArray()
            }

            return this.#cachedItems
        } else {
            return await this.table.toArray()
        }
    }

    async itemsById(ids: string[]): Promise<TData[]> {
        if (this.isCached) {
            const items = await this.items()

            return items.filter(x => ids.includes(x.Id!))
        } else {
            const items = await this.table.bulkGet(ids)

            return items.filter(x => x !== undefined) as TData[]
        }
    }

    /**
     * This does not take user scopes into account. It deletes every given entry.
     */
    async delete(id: string): Promise<void> {
        if (!id) return

        await this._deleteCore(false, [id])
    }

    /**
     * This does not take user scopes into account. It deletes every given entry.
     */
    async deleteRange(ids: string[]): Promise<void> {
        if (!ids?.length) return

        await this._deleteCore(false, ids)
    }

    /**
     * Performs a cascading delete of all data.
     * WARNING: This does not take company or user scopes into account. It deletes everything.
     */
    async deleteAll(): Promise<void> {
        await this._deleteCore(true)
    }

    /**
     * Performs a cascading delete of all data.
     * WARNING: This does not take company or user scopes into account. It deletes everything.
     */
    protected async cascadeDelete(_item: TData): Promise<void> {
        return Promise.resolve()
    }

    /**
     * Performs a cascading delete of all data.
     * WARNING: This does not take company or user scopes into account. It deletes everything.
     */
    protected async _deleteCore(all: boolean, ids?: string[]): Promise<void> {
        if (!all && !ids?.length) return

        const effectiveIds: string[] | undefined = all
            ? await this.table.toCollection().primaryKeys()
            : ids!

        await this.table.bulkDelete(effectiveIds)
    }

    override async _deleteTable() {
        await this.table?.clear()
    }
}
