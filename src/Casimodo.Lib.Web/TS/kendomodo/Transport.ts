/// <reference path="../casimodo/Transport.ts" />
/// <reference path="DataSource.ts" />

namespace kmodo {

    export function odataQueryFirstOrDefault(url: string, parameter): Promise<any> {

        return new Promise(function (resolve, reject) {
            var ds = createReadDataSource(url, parameter);

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
                    var msg = cmodo.getResponseErrorMessage("odata", err);

                    cmodo.showError(msg);

                    reject(err);
                });
        });
    }

    function createReadDataSource(url: string, parameter: any, options?: any): kendo.data.DataSource {

        var dsoptions: kendo.data.DataSourceOptions = {
            type: "odata-v4",
            transport: {
                parameterMap: function (data, type) { return kmodo.parameterMapForOData(data, type, null); },
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

        if (options) {
            Object.keys(options).forEach(function (prop) {
                dsoptions[prop] = options[prop];
            });
        }

        return new kendo.data.DataSource(dsoptions);
    }
}