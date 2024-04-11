import { HttpClient } from "@angular/common/http"
import { inject } from "@angular/core"
import { ODataQueryBuilder } from "@lib/data-utils"
import { lastValueFrom } from "rxjs"
import { fixupReceivedDataDeep } from "../utils"

export interface IWebApiResult<TData> {
    readonly hasSucceeded: boolean
    readonly data?: TData | TData[]
}

export abstract class WebService {
    protected readonly _http = inject(HttpClient)
    readonly baseUrl: string | undefined

    constructor(baseUrl?: string) {
        this.baseUrl = baseUrl
    }

    protected async _query<TData>(path: string,
        buildQuery?: (qb: ODataQueryBuilder<any>) => void
    ): Promise<TData> {
        const qb = new ODataQueryBuilder<any>()

        let url = ""

        if (this.baseUrl) {
            url = this.baseUrl
            if (!url.endsWith("/")) {
                url += "/"
            }
        }

        url += path

        qb.url(url)

        buildQuery?.(qb)

        const query = qb.toString()

        const response = await lastValueFrom(this._http.get<any>(query))
        fixupReceivedDataDeep(response)

        const data = url.includes("odata/")
            // OData returns data in a property named "value".
            // JFYI: OData does that because it can also return metadata in the response.
            ? response.value
            : response

        return data
    }
}

export abstract class EntityWebService<TEntity> extends WebService {
    protected _queryEntities(path: string, buildQuery?: (qb: ODataQueryBuilder<TEntity>) => void) {
        return super._query<Partial<TEntity>[]>(path, buildQuery)
    }
}
