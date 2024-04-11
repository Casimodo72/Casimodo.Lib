import { isEqual } from "lodash-es"

import { IEntityCore } from "@lib/data"

import { DataRepository } from "./dataRepository"
import { DeltaChangeOptions, createEntityStateDeltaOnDownload } from "../data"
import { EntityWebRepository } from "../web"

export class EntityRepository<TEntity extends Partial<IEntityCore>> extends DataRepository<TEntity> {
    protected _remote?: EntityWebRepository<Partial<IEntityCore>>

    override async putOnDownload(entity: TEntity): Promise<void> {
        await this.put(entity)

        if (this.hasEntityState) {
            await this.dbcore.entityStates.modify(entity.Id!, this, async (changes, _currentState) => {
                changes.delta = createEntityStateDeltaOnDownload(entity as IEntityCore)
            })
        }
    }

    /**
     * Used for adding of new items which are intended to be sent
     * to the server at some point.
     * NOTE: Performs a PUT web API operation if applicable.
     */
    async addNew(item: TEntity): Promise<TEntity> {
        if (!item) return item;

        (item as any)._isSyncPending = true

        await this.table.put(item)

        await this.dbcore.entityStates.markAsDirty(item.Id!, item.ModifiedOn!, this)

        if (this._remote) {
            await this._remote.trySend(item)
        }

        return item
    }

    async putRange(items: TEntity[]): Promise<void> {
        if (!items?.length) return

        for (const item of items) {
            await this.put(item)
        }
    }

    protected async processEntityDeltaChanges(obj: Partial<TEntity>, delta: Partial<TEntity>, options?: DeltaChangeOptions): Promise<boolean> {
        if (!delta) return false

        let hasChanges = false
        for (const prop of Object.keys(delta)) {
            const value = (obj as any)[prop]
            const newValue = (delta as any)[prop]

            if (options?.ownedEntityProps?.includes(prop)) {
                if (Array.isArray(value) || Array.isArray(newValue)) {
                    const list = value as Array<any> ?? []
                    const newList = newValue as Array<any> ?? []

                    if (list.length === 0 && newList.length === 0) {
                        continue
                    }

                    if (list.length !== newList.length) {
                        hasChanges = true
                    }

                    const listCopy = [...list]
                    const itemPropName = prop + ".item"

                    for (const newItem of newList) {
                        const itemIndex = listCopy.findIndex(x => x?.[DataRepository.ID_PROP] === newItem[DataRepository.ID_PROP])
                        if (itemIndex === -1) {
                            hasChanges = true
                            await this.dbcore.initNewEntity(newItem)
                            options?.entityChanged?.(itemPropName, "added", undefined, newItem)
                        } else {
                            const item = listCopy[itemIndex]
                            listCopy[itemIndex] = undefined

                            if (!isEqual(item, newItem)) {
                                hasChanges = true
                                await this.dbcore.initModifiedEntity(newItem)
                                options?.entityChanged?.(itemPropName, "modified", item, newItem)
                            }
                        }
                    }

                    for (const item of listCopy) {
                        if (item !== undefined) {
                            hasChanges = true
                            options?.entityChanged?.(itemPropName, "deleted", item, undefined)
                        }
                    }
                } else if (!isEqual(value, newValue)) {
                    hasChanges = true
                    if (newValue) {
                        if (value) {
                            await this.dbcore.initModifiedEntity(newValue)
                            options?.entityChanged?.(prop, "modified", value, newValue)
                        } else {
                            await this.dbcore.initNewEntity(newValue)
                            options?.entityChanged?.(prop, "added", undefined, newValue)
                        }
                    } else {
                        options?.entityChanged?.(prop, "deleted", value, undefined)
                    }
                }
            } else if (!isEqual(value, newValue)) {
                hasChanges = true
                options?.valueChanged?.(prop, value, newValue)
            }
        }

        return hasChanges
    }

    protected override async _deleteCore(all: boolean, ids?: string[]): Promise<void> {
        if (!all && !ids?.length) return

        // TODO: Use Transactions?

        // NOTE: Dexie (and probably IndexedDB) has not API for streaming rows based on a list of IDs.
        // In Dexie we have only the "each()" (which is read-only by default) function which takes no IDs and streams all rows.

        let effectiveIds: string[] | undefined
        let items: TEntity[] | undefined

        if (all) {
            items = await this.items()
            effectiveIds = items.map(x => (x as any)[this.tablePrimaryKey] as string)
        } else {
            effectiveIds = ids!
        }

        const __findById = async (id: string): Promise<TEntity | undefined> => {
            return items
                ? items.find(x => id === (x as any)[this.tablePrimaryKey] as string)
                : await this.table.get(id)
        }

        if (this.hasDependents) {

            for (const id of effectiveIds) {
                if (!id) continue

                // TODO: IMPORTANT: Getting errors here:
                //   "Unhandled rejection: NotFoundError: Failed to execute 'objectStore' on 'IDBTransaction': The specified object store was not found.
                let item: TEntity | undefined
                try {
                    item = await __findById(id)
                } catch (err: any) {
                    console.debug("Error in deleteCore: " + err?.message)
                    // throw err
                }

                if (!item) continue

                await this.cascadeDelete(item)
            }
        }

        await this.table.bulkDelete(effectiveIds)

        if (this.hasEntityState) {
            await this.dbcore.entityStates.deleteRange(effectiveIds, this)
        }

        if (await this.table.count() === 0) {
            await this.dbcore.entityTypeStates.delete(this)
        }
    }
}
