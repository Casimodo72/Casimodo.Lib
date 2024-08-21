export interface IEntitySelectorOptions {
    title?: string | null
}

export interface IEntitySelectorService<TEntity = any> {
    selectSingle(options?: IEntitySelectorOptions): Promise<Partial<TEntity> | undefined>
    selectMultiple(options?: IEntitySelectorOptions): Promise<Partial<TEntity>[] | undefined>
}

export abstract class EntitySelectorService<TEntity = any> implements IEntitySelectorService<TEntity> {
    async selectSingle(options?: IEntitySelectorOptions): Promise<Partial<TEntity> | undefined> {
        const items = await this.selectCore(true, options)

        return items?.[0]
    }

    selectMultiple(options?: IEntitySelectorOptions): Promise<Partial<TEntity>[] | undefined> {
        return this.selectCore(false, options)
    }

    abstract selectCore(isSingleSelection: boolean, options?: IEntitySelectorOptions): Promise<Partial<TEntity>[] | undefined>
}
