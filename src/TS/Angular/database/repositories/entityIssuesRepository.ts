import { Database, EntityIssue, Repository } from "@lib/database"
import { IssueEntityNode } from "@lib/data"

function toTableKey(entityId: string, repository: Repository): [string, string] {
    return [entityId, repository.entityTypeId]
}

export class EntityIssuesRepository extends Repository<EntityIssue, [string, string]> {
    override readonly tableName = "entityIssues"
    override readonly tableKeys = "[id+typeId]"
    readonly #db: Database<any, any>

    constructor(db: Database<any, any>) {
        super()
        this.dbcore = db
        this.#db = db
    }

    override _setTable(table: any) {
        this.table = table
    }

    async getOrAddNew(entityId: string, repository: Repository): Promise<EntityIssue> {
        return await this.table.get(toTableKey(entityId, repository))
            ?? await this.addNew(entityId, repository)
    }

    async find(entityId: string, repository: Repository): Promise<EntityIssue | undefined> {
        return await this.table.get(toTableKey(entityId, repository))
    }

    async addNew(entityId: string, repository: Repository): Promise<EntityIssue> {
        const entityTypeName = this.#db.getEntityTypeNameById(repository.entityTypeId)

        const entry: EntityIssue = {
            id: entityId,
            typeId: repository.entityTypeId,
            typeName: entityTypeName,
            issues: false
        }

        await this.table.add(entry)

        return entry
    }

    async modify(
        entityId: string,
        repository: Repository,
        action: (changes: { delta: { issues?: IssueEntityNode } }, entityState: EntityIssue) => Promise<void>)
        : Promise<void> {

        const entity = await this.getOrAddNew(entityId, repository)
        const changes = { delta: {} }

        await action(changes, entity)

        if (!changes.delta || !Object.keys(changes.delta).length)
            return

        await this.table.update(toTableKey(entityId, repository), changes.delta)
    }

    async delete(id: string, repository: Repository): Promise<void> {
        await this.table.delete(toTableKey(id, repository))
    }

    async deleteRange(ids: string[], repository: Repository): Promise<void> {
        if (!ids.length) return

        await this.table.bulkDelete(ids.map(id => toTableKey(id, repository)))
    }

    override async _deleteTable() {
        await this.table.clear()
    }
}
