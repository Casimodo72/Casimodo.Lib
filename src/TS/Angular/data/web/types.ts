import { ODataFilterBuilder, ODataQueryBuilder } from "@lib/data/utils"

export interface IStandardQueryOptions<TEntity> {
    query?: ODataQueryBuilder<TEntity>
    buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void
}

export interface IStandardQueryFilterOptions<TEntity> {
    filter?: ODataFilterBuilder<TEntity>
    buildFilter?: (f: ODataFilterBuilder<TEntity>) => void
}
