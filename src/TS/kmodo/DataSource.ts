
namespace kmodo {

    // KEY_FILTER_ID is provided e.g. via navigation arguments.
    //   Also used when using editor/readonly forms.
    export const KEY_FILTER_ID = "5883120a-b1a6-4ac8-81a2-1d23028daebe";

    // KEY_FILTER_ID is used for filtering by tag.
    //   The tag filter UI resides in the grid's toolbar.
    export const TAGS_FILTER_ID = "2bd9e0d8-7b2d-4c1e-90c0-4d7eac6d01a4";

    export const COMPANY_FILTER_ID = "4c30f0ae-8478-457b-992a-a774c216dca2";
    export const COMPANY_REF_FIELD = "CompanyId";

    export function createLocalDataSourceTransport(data: any[]): kendo.data.DataSourceTransportWithFunctionOperations {

        const transport: kendo.data.DataSourceTransportWithFunctionOperations = {
            read: e => {
                // alert("local read " + e.data);
                e.success(data);
            },
            update: e => {
                // alert("local update " + e.data);
                // TODO:?
                e.success();
            },
            destroy: e => {
                // TODO:?
                e.success();
            },
            create: e => {
                e.success(e.data);
            }
        };

        return transport;
    }


    export function createReadDataSource(url: string, parameter?: any, extraDataSourceOptions?: Object) {

        const dsoptions: kendo.data.DataSourceOptions = {
            type: "odata-v4",
            error: kendoDataSourceODataErrorHandler,
            transport: {
                parameterMap: parameterMapForOData,
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
            for (let prop of Object.keys(extraDataSourceOptions))
                dsoptions[prop] = extraDataSourceOptions[prop];
        }

        return new kendo.data.DataSource(dsoptions);
    }

    export function createODataReadSource(opts: kendo.data.DataSourceOptions): kendo.data.DataSource {
        return new kendo.data.DataSource(extendODataSourceReadOptions(opts));
    }

    export function extendODataSourceReadOptions(opts: kendo.data.DataSourceOptions): kendo.data.DataSourceOptions {
        let options: kendo.data.DataSourceOptions = {
            type: 'odata-v4',
            error: kendoDataSourceODataErrorHandler,
            schema: {
                model: {
                    id: 'Id'
                }
            },
            transport: {
                parameterMap: function (data, type) { return kmodo.parameterMapForOData(data, type, null); },
            },
            pageSize: 0,
            serverPaging: true,
            serverSorting: true,
            serverFiltering: true
        }

        return kmodo.extendDeep(opts, options) as kendo.data.DataSourceOptions;
    }

    export function findDataSourceFilter(
        filters: DataSourceFilterNode[],
        predicate: (filter: DataSourceFilterNode) => boolean): DataSourceFilterNode {

        if (!filters || !filters.length)
            return null;

        for (const filter of filters) {
            if (filter.logic) {
                const foundFilter = findDataSourceFilter(filter.filters, predicate);
                if (foundFilter)
                    return foundFilter;
            }
            else if (predicate(filter))
                return filter;
        }

        return null;
    }

    export interface DataSourceFilterNode extends kendo.data.DataSourceFilter {
        // kendo.data.DataSourceFilters: logic, filters
        // If this is a logical operator node: only @logic and @filters are present.
        logic?: string;
        filters?: DataSourceFilterNode[];

        // kendo.data.DataSourceFilterItem: operator, field, value
        operator?: string;
        field?: string;
        value?: any;

        // @customExpression is used for OData filter expressions
        //   which cannot be expressed by Kendo's filters.
        // IMPORTANT: Note that we modified Kendo's sources to use that customExpression.
        //   I.e. this won't work with the Kendo's default sources.
        customExpression?: string;

        // More custom fields used for identification/activation/deactivation of filters.
        _id?: string;
        _persistent?: boolean;
        _deactivatable?: boolean;
        _targetTypeId?: string;
        // @_targetTypeName is not used by populated by the code generator.
        //   It's a nice-to-have for debug purposes.
        // TODO: REVISIT: Maybe remove in the future.
        _targetTypeName?: string;
    }

    export type DataSourceFilterOneOrMany = DataSourceFilterNode | DataSourceFilterNode[];

    export function buildTagsDataSourceFilters(dataTypeId: string, companyId?: string): DataSourceFilterNode[] {
        const filters: DataSourceFilterNode[] = [];

        filters.push({
            field: "AssignableToTypeId",
            operator: "eq",
            value: dataTypeId
        });

        if (!companyId)
            return filters;

        // Combine with persistent company filter.

        filters.push({
            logic: "or",
            filters: [
                { field: COMPANY_REF_FIELD, operator: "eq", value: companyId, _id: COMPANY_FILTER_ID, _persistent: true },
                { field: COMPANY_REF_FIELD, operator: "eq", value: null }]
        });

        return filters;
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

        let effectiveData = data;
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

        const result = fixODataV4FilterParameterMap(effectiveData, type);

        return result;
    }

    export function fixODataV4FilterParameterMap(data: any, type: string) {
        // This is needed for Kendo grid's filters when using OData v4.

        // Call the default OData V4 parameterMap.
        const result = kendo.data.transports["odata-v4"].parameterMap(data, type);

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
        const ds = new kendo.data.DataSource({
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
        const items = [];
        let item;
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