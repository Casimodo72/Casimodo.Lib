import { DataRepository } from "./dataRepository"

export abstract class RepositoriesContainer {
    readonly #items: DataRepository[] = []

    items(): DataRepository[] {
        return this.#items
    }

    add(repository: DataRepository): void {
        (this as any)[repository.tableName] = repository
        this.#items.push(repository)
    }

    getByEntityTypeId(entityTypeId: string): DataRepository {
        const repo = this.#items.find(x => x.entityTypeId === entityTypeId)
        if (!repo) {
            throw new Error(`Repository for entity type ID '${entityTypeId}' not found.`)
        }

        return repo
    }

    getByEntityTypeName(entityTypeName: string): DataRepository {
        const repo = this.#items.find(x => x.entityTypeName === entityTypeName)
        if (!repo) {
            throw new Error(`Repository for entity type '${entityTypeName}' not found.`)
        }

        return repo
    }

    async _deleteAllTables() {
        for (const repo of this.#items) {
            await repo._deleteTable()
        }
    }
}
