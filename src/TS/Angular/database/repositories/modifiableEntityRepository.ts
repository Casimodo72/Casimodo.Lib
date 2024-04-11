import { cloneDeep } from "lodash-es"

import { IEntityCore } from "@lib/data"

import { EntityRepository } from "./entityRepository"

export class ModifiableEntityRepository<TEntity extends Partial<IEntityCore>> extends EntityRepository<TEntity> {
    protected async _modify(entity: Partial<TEntity>, delta: Partial<TEntity>): Promise<boolean> {
        await this.dbcore.initModifiedEntity(delta as IEntityCore)

        await this.table.db.transaction("rw", [this.table, this.dbcore.entityStates.table], async () => {
            const storeDelta: Partial<TEntity> = Object.assign({}, delta)
            storeDelta._isSyncPending = true

            if (await this.table.update(entity.Id!, storeDelta) === 0) {
                throw new Error(`Failed to update the entity in the local DB (ID: ${entity.Id}).`)
            }

            await this.dbcore.entityStates.addPatch(entity as IEntityCore, this, delta)

            Object.assign(entity, cloneDeep(storeDelta))
        })

        if (this._remote) {
            await this._remote.trySend(entity)
        }

        return true
    }
}
