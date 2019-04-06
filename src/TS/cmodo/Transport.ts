
namespace cmodo {

    export function webApiGet(url: string, data?: any, options?: any): Promise<any> {
        return _webApiAction("GET", url, data, options);
    }

    export function webApiPost(url: string, data?: any, options?: any): Promise<any> {
        return _webApiAction("POST", url, data, options);
    }

    function _webApiAction(method: string, url: string, data?: any, options?: any): Promise<any> {

        return new Promise((resolve, reject) => {

            options = options || {};

            $.ajax({
                url: url,
                type: method,
                contentType: "application/json;charset=utf-8",
                dataType: options.resultDataType || "json",
                data: data ? JSON.stringify(data) : null,
                // KABU TODO: IMPORTANT: I don't know anymore why I don't use the success callback.
                complete: function (xhr, textStatus) {
                    // NOTE: We need to use the "complete" handler, because
                    // it will be called after the success and error handlers,
                    // which we'll use for error checking.
                    if (xhr.status >= 200 && xhr.status < 300) {
                        if (xhr.responseJSON) {
                            const data = xhr.responseJSON;
                            if (!options.isDataFixupDisabled)
                                cmodo.fixupDataDeep(data);
                            resolve(data);
                        }
                        else
                            resolve(xhr.responseText);
                        return;
                    }

                    // KABU TODO: VERY IMPORTANT: What to do exactly in this case?
                    reject();
                },
                error: function (jqXHR: JQueryXHR) {
                    const msg = getODataErrorMessageFromJQueryXHR(jqXHR);
                    cmodo.showError(msg);
                    reject(new Error(msg));
                }
            });
        });
    }

    export function oDataQuery(url: string, options?: any): Promise<any> {

        return new Promise(function (resolve, reject) {
            const headers = { "Content-Type": "application/json", Accept: "application/json" };
            const request = {
                requestUri: url,
                method: "GET",
                headers: headers,
                data: options ? options.data || null : null
            };

            odatajs.oData.request(request, function (data) {
                resolve(getODataResponseValue(data, options));
            }, function (err: odatajs.HttpOData.Error) {
                const msg = getODataErrorMessageFromOlingo(err);
                cmodo.showError(msg);
                reject(new Error(msg));
            });

        });
    }

    function getODataResponseValue(data: any, options?: any): any {
        let value = data;
        if (!value)
            return null;

        // NOTE: Web API OData queries always have a "value" field.
        // Custom function, etc. do not have a "value" field.
        if (typeof data.value !== "undefined")
            value = data.value;

        if (options) {
            if (options.single) {
                if (typeof value.length !== "undefined") {
                    value = value.length ? value[0] : null;
                }
            }
        }

        cmodo.fixupDataDeep(value);

        return value;
    }

    export function oDataAction(url, method, args): Promise<any> {
        return oDataFunctionOrAction(url, method, "action", null, args);
    }

    export function oDataFunction(url: string, method: string, args?: any | any[]): Promise<any> {
        return oDataFunctionOrAction(url, method, "function", null, args);
    }

    function oDataFunctionOrAction(url: string, method: string, kind: string, id: string, args?: any | any[]): Promise<any> {
        return new Promise((resolve, reject) => {
            url = url + "/" + method;
            let payload = null;

            if (kind === "function") {
                url += "(";
                if (args) {
                    if (!Array.isArray(args))
                        args = [args];

                    url += args.map((x: { name: string; value: string; }) => x.name + "=" + x.value).join(",");
                }
                url += ")";
            } else {
                url += "()";
                if (args) {
                    if (!Array.isArray(args))
                        args = [args];

                    payload = {};
                    for (const arg of args)
                        payload[arg.name] = arg.value;
                }
            }

            executeODataRequestCore(kind === "function" ? "GET" : "POST", url, payload)
                .then(resolve)
                .catch(reject);
        });
    }

    export function executeODataRequestCore(httpMethod: string, url: string, data?: any): Promise<any> {

        return new Promise((resolve, reject) => {

            const headers = { "Content-Type": "application/json", Accept: "application/json" };
            const request = {
                requestUri: url,
                method: httpMethod,
                headers: headers,
                data: data || null
            };

            odatajs.oData.request(request,
                function (data) {
                    resolve(getODataResponseValue(data));
                }, function (err: odatajs.HttpOData.Error) {
                    const msg = getODataErrorMessageFromOlingo(err);
                    cmodo.showError(msg);
                    reject(new Error(msg));
                });
        });
    }

    interface ODataCoreOptions {
        url: string,
        id?: string,
        method?: string,
        data?: any
    }

    export interface ODataUpdateOptions extends ODataCoreOptions {
        url: string,
        id: string,
        method?: string,
        data: any
    }

    export interface ODataCreateOptions {
        url: string,
        data: any
    }

    export interface ODataDeleteOptions {
        url: string,
        id: string,
    }

    export function oDataCreate(opts: ODataCreateOptions): Promise<any> {
        return oDataCRUD("POST", null, opts);
    }

    export function oDataUpdate(opts: ODataUpdateOptions): Promise<any> {
        return oDataCRUD("POST", "use-model", opts);
    }

    export function oDataDelete(opts: ODataDeleteOptions): Promise<any> {

        return oDataCRUD("DELETE", null, opts);
    }

