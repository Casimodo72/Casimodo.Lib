
namespace kmodo {

    export function oDataQuery(url: string, parameter?: any, extraDataSourceOptions?: Object): Promise<any> {

        return new Promise((resolve, reject) => {
            var ds = createReadDataSource(url, parameter, extraDataSourceOptions);

            ds.fetch()
                .then(function () {
                    var items = ds.data();
                    // Clear the data source.
                    ds.data([]);

                    cmodo.fixupDataDeep(items);

                    resolve(items);

                }, function (err) {
                    var msg = cmodo.getResponseErrorMessage("odata", err);

                    cmodo.showError(msg);

                    reject();
                });
        });
    }

    export function createReadDataSource(url: string, parameter?: any, extraDataSourceOptions?: Object) {

        var dsoptions: kendo.data.DataSourceOptions = {
            type: "odata-v4",
            transport: {
                parameterMap: function (data, type) { return parameterMapForOData(data, type); },
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
            },
            pageSize: 0,
            serverPaging: true,
            serverSorting: true,
            serverFiltering: true
        };

        if (extraDataSourceOptions) {
            Object.keys(extraDataSourceOptions).forEach(function (prop) {
                dsoptions[prop] = extraDataSourceOptions[prop];
            });
        }

        return new kendo.data.DataSource(dsoptions);
    }

    export function findDataSourceFilter(filters: any[], predicate: (filter: any) => boolean): any | null {
        if (!filters || !filters.length)
            return null;

        var filter;
        for (let i = 0; i < filters.length; i++) {
            filter = filters[i];
            if (filter.logic) {
                var foundFilter = findDataSourceFilter(filter.filters, predicate);
                if (foundFilter)
                    return foundFilter;
            }
            else if (predicate(filter))
                return filter;
        }

        return null;
    }

    export interface ExtKendoDataSourceFilterItem extends kendo.data.DataSourceFilterItem {
        _filterId?: string;
        customExpression?: string;
        targetTypeId?: string;
        deactivatable?: boolean;
    }

    export function buildTagsDataSourceFilters(dataTypeId: string, companyId?: string)
        : kendo.data.DataSourceFilter {

        var assignableFilter = { field: "AssignableToTypeId", operator: "eq", value: dataTypeId };

        if (typeof companyId === "undefined")
            return assignableFilter as kendo.data.DataSourceFilterItem;

        return {
            logic: "and",
            filters: [
                assignableFilter,
                {
                    logic: "or",
                    filters: [
                        { field: "CompanyId", operator: "eq", value: companyId, targetTypeId: "59a58131-960d-4197-a537-6fbb58d54b8a", deactivatable: false },
                        { field: "CompanyId", operator: "eq", value: null }]
                }
            ]
        } as kendo.data.DataSourceFilters;
    }

    // NOTE: Kendo will normalize an array of DataSourceFilterItem.
    /*
    function normalizeFilter(expression) {
            if (expression && !isEmptyObject(expression)) {
                if (isArray(expression) || !expression.filters) {
                    expression = {
                        logic: 'and',
                        filters: isArray(expression) ? expression : [expression]
                    };
                }
                normalizeOperator(expression);
                return expression;
            }
        }
    */

    export function parameterMapForOData(data: any, type: string, strategy?: string) {

        var effectiveData = data;
        if (strategy === "Action" && (type === "update" || type === "create")) {
            // We are using OData actions for updates.
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

                // Ignore members of ObservableArray. Otherwise the integer value
                // of field "length" of an ObservableArray would be converted to a string.
                if (this.parent && this.parent.node instanceof kendo.data.ObservableArray)
                    return;

                if ((x || (x === 0)) && this.isLeaf && typeof x === "number" && this.key) {
                    this.update(x + '');
                }
            });
        }

        var result = fixODataV4FilterParameterMap(effectiveData, type);

        return result;
    }

    export function fixODataV4FilterParameterMap(data: any, type: string) {
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
    }

    export function oDataLookupValueAndDisplay(url: string, valueProp: string, displayProp: string, async: boolean): Promise<any> | kendo.data.ObservableArray {
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
                    data.value = _convertLookupData(data.value, valueProp, displayProp);
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
                    resolve((ds.data() as any).value);
                })
                .fail((ex) => reject());

        });
    }

    function _convertLookupData(data: any[], valueProp: string, displayProp: string): any[] {
        var items = [];
        var item;
        for (let i = 0; i < data.length; i++) {
            item = data[i];
            items.push({
                value: item[valueProp],
                text: item[displayProp]
            });
        }

        return items;
    }
}