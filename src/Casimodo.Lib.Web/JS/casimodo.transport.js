"use strict";

var casimodo;
(function (casimodo) {

    // Web API ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Apply authorization
    casimodo.getActivityAuth = function (area) {

        //return casimodo.webApiGet("/api/GetActivityAuth/" + area, null, { isDataFixupDisabled: true })
        //    .then(function (result) {
        //        if (result.result && result.result.Items)
        //            return new ActivityAuthInfo(result.result);
        //        else
        //            throw new Error("Authorization service did not return valid authorization info.");
        //    });
    };

    casimodo.getActionAuth = function (queryItems) {
        return casimodo.webApiPost("/api/GetActionAuth", queryItems, { isDataFixupDisabled: true })
            .then(function (response) {
                return new AuthActionManager(response.result);
            });
    };

    var AuthPart = (function () {

        function AuthPart(container, part) {
            this.container = container;
            this.part = part;
        }

        var fn = AuthPart.prototype;

        fn.can = function (action, vrole) {
            if (!this.part)
                return false;

            vrole = vrole || null;

            var permissions = this.part.Permissions;
            var perm;
            for (var i = 0; i < permissions.length; i++) {
                perm = permissions[i];
                if (perm.Action === action && perm.VRole === vrole)
                    return true;
            }

            return false;
        };

        return AuthPart;
    })();

    var AuthActionManager = (function () {

        function AuthActionManager(data) {
            this.items = data.Items;
            this.userId = data.UserId;
            this.userName = data.UserName;
            this.userRoles = data.UserRoles;
        }

        var fn = AuthActionManager.prototype;

        fn.hasUserRole = function (role) {
            for (var i = 0; i < this.userRoles.length; i++)
                if (this.userRoles[i] === role)
                    return true;

            return false;
        };

        fn.part = function (name, group) {
            group = group || null;
            return new AuthPart(this, this.items.find(x => x.Part === name && (group === "*" || x.Group === group)));
        };

        return AuthActionManager;

    })();

    var AuthContext = (function (_super) {
        casimodo.__extends(AuthContext, _super);

        function AuthContext() {
            _super.call(this);

            this.manager = null;
            this.items = [];
        }

        var fn = AuthContext.prototype;

        fn.read = function () {
            var self = this;
            return Promise.resolve()
                .then(() => casimodo.getActionAuth(self.items))
                .then(function (manager) {
                    self.manager = manager;
                    self.userId = manager.userId;
                    self.userName = manager.userName;
                    
                    self.trigger("read", { sender: this, auth: manager });

                    return manager;
                });
        };

        fn.hasUserRole = function (role) {
            if (!this.manager)
                return false;

            return this.manager.hasUserRole(role);
        };

        fn.addQueries = function (queries) {
            for (var i = 0; i < queries.length; i++) {
                this.items.push(queries[i]);
            }
        };

        return AuthContext;

    })(casimodo.ObservableObject);
    casimodo.AuthContext = AuthContext;

    casimodo.authContext = new casimodo.AuthContext();

    casimodo.webApiGet = function (url, data, options) {
        return _webApiAction("GET", url, data, options);
    };

    casimodo.webApiPost = function (url, data, options) {
        return _webApiAction("POST", url, data, options);
    };

    function _webApiAction(method, url, data, options) {
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
                                casimodo.data.fixupDataDeep(data);
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
                    casimodo.handleServerError("webapi", err);
                    reject(err);
                }
            });
        });
    }

    // OData ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    casimodo.oDataCRUD = function (method, url, id, data) {

        if (id) {
            url += "(" + id + ")";
        }
        var headers = { "Content-Type": "application/json", Accept: "application/json" };
        var request = {
            requestUri: url,
            method: method,
            headers: headers,
            data: data || null
        };

        return _executeODataCore(request);
    };

    function _executeODataCore(request) {

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
                    msg = casimodo.getResponseErrorMessage("odata", err.response);
                else
                    msg = casimodo.getResponseErrorMessage("odata", err);
                alert(msg);
                reject(msg);
            });

        });
    }

    casimodo.oDataQuery = function (url, options) {

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
                var msg = casimodo.getResponseErrorMessage("odata", err.response);
                alert(msg);
                reject(msg);
            });

        });
    };

    function getODataResponseValue(data, options) {
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

        casimodo.data.fixupDataDeep(value);

        return value;
    }

    casimodo.oDataAction = function (url, method, args) {
        return casimodo.oDataFunctionOrAction(url, method, "action", args);
    };

    casimodo.oDataFunction = function (url, method, args) {
        return casimodo.oDataFunctionOrAction(url, method, "function", args);
    };

    casimodo.oDataFunctionOrAction = function (url, method, kind, args) {
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
                    for (var i = 0; i < args.length; i++) {
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
                    var msg = casimodo.getResponseErrorMessage("odata", err.response);
                    alert(msg);
                    reject(err);
                });
        });
    };

    // Formatters

    casimodo.toODataUtcDateFilterString = function (date) {
        if (!date)
            return null;

        return moment(date).format("YYYY-MM-DD") + "T00:00:00Z";
    };

    // Error helpers

    casimodo.handleServerError = function (kind, err) {
        var msg = "";
        if (err.response)
            msg = casimodo.getResponseErrorMessage(kind, err.response);
        else
            msg = casimodo.getResponseErrorMessage(kind, err);
        alert(msg);
    };

    casimodo.getResponseErrorMessage = function (kind, response) {

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
    };

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

})(casimodo || (casimodo = {}));