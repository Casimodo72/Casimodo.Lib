"use strict";
var casimodo;
(function (casimodo) {
    (function (ns) {
    
        ns.initDomainDataItem = function (item, itemTypeName, scenario) {
            throw new Error("The function 'casimodo.data.initDomainDataItem' is expected to be set by the application.");
        };

        ns.fixupDataDeep = function (data) {
            if (!data) return;

            if (Array.isArray(data)) {
                for (var i = 0; i < data.length; i++)
                    ns.fixupDataDeep(data[i]);

                return;
            }

            // Parse time fields by convention: time fields end with "On" and do not start with "Is".
            // Using "traverse" https://github.com/substack/js-traverse
            traverse(data).forEach(function (x) {
                if (x && this.isLeaf &&
                    typeof x === "string" &&
                    this.key &&
                    this.key.endsWith("On") &&
                    !this.key.startsWith("Is")) {

                    // Parse time.
                    this.update(moment(x).toDate());
                }
            });
        };

    })(casimodo.data || (casimodo.data = {}));
})(casimodo || (casimodo = {}));