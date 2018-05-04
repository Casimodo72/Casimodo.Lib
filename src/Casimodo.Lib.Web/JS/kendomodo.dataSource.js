"use strict";

var kendomodo;
(function (kendomodo) {

    kendomodo.parameterMapForOData = function (data, type, strategy) {

        var effectiveData = data;
        if (type === "update" && strategy === "Action") {
            // OData actions need a named parameter in the payload.
            effectiveData = new kendo.data.ObservableObject({
                model: data
            });
        }

        if (type !== "read") {
            // Convert numbers to strings in order to satisfy IEEE754Compatible.
            // This is needed because Kendo does not convert number fields
            // in nested objects.
            // See https://github.com/telerik/kendo-ui-core/issues/2043
            // Using traverse: https://github.com/substack/js-traverse
            traverse(effectiveData).forEach(function (x) {
                if ((x || (x === 0)) && this.isLeaf && typeof x === "number" && this.key) {
                    this.update(x + '');
                }
            });
        }

        var result = kendomodo.fixODataV4FilterParameterMap(effectiveData, type);

        return result;
    };

    kendomodo.fixODataV4FilterParameterMap = function (data, type) {
        // This is needed for Kendo grid's filters when using OData v4.

        // Call the default OData V4 parameterMap.
        var result = kendo.data.transports["odata-v4"].parameterMap(data, type);

        if (type === "read" && result && result.$filter) {

            // Remove the single quotation marks around the GUID (OData v4).
            // See http://www.telerik.com/forums/guids-in-filters
            result.$filter = result.$filter.replace(/('[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}')/ig, function (x) {
                return x.substring(1, x.length - 1);
            });
        }
        return result;
    };

    // Only needed for Kendo MVC grids.
    // Removes all model fields which are not listed in the given @usedFieldNames.
    kendomodo.fixupDataSourceModel = function (grid, usedFieldNames) {
        if (!usedFieldNames)
            return;

        usedFieldNames = usedFieldNames.split(",");

        var fields = grid.dataSource.reader.model.fields;
        //var protoFields = grid.dataSource.reader.model.fn.fields;        
        var names = Object.getOwnPropertyNames(fields);
        var name;
        for (var i = 0; i < names.length; i++) {
            name = names[i];
            if (usedFieldNames.indexOf(name) === -1) {
                delete fields[name];
            }
        }
    };

    // KABU TODO: REMOVE? Not used
    function setSchemaFieldValidationRequired(field, required) {
        if (!field.validation) {
            field.validation = {
            };
        }
        field.validation.required = true;
    }

    kendomodo.oDataLookupValueAndDisplay = function (url, valueProp, displayProp, async) {
        var ds = new kendo.data.DataSource({
            type: 'odata-v4',
            transport: {
                read: {
                    url: url,
                    async: async
                },

            },
            schema: {
                parse: function (data) {
                    data.value = kendomodo.convertLookupData(data.value, valueProp, displayProp);
                    return data;
                }
            }
        });

        if (!async) {
            ds.read();
            return ds.data();
        }

        return new Promise(function (resolve, reject) {

            ds.read()
                .then(function () {
                    resolve(ds.data().value);
                })
                .fail((ex) => reject());

        });
    };

    kendomodo.convertLookupData = function (data, valueProp, displayProp) {
        var items = [];
        var item;
        for (var i = 0; i < data.length; i++) {
            item = data[i];
            items.push({
                value: item[valueProp],
                text: item[displayProp]
            });
        }

        return items;
    };

    kendomodo.oDataQuery = function (url, parameter, options) {

        return new Promise((resolve, reject) => {
            var ds = createReadDataSource(url, parameter);
            var guid = kendomodo.guid();

            ds.fetch()
                .then(function () {
                    var items = ds.data();
                    // Clear the data source.
                    ds.data([]);

                    casimodo.data.fixupDataDeep(items);

                    resolve(items);

                }, function (err) {
                    var msg = casimodo.getResponseErrorMessage("odata", err);

                    casimodo.ui.showError(msg);

                    reject();
                });
        });
    };

    kendomodo.odataQueryFirstOrDefault = function (url, parameter) {

        return new Promise(function (resolve, reject) {
            var ds = createReadDataSource(url, parameter);
            var guid = kendomodo.guid();

            ds.fetch()
                .then(function () {
                    var items = ds.data();
                    // Clear the data source.
                    ds.data([]);
                    if (items.length)
                        resolve(items[0]);
                    else
                        resolve(null);
                }, function (err) {
                    var msg = casimodo.getResponseErrorMessage("odata", err);

                    casimodo.ui.showError(msg);

                    reject(err);
                });
        });
    };

    kendomodo.query = function (url, parameter, callback) {

        var ds = createReadDataSource(url, parameter);
        var guid = kendomodo.guid();

        ds.fetch()
            .then(function () {
                var items = ds.data();
                // Clear the data source.
                ds.data([]);
                // Invoke callback
                if (callback) callback(items);
            }, function (err) {
                var msg = casimodo.getResponseErrorMessage("odata", err);

                casimodo.ui.showError(msg);
            });
    };

    kendomodo.queryObsolete = function (url, parameter, callback) {

        var ds = createReadDataSource(url, parameter);
        var guid = kendomodo.guid();

        // On completed
        ds.transport.options.read.complete = function (context, status) {

            ds.transport.options.read.complete = null;

            if (status === "success") {

                var items = ds.data();

                // Clear the data source.
                ds.data([]);

                // Invoke callback
                if (callback) callback(items);
            }
            else if (status === "error") {

                if (callback) callback(null);
                //var message = getReponseErrorMessage(context);                    
            }
        };

        // KABU TODO: REMOVE: Apparently this one is never called. Dunny why.
        // Error handler
        // ds.options.error = function (e) { }

        // KABU TODO: Try using ds.fetch() instead.
        ds.read();
    };

    kendomodo.createLocalDataSource = function (items) {
        return new kendo.data.DataSource({
            data: items
        });
    };

    function createReadDataSource(url, parameter) {
        return new kendo.data.DataSource({
            type: "odata-v4",
            transport: {
                read: {
                    url: url,
                    dataType: "json",
                    data: function (optionsData) {
                        if (parameter)
                            return parameter;
                        else
                            return optionsData;
                    }
                }
            },
            schema: {
                model: {
                    id: "Id"
                }
            }
        });
    }
    kendomodo.createReadDataSource = createReadDataSource;


    kendomodo.extendDisplayableMoFiles = function (items) {
        // Convention: enhance File models if applicable.

        if (!Array.isArray(items) || !items.length)
            return;

        var first = items[0];

        for (var prop in first) {
            if (prop !== "Files" && first.hasOwnProperty(prop) && Array.isArray(first[prop])) {
                kendomodo.extendDisplayableMoFiles(first[prop]);
            }
        }

        var isFile = typeof first.FileName !== "undefined";
        var hasFile = typeof first.File !== "undefined";
        var hasFiles = typeof first.Files !== "undefined";

        if (!isFile && !hasFile && !hasFiles)
            return;

        for (var i = 0; i < items.length; i++) {

            if (!isFile && !hasFile && !hasFiles)
                break;

            var item = items[i];

            if (isFile) {
                kendomodo.extendFileByConvention(item);
            }
            else {

                if (hasFile) {

                    if (item.File) {

                        if (typeof item.File.FileName === "undefined") {
                            hasFile = false;
                            continue;
                        }

                        kendomodo.extendFileByConvention(item.File);
                        // KABU TODO: REMOVE
                        //kendomodo.extendFileByConvention(item.File);
                        //item.fileNameNoExtension = item.File.fileNameNoExtension;
                        //item.fileUrl = item.File.fileUrl;
                    }
                }

                if (hasFiles) {
                    kendomodo.extendDisplayableMoFiles(item.Files);
                }
            }
        }
    };

    kendomodo.extendFileByConvention = function (file) {
        if (!file) return file;

        // Remove file extension from file name.
        file.fileNameNoExtension = casimodo.removeFileNameExtension(file.FileName, file.FileExtension);

        kendomodo.extendFileByUrls(file);

        return file;
    };

    kendomodo.extendFileByUrls = function (file) {
        if (!file) return file;

        // Build file URL.
        var pdf = file.FileExtension === "pdf";
        file.fileUrl = "/FileViewer/" + (pdf ? "Pdf" : "File") + "/" + file.Id;
        file.fileDownloadUrl = "/api/GetFileForDownload/" + file.Id;

        return file;
    };

    // Sets empty string values to either default values or null.
    // This is needed because Kendo annoyingly insists in initializing string values to ""
    //   even if we explicitely specify NULL as default value.
    kendomodo.initDataItemOnCreating = function (item, propInfos) {
        var propNames = Object.getOwnPropertyNames(propInfos);
        var info;

        for (var i = 0; i < propNames.length; i++) {
            var name = propNames[i];
            if (!item.hasOwnProperty(name))
                continue;

            if (item[name] !== "")
                continue;

            info = propInfos[name];

            if (info.defaultValue !== undefined)
                item[name] = info.defaultValue;
            else
                item[name] = null;
        }
    };

    // KABU TODO: ELIMINATE and move into the view models.
    kendomodo.onServerErrorOData = function (args) {

        var message = casimodo.getResponseErrorMessage("odata", args.xhr);

        casimodo.ui.showError(message);

        var $errorBox = $("#validation-errors-box");
        if ($errorBox) {
            $errorBox.empty();
            var template = kendo.template("<li>#:message #</li>");
            $errorBox.append(template({
                message: message
            }));
        }

        return message;
    };

})(kendomodo || (kendomodo = {}));
