// TODO: DELETE FILE
/*
namespace kmodo {

    function createReadDataSource(url: string, parameter?: any, options?: any): kendo.data.DataSource {

        const dsoptions: kendo.data.DataSourceOptions = {
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
            for (let prop of Object.keys(options))
                dsoptions[prop] = options[prop];
        }

        return new kendo.data.DataSource(dsoptions);
    }
}
*/