import { HttpClient } from "@angular/common/http"
import { Injectable, inject } from "@angular/core"
import { lastValueFrom } from "rxjs"

import { AuthService } from "@lib/auth"
import { fixupReceivedDataDeep } from "@lib/data/utils/utils"
import { ODataFilterBuilder, ODataQueryBuilder, PropPath, toGuid } from "@lib/data/utils"
import type { IStandardQueryFilterOptions, IStandardQueryOptions } from "./types"

export interface IWebServiceConfig {
    basePath?: string
}

export interface IWebApiResult<TData> {
    readonly hasSucceeded: boolean
    readonly data?: TData | TData[]
}

@Injectable()
export abstract class AbstractWebService {
    protected readonly _http = inject(HttpClient)
    readonly basePath: string | undefined

    protected constructor(config?: IWebServiceConfig) {
        this.basePath = config?.basePath
    }

    protected async _query<TData>(
        path: string | null | undefined,
        buildQuery?: (qb: ODataQueryBuilder<any>) => void,
        query?: ODataQueryBuilder<any>
    ): Promise<TData> {
        const qb = query ?? new ODataQueryBuilder<any>()

        buildQuery?.(qb)
        const effectivePath = path ?? qb.path ?? "query"
        const url = this._applyBasePath(effectivePath)
        qb.url(url)

        return await this._queryByUrl(qb.toString())
    }

    protected _applyBasePath(path: string | null | undefined) {
        if (!path) return this.basePath ?? ""

        if (this.basePath && path.startsWith(this.basePath)) {
            return path
        }

        let url = ""

        if (this.basePath) {
            url = this.basePath
            if (!url.endsWith("/")) {
                url += "/"
            }
        }

        url += path

        return url
    }

    protected async _queryByUrl(queryUrl: string) {
        const response = await lastValueFrom(this._http.get<any>(queryUrl))
        fixupReceivedDataDeep(response)

        if (queryUrl.includes("odata/")) {
            if (response.value !== undefined) {
                // OData returns data in a property named "value" (only for arrays?).
                return response.value
            }

            // TODO: Do we want to remove any OData metadata properties from the data?
        }

        return response
    }

    protected _performOperation(operation: () => Promise<any>): Promise<any> {
        // TODO: Error handling?
        return operation()
    }
}

export interface IDataSourceWebService {
    get<TData>(url: string): Promise<TData>
}

@Injectable({ providedIn: "root" })
export class DataSourceWebService extends AbstractWebService implements IDataSourceWebService {
    constructor() {
        super()
    }

    get<TData>(url: string): Promise<TData> {
        return this._queryByUrl(url)
    }

    async query<TData>(path: string, buildQuery?: (qb: ODataQueryBuilder<any>) => void): Promise<TData> {
        return this._query(path, buildQuery)
    }
}

const _entityIdProp = new PropPath("Id")

export abstract class AppEntityWebService<TEntity> extends AbstractWebService {

    readonly #authService = inject(AuthService)

    protected _queryEntitiesCore(query: ODataQueryBuilder<TEntity>) {
        return super._query<Partial<TEntity>[]>(null, undefined, query)
    }

    protected _queryEntitiesWithBuild(buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void) {
        return super._query<Partial<TEntity>[]>(null, buildQuery)
    }

    async queryEntity(buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void): Promise<Partial<TEntity> | undefined> {
        const entities = await super._query<Partial<TEntity>[]>(null, buildQuery)

        return entities[0]
    }

    queryEntityById(id: string, buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void): Promise<Partial<TEntity> | undefined> {
        return this.queryEntity(q => {
            q.filter(f => f.eq(_entityIdProp, toGuid(id)))
            buildQuery?.(q)
        })
    }

    get requiredUserId() {
        return this.#authService.requiredUser.Id
    }

    get requiredCompanyId() {
        return this.#authService.requiredUser.CompanyId
    }

    buildQuery(options: IStandardQueryOptions<TEntity>) {
        const q = this._ensureQueryBuilder(options.query)
        q.url(this._applyBasePath("query"))
        options.buildQuery?.(q)

        return q
    }

    buildFilter(options: IStandardQueryFilterOptions<TEntity>) {
        const f = this._ensureFilterBuilder(options.filter)
        options.buildFilter?.(f)

        return f
    }

    protected _ensureQueryBuilder(q?: ODataQueryBuilder<TEntity>) {
        return q ?? new ODataQueryBuilder<TEntity>()
    }

    protected _ensureFilterBuilder(f?: ODataFilterBuilder<TEntity>) {
        return f ?? new ODataFilterBuilder<TEntity>()
    }
}
