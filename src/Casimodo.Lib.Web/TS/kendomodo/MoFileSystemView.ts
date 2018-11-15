
namespace kmodo {
    export function createMoTreeViewDataSource(items: Object[]): kendo.data.HierarchicalDataSource {
        return new kendo.data.HierarchicalDataSource({
            data: items || [],
            schema: {
                model: {
                    id: "Id",
                    children: "folders",
                    hasChildren: function (item) {
                        return item.folders && item.folders.length;
                    },
                    fields: {
                        Id: { validation: { required: true }, editable: false, defaultValue: '00000000-0000-0000-0000-000000000000', },
                        Name: { validation: { required: true }, defaultValue: "" },
                        ParentId: {},
                        TypeId: {},
                        IsContainer: { type: 'boolean' },
                        Role: {},
                        CreatedOn: { type: 'date' },
                        ModifiedOn: { type: 'date' }
                        //Index: { type: 'number' }
                    }
                }
            }
        });
    }

    // KABU TODO: REMOVE? Not used
    /*
    function createMoFolderDataSource(items): kendo.data.DataSource {
        return new kendo.data.DataSource({
            data: items || [],
            schema: {
                model: {
                    id: "Id",
                    fields: {
                        Id: { validation: { required: true }, editable: false, defaultValue: '00000000-0000-0000-0000-000000000000', },
                        Name: { validation: { required: true }, defaultValue: "" },
                        ParentId: {},
                        TypeId: {},
                        IsContainer: { type: 'boolean' },
                        Role: {},
                        CreatedOn: { type: 'date' },
                        ModifiedOn: { type: 'date' }
                        //Index: { type: 'number' }
                    }
                }
            }
        });
    }
    */
}