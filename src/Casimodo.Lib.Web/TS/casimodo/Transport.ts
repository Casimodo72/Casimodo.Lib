
namespace cmodo {

    export function webApiGet(url: string, data: any, options: any): Promise<any> {
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
                            var data = xhr.responseJSON;
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
                error: function (err) {
                    handleServerError("webapi", err);
                    reject(err);
                }
            });
        });
    }

    export function oDataQuery(url: string, options?: any): Promise<any> {

        return new Promise(function (resolve, reject) {
            var headers = { "Content-Type": "application/json", Accept: "application/json" };
            var request = {
                requestUri: url,
                method: "GET",
                headers: headers,
                data: options ? options.data || null : null
            };

            odatajs.oData.request(request, function (data) {
                resolve(getODataResponseValue(data, options));
            }, function (err) {
                var msg = getResponseErrorMessage("odata", err.response);

                cmodo.showError(msg);

                reject(msg);
            });

        });
    }

    function getODataResponseValue(data: any, options?: any): any {
        var value = data;
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
        return oDataFunctionOrAction(url, method, "action", args);
    }

    export function oDataFunction(url: string, method: string, kind: string, args?: any | any[]): Promise<any> {
        return oDataFunctionOrAction(url, method, "function", args);
    }

    function oDataFunctionOrAction(url: string, method: string, kind: string, args?: any | any[]): Promise<any> {
        return new Promise(function (resolve, reject) {
            url = url + "/" + method;
            var payload = null;

            if (kind === "function") {
                url += "(";
                if (args) {
                    if (!Array.isArray(args))
                        args = [args];

                    url += args.map(function (x) { return x.name + "=" + x.value; }).join(",");
                }
                url += ")";
            }
            else {
                url += "()";
                if (args) {
                    if (!Array.isArray(args))
                        args = [args];

                    payload = {};
                    for (let i = 0; i < args.length; i++) {
                        payload[args[i].name] = args[i].value;
                    }

                    //url += "?" + args.map(function (x) { return x.name + "=" + x.value }).join("&");
                }
            }

            var headers = { "Content-Type": "application/json", Accept: "application/json" };
            var request = {
                requestUri: url,
                method: kind === "function" ? "GET" : "POST",
                headers: headers,
                data: payload
            };

            odatajs.oData.request(request,
                function (data) {
                    resolve(getODataResponseValue(data, null));
                }, function (err) {
                    var msg = getResponseErrorMessage("odata", err.response);

                    cmodo.showError(msg);

                    reject(err);
                });
        });
    }

    export function oDataCRUD(method: string, url: string, id: string, data: any): Promise<any> {

        if (id) {
            url += "(" + id + ")";
        }
        var request: odatajs.HttpOData.Request = {
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            requestUri: url,
            method: method,
            data: data || null
        };

        return _executeODataCore(request);
    }

    function _executeODataCore(request: odatajs.HttpOData.Request): Promise<any> {

        return new Promise(function (resolve, reject) {
            var method = request.method;

            odatajs.oData.request(request, function (data) {
                if (method === "PUT" || method === "DELETE")
                    resolve();
                else if (method === "POST")
                    resolve(data);
            }, function (err) {
                var msg = "";
                if (err.response)
                    msg = getResponseErrorMessage("odata", err.response);
                else
                    msg = getResponseErrorMessage("odata", err);

                cmodo.showError(msg);

                reject(msg);
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

    function handleServerError(kind: string, err: any) {
        var msg = "";
        if (err.response)
            msg = getResponseErrorMessage(kind, err.response);
        else
            msg = getResponseErrorMessage(kind, err);

        cmodo.showError(msg);
    }

    export function getResponseErrorMessage(kind: string, response: any) {

        var responseStatus = "";
        var message = "";

        if (typeof response === "string") {
            return response;
        }

        if (response.status) {
            responseStatus += response.status;
        }
        else if (response.statusCode && typeof response.statusCode !== "function") {
            responseStatus += response.statusCode;
        }

        if (response.statusText)
            responseStatus += " " + response.statusText;

        if (kind === "webapi") {

            if (response.responseJSON && response.responseJSON.Message) {
                message += getWebApiResponseErrorMessage(response.responseJSON);
            }
        }
        else {

            var error = null;

            if (response.responseJSON) {
                if (response.responseJSON.error)
                    error = response.responseJSON.error;
            }
            else if (response.body) {
                // response.body is avaiable when using Olingo instead of Kendo-datasource.

                try {
                    error = JSON.parse(response.body).error;
                }
                catch (ex) {
                    // TODO: The response body is HTML in this case?
                    message += response.body;
                }
            }

            if (error) {
                if (error.message)
                    message += _fixErrorMessage(kind, error.message);

                if (error.innererror) {
                    message += ": " + _fixErrorMessage(kind, error.innererror.message);

                    if (error.innererror.internalexception) {
                        var ex;
                        for (ex = error.innererror.internalexception; ex; ex = ex.internalexception) {
                            if (ex.message)
                                message += " <- " + _fixErrorMessage(kind, ex.message);
                        }
                    }
                }
            }
            else if (response.responseText) {
                // responseText is available when using Kendo-datasource.
                // Use it as a last resort because OData will set responseText to just serialized error object,
                // which is not really of any use to us.
                message += response.responseText;
            }
        }

        if (!message)
            return responseStatus;

        return message;
    }

    function getWebApiResponseErrorMessage(error) {
        var message = "";

        if (error.ExceptionMessage)
            message += error.ExceptionMessage;
        else if (error.Message)
            message += error.Message;

        if (error.MessageDetail)
            message += " " + error.MessageDetail;

        while (error.InnerException) {
            error = error.InnerException;

            if (error.ExceptionMessage)
                message += " << " + error.ExceptionMessage;
            else if (error.Message)
                message += " << " + error.Message;
        }

        return message;
    }

    var _rexODataMessageFix1 = new RegExp("^model : !.+?!");
    var _rexODataMessageFix2 = new RegExp("( Ploc| Plo)[!]*", "g");

    // Removes OData's annoying trailing Ploc's and leading error identifiers from error messages.
    function _fixErrorMessage(kind, message) {
        if (typeof message === "undefined" || message === null)
            return "";
        if (kind !== "odata")
            return message;

        message = message.replace(_rexODataMessageFix1, "").replace(_rexODataMessageFix2, "");

        return message;
    }
}