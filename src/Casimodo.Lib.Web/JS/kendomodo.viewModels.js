"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        /*
        Example: extending ObservableObject:
    
        var Example = kendo.data.ObservableObject.extend({
            someProp: null,        
            init: function(value) {
                var self = this;
                this.someProp = value;
                this.someFunc = function () { };
                kendo.data.ObservableObject.fn.init.call(self, null);
            }       
        });    
        */

        // KABU TODO: Not used anywhere.
        ui.EditCollectionItemViewModel = (function () {
            var EditCollectionItemViewModel = function (options) {
                this.data = options.data;
                this.isNew = !!options.isNew;
                this.index = options.index || 0;
            };

            var fn = EditCollectionItemViewModel.prototype;

            return EditCollectionItemViewModel;
        })();

        // KABU TODO: Not used anywhere.
        ui.EditCollectionViewModel = (function () {
            var EditCollectionViewModel = function (options) {
                this._items = new kendo.data.ObservableArray([]);
                this._removedItems = new kendo.data.ObservableArray([]);
            };

            var fn = EditCollectionViewModel.prototype;

            fn.items = function () {
                return this._items;
            };

            fn.removedItems = function () {
                return this._removedItems;
            };

            fn.getRemovedData = function () {
                return this._removedItems.map(function (x) { return x.data; });
            };

            fn.hasChanges = function () {
                return this.hasRemoved() || this.hasNew();
            };

            fn.hasNew = function () {
                return this._items.filter(function (x) { return x.isNew && x.data; }).length !== 0;
            };

            fn.getNewData = function () {
                return this._items.filter(function (x) { return x.isNew && x.data; }).map(function (x) { return x.data; });
            };

            fn.hasRemoved = function () {
                return this._removedItems.length !== 0;
            };

            fn.findByUid = function (uid) {
                return this._items.find(function (x) { return x.uid === uid; });
            };

            fn.last = function () {
                return this._items.length ? this._items[this._items.length - 1] : null;
            };

            fn.where = function (predicate) {
                return new kendo.data.ObservableArray(this._items.filter(predicate));
            };

            fn.addData = function (dataItem, isNew) {
                var idx = this._items.length;
                var item = this._createItem({ data: dataItem, index: idx, isNew: !!isNew });
                this._items.push(item);
                this._updateIndexes();

                return item;
            };

            fn.addDataRange = function (dataItems, isNew) {
                var self = this;
                dataItems.forEach(function (x) { self.addData(x, isNew); });
            };

            fn._createItem = function (options) {
                return kendo.observable(new kendomodo.EditCollectionItemViewModel(options));
            };

            fn.removeByUid = function (uid) {
                var item = this._items.find(function (x) { return x.uid === uid; });
                if (item) {
                    this._items.remove(item);
                    if (!item.isNew) this._removedItems.push(item);
                    this._updateIndexes();
                }

                return item;
            };

            fn.removeData = function (dataItem) {
                var item = this._items.find(function (x) { x.data === dataItem; });
                if (item) {
                    this._items.remove(item);
                    if (!item.isNew) this._removedItems.push(item);
                    this._updateIndexes();
                }

                return item;
            };

            fn.removeRange = function (items) {
                var self = this;
                items.forEach(function (x) { self._items.remove(x); });
            };

            fn.removeAt = function (idx) {
                var item = this._items.at(idx);
                if (item) {
                    this._items.remove(item);
                    if (!item.isNew) this._removedItems.push(item);
                    this._updateIndexes();
                }

                return item;
            };

            fn._updateIndexes = function () {
                var item;
                for (var i = 0; i < this._items.length; i++) {
                    item = this._items[i];
                    item.index = i;
                    if (item.data && typeof item.data.Index !== "undefined") item.data.set("Index", i);
                }
            };

            return EditCollectionViewModel;
        })();

        // KABU TODO: Not used anywhere.
        var PickItemLookupModel = (function () {
            var PickItemLookupModel = function (url, idProp, displayProp) {
                this.url = url;
                this.idProp = idProp;
                this.displayProp = displayProp;
                this.sortProp = displayProp;
                this._items = [];
            };

            var fn = PickItemLookupModel.prototype;

            // Non-async lookup function.
            fn.fetch = function () {
                this._items = kendomodo.oDataLookupValueAndDisplay(this.url, this.idProp, this.displayProp, false);
                return this;
            };

            fn.items = function () {

                var items = [];
                var item;
                for (var i = 0; i < this._items.length; i++) {
                    item = this._items[i];
                    items.push({
                        value: item.value,
                        text: item.text
                    });
                }

                return items;
            };

            fn.get = function (id) {
                var items = this._items;
                var length = items.length;
                var prop = this.idProp;
                for (var i = 0; i < length; i++) {
                    if (items[i][prop] === id)
                        return items[i];
                }
            };

            /*
            fn.compare = function (id1, id2) {
                if (id1 === id2)
                    return 0;
    
                var item1 = fn.get(id1);
                var item2 = fn.get(id2);
    
                if (item1[this.sortProp] < item2[this.sortProp])
                    return -1;
                else if (item1[this.sortProp] === item2[this.sortProp])
                    return 0;
                else
                    return 1;
            };
            */

            return PickItemLookupModel;
        })();
        ui.PickItemLookupModel = PickItemLookupModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