    function oDataCRUD(httpMethod: string, strategy: string, opts: ODataCoreOptions): Promise<any> {

        let url = opts.url;

        if (opts.id)
            url += "(" + opts.id + ")";

        if (opts.method)
            url += "/" + opts.method;

        // OData actions (which we are using for updates) need a named parameter in the payload.
        let data: any = opts.data
            ? (strategy === "use-model" ? { model: opts.data } : opts.data)
            : null

        const request: odatajs.HttpOData.Request = {
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            requestUri: url,
            method: httpMethod,
            data: data
        };

        return _executeODataCore(request);
    }

    function _executeODataCore(request: odatajs.HttpOData.Request): Promise<any> {

        return new Promise(function (resolve, reject) {
            const method = request.method;

            odatajs.oData.request(request, function (data) {
                if (method === "PUT" || method === "DELETE")
                    resolve();
                else if (method === "POST")
                    resolve(data);
            }, function (err: odatajs.HttpOData.Error) {
                const msg = getODataErrorMessageFromOlingo(err);
                cmodo.showError(msg);
                reject(new Error(msg));
            });

        });
    }

    // Formatters

    export function toODataUtcDateFilterString(date: Date): string {
        if (!date)
            return null;

        return moment(date).format("YYYY-MM-DD") + "T00:00:00Z";
    }

    // Error helpers

    export function getODataErrorMessageFromJQueryXHR(jqXhr: JQueryXHR): string {
        const items: string[] = [];
        const statusCode = jqXhr.status.toString();

        // jqXHR: https://api.jquery.com/Types/#jqXHR   
        if (jqXhr.response) {
            items.push(getODataErrorMessage(statusCode, jqXhr.response));
        } else if (jqXhr.responseText) {
            items.push(getODataErrorMessage(statusCode, jqXhr.responseText));
        }

        return formatErrorMessageParts(statusCode, items);
    }

    function getODataErrorMessageFromOlingo(error: odatajs.HttpOData.Error): string {
        const items: string[] = [];

        const statusCode = error.response ? error.response.statusCode : "";

        if (error.response) {
            const body = error.response.body;
            if (body) {
                items.push(getODataErrorMessage(statusCode, body));
            }
        }

        if (items.length === 0 && error.message) {
            // Fallback to the standard error message provided by olingo.
            items.push(error.message);
        }

        return formatErrorMessageParts(statusCode, items);
    }

    function getODataErrorMessage(statusCode: string, body: any): string {
        return getAnyApiErrorMessage(statusCode, body);
    }

    const isErrorStackTraceEnabled = false;

    function getAnyApiErrorMessage(statusCode: string, body: any): string {
        const items: string[] = [];

        let error: IAnyApiError = null;

        if (typeof body === "string") {
            if (!body.startsWith("{")) {
                let msg = statusCode ? `(${statusCode}) ` : "";
                msg += body as string;
                // Just a string; no error object.
                return msg;
            }

            // The error object must contain an "error" field.
            error = JSON.parse(body).error || null;
        } else {
            error = body.error || null;
        }

        if (error) {
            // Prefer details over main message.
            //   We need that in case ASP Core returns model validation errors.
            if (error.details && error.details.length) {
                for (const errorDetail of error.details) {
                    items.push(getErrorPartAsText(errorDetail));
                }
            } else if (error.message) {
                items.push(getErrorPartAsText(error));
            }           

            if (isErrorStackTraceEnabled) {
                for (let err = error.innererror; !!err; err = err.innererror) {
                    items.push(">> " + getErrorPartAsText(err));
                }
            }
        }

        return formatErrorMessageParts(statusCode, items);
    }

    function formatErrorMessageParts(statusCode: string, items: string[]): string {
        if (items.length) {
            return items.join("\n");
        }

        return `(${statusCode}) [Error message not available]`;
    }

    interface IAnyApiErrorDetails {
        code: string;
        message: string;
        target?: string;
    }

    interface IAnyApiError extends IAnyApiErrorDetails {
        details?: IAnyApiErrorDetails[];
        innererror?: IAnyApiInnerError;
    }

    // OData spec: "The value for the innererror name/value pair MUST be an object.
    //   The contents of this object are service-defined"
    interface IAnyApiInnerError {
        trace?: string;
        // NOTE: .NET ODataError also has a "Message" and "TypeName" property
        // which are not part of the OData spec.
        innererror?: IAnyApiInnerError;
        message?: string;
        typeName?: string;
    }

    function getErrorPartAsText(error: any): string {
        let text = "";
        if (error.code) text += `(${error.code}) `;
        if (error.target) text += `${error.target}: `;
        if (error.message) text += `${error.message}`;

        if (isErrorStackTraceEnabled) {
            if (error.typeName) text += `[${error.typeName}]: `;
            if (error.trace) text += `\nStack trace: ${error.trace.replace("\r\n", "\n")})`;
        }

        return text;
    }


    export class UrlBuilder {
        path: string;
        params: string;
        constructor(path: string) {
            this.path = path || "";
            this.params = "";
        }

        param(name: string, value: string) {
            if (typeof value !== "undefined" && value !== null) {
                if (this.params)
                    this.params += "&";
                this.params += name + "=" + value;
            }

            return this;
        }

        get(): string {
            let result = this.path;
            if (this.path && this.params)
                result += "?" + this.params;

            return result;
        }

        toString(): string {
            return this.get();
        }
    }
}