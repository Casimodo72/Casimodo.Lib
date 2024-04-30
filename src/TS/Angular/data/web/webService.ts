import { HttpClient } from "@angular/common/http"
import { Injectable, inject } from "@angular/core"
import { ODataQueryBuilder } from "@lib/data-utils"
import { lastValueFrom } from "rxjs"
import { fixupReceivedDataDeep } from "../utils"
import { AuthService } from "@lib/auth"

export interface IWebApiResult<TData> {
    readonly hasSucceeded: boolean
    readonly data?: TData | TData[]
}

export abstract class AbstractWebService {
    protected readonly _http = inject(HttpClient)
    readonly basePath: string | undefined

    constructor(basePath?: string) {
        this.basePath = basePath
    }

    protected async _query<TData>(
        path: string,
        buildQuery?: (qb: ODataQueryBuilder<any>) => void
    ): Promise<TData> {
        const qb = new ODataQueryBuilder<any>()

        const url = this._applyBasePath(path)
        qb.url(url)

        buildQuery?.(qb)

        return await this._queryByUrl(qb.toString())
    }

    protected _applyBasePath(path: string) {
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

        const data = queryUrl.includes("odata/")
            // OData returns data in a property named "value".
            // JFYI: OData does that because it can also return metadata in the response.
            ? response.value
            : response

        return data
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

    async query<TData>(
        path: string,
        buildQuery?: (qb: ODataQueryBuilder<any>) => void
    ): Promise<TData> {
        return this._query(path, buildQuery)
    }
}

export abstract class AppEntityWebService<TEntity> extends AbstractWebService {
    readonly #authService = inject(AuthService)

    protected _queryEntities(path: string, buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void) {
        return super._query<Partial<TEntity>[]>(path, buildQuery)
    }

    get requiredUserId() {
        return this.#authService.requiredUser.Id
    }

    get requiredCompanyId() {
        return this.#authService.requiredUser.CompanyId
    }
}
