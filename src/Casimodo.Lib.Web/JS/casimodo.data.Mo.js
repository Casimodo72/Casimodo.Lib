"use strict";
var casimodo;
(function (casimodo) {
    (function (data) {

        // KABU TODO: IMPORTANT: Uses geoassistant.
        data.hasMoManagerPermissionOnly = function (mo) {
            if (!mo)
                return false;
           
            if (typeof mo.Permissions === "undefined")
                // Be overly strict if no permission information was retrieved at all.
                return true;

            if (mo.Permissions === null)
                // Currently an empty set of permissions means: no restrictions.
                return false;

            var manamegentPerm = mo.Permissions.find(x => x.RoleId === geoassistant.MoRoleKeys.Manager);
            if (manamegentPerm)
                // Currently a manager permission means: management only.
                return true;

            return false;
        };

        data.createMoTreeViewDataSource = function (items) {
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
        };

        data.createMoFolderDataSource = function (items) {
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
        };

    })(casimodo.data || (casimodo.data = {}));
})(casimodo || (casimodo = {}));