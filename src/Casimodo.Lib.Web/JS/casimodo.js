"use strict";

var casimodo;
(function (casimodo) {

    // KABU TODO: IMPORTANT: Replace with TypeScript generated __extends
    casimodo.__extends = casimodo.__extends || function (d, b) {
        for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };

    casimodo.EventManager = (function () {
        // KABU TODO: Should we use the built-in Event class instead?
        // See https://developer.mozilla.org/en-US/docs/Web/API/Event
        var EventManager = function (source) {
            this._source = source || null;
            this._events = [];
        };

        var fn = EventManager.prototype;

        fn.on = function (eventName, func) {
            this._add(eventName, func, null);
        };

        fn.one = function (eventName, func) {
            this._add(eventName, func, "one");
        };

        fn.trigger = function (eventName, e, source) {

            var eve = this._get(eventName, false);
            if (!eve)
                return;

            if (e) {
                if (typeof e._defaultPrevented === "undefined" && typeof e.defaultPrevented === "undefined")
                    e.defaultPrevented = false;

                if (!e.preventDefault) {
                    e.preventDefault = function () {
                        e.defaultPrevented = true;
                    };
                }
            }

            for (var i = 0; i < eve.bindings.length; i++) {
                var binding = eve.bindings[i];

                if (binding.mode === "one") {
                    eve.bindings.splice(i, 1);
                    i--;
                }

                try {
                    binding.func.call(source || this._source, e);
                }
                catch (e) {
                    // KABU TODO: How to handle exceptions here? Suppress?
                }
            }
        };

        fn._add = function (eventName, func, mode) {
            this._get(eventName, true).bindings.push({ mode: mode, func: func });
        };

        fn._get = function (eventName, createIfMissing) {
            var eves = this._events;
            for (var i = 0; i < this._events.length; i++)
                if (eves[i].name === eventName)
                    return eves[i];

            if (!createIfMissing)
                return null;

            var eve = { name: eventName, bindings: [] };
            eves.push(eve);

            return eve;
        };

        return EventManager;
    })();

    var ObservableObject = (function () {

        function ObservableObject() {
            this._events = new casimodo.EventManager();
        }

        var fn = ObservableObject.prototype;

        fn.on = function (eventName, func) {
            this._events.on(eventName, func);
        };

        fn.one = function (eventName, func) {
            this._events.one(eventName, func);
        };

        fn.trigger = function (eventName, e) {
            this._events.trigger(eventName, e, this);
        };

        return ObservableObject;

    })();
    casimodo.ObservableObject = ObservableObject;

    casimodo.isPropPathNotNull = function (obj, path) {

        if (!obj || !path || typeof path !== "string" || !path.length)
            return false;

        var value = casimodo.getValueAtPropPath(obj, path);

        return typeof value !== "undefined" && value !== null;
    };

    /**
        NOTE: Returns null if the object at path is undefined.
    */
    casimodo.getValueAtPropPath = function (obj, path) {
        if (obj === null || typeof path !== "string" || !path.length)
            return null;

        var steps = path.split('.');
        do { obj = obj[steps.shift()]; } while (steps.length && typeof obj !== "undefined" && obj !== null);

        if (typeof obj === "undefined")
            return null;

        return obj;
    };

    casimodo.toDisplayBool = function (value) {
        // KABU TODO: How to return culture specific values?
        if (value === true)
            return "Ja";
        else if (value === false)
            return "Nein";

        return "";
    };

    casimodo.toDisplayTimeSpan = function (value) {
        return value ? "" + value.Hours + ":" + value.Minutes : "";
    };

    casimodo.toODataZonedDateTimeEncode = function (value, timezone) {
        if (!value)
            return null;

        var dateTime = moment.tz(value, timezone);

        return encodeURIComponent(dateTime.toISOString());      
    };

    casimodo.toODataFilterValueEncode = function (value) {
        if (!value)
            return null;

        return casimodo.toODataValue(value, true);
    };

    casimodo.toODataValue = function (value, encode) {
        if (!value)
            return null;

        if (value._isAMomentObject || Object.prototype.toString.call(value) === "[object Date]") {
            // Add 2 milliseconds because ASP.NET returns values more precise than
            // the JS Date (only milliseconds). Thus locally we will have truncated values.
            // If we don't add 2 ms then we will get already downloaded items over and over.
            // NOTE that adding only 1 ms does not always work, because e.g. the time 22:51:12.4845062
            // gets converted to 22:51:12.483 (dunno why). So we have to add 2 ms to get the next higher value.
            value = moment(value).add(2, "ms");

            value = value.format("YYYY-MM-DD[T]HH:mm:ss.SSSZ");

            if (encode)
                value = encodeURIComponent(value);

            return value;
        }

        return value;
    };

    casimodo.removeFileNameExtension = function (fileName, fileExtension) {

        if (fileName && fileExtension && fileName.endsWith("." + fileExtension)) {
            return fileName.substring(0, fileName.length - fileExtension.length - 1);
        }

        return fileName;
    };

    casimodo.getDateDiff = function (date1, date2) {
        return moment(date1).startOf('day').diff(moment(date2).startOf("day"), "days", true);
    };

    casimodo.getNowDateDiff = function (date) {
        return moment().startOf('day').diff(moment(date).startOf("day"), "days", true);
    };

    // Source: http://stackoverflow.com/questions/728360/most-elegant-way-to-clone-a-javascript-object
    casimodo.cloneDeep = function (obj) {
        var copy;

        // Handle the 3 simple types, and null or undefined
        if (null === obj || "object" !== typeof obj) return obj;

        // Handle Date
        if (obj instanceof Date) {
            copy = new Date();
            copy.setTime(obj.getTime());
            return copy;
        }

        // Handle Array
        if (obj instanceof Array) {
            copy = [];
            for (var i = 0, len = obj.length; i < len; i++) {
                copy[i] = clone(obj[i]);
            }
            return copy;
        }

        // Handle Object
        if (obj instanceof Object) {
            copy = {};
            for (var attr in obj) {
                if (obj.hasOwnProperty(attr)) copy[attr] = clone(obj[attr]);
            }
            return copy;
        }

        throw new Error("Unable to copy obj! Its type isn't supported.");
    };

    casimodo.trimLeft = function (text, trim) {
        if (text.startsWith(trim))
            text = text.substring(trim.length);

        return text;
    };

    casimodo.collapseWhitespace = function (text) {
        if (!text) return text;
        // KABU TODO: collapse any other whitespace characters as well.
        text = text.replace(/\s+/g, ' ').trim();
        return text;
    };

    casimodo.firstCharToUpper = function (text) {
        var first = text.charAt(0).toUpperCase();
        if (first === text.charAt(0))
            return text;

        return first + text.slice(1);
    };

    casimodo.isEmptyOrWhiteSpace = function (value) {
        return value === null || value === "" || (/^\s*$/).test(value);
    };

    /**
        Sets all empty or whitespace-only strings to null.
     */
    casimodo.whiteSpacePropsToNull = function (item, propInfos) {
        var propNames = Object.getOwnPropertyNames(propInfos),
            name,
            value;

        for (var i = 0; i < propNames.length; i++) {
            name = propNames[i];
            if (!item.hasOwnProperty(name))
                continue;

            value = item[name];
            if (value === null || typeof value !== "string" || !casimodo.isEmptyOrWhiteSpace(value))
                continue;

            item[name] = null;
        }
    };

    casimodo.GuidEmpty = function () {
        return "00000000-0000-0000-0000-000000000000";
    };

    casimodo.normalizeTimeToMinutes = function (value) {
        // Sets seconds and milliseconds to zero.
        return value ? moment(value).startOf("minute").toDate() : null;
    };

    casimodo.parseURL = function (url) {
        // Source: http://www.abeautifulsite.net/parsing-urls-in-javascript/

        var parser = document.createElement('a'),
            searchObject = {},
            queries, split, i;

        // Let the browser do the work
        parser.href = url;

        // Convert query string to object
        queries = parser.search.replace(/^\?/, '').split('&');
        for (i = 0; i < queries.length; i++) {
            split = queries[i].split('=');
            searchObject[split[0]] = split[1];
        }

        return {
            protocol: parser.protocol,
            host: parser.host,
            hostname: parser.hostname,
            port: parser.port,
            pathname: parser.pathname,
            search: parser.search,
            searchObject: searchObject,
            hash: parser.hash
        };
    };

    // Source: http://pixelscommander.com/en/javascript/javascript-file-download-ignore-content-type/
    casimodo.downloadFile = function (url, fileName) {

        //iOS devices do not support downloading. We have to inform user about this.
        if (/(iP)/g.test(navigator.userAgent)) {
            alert('Your device do not support files downloading. Please try again in desktop browser.');
            return false;
        }

        //If in Chrome or Safari - download via virtual link click
        if (casimodo.downloadFile.isChrome || casimodo.downloadFile.isSafari) {
            //Creating new link node.
            var link = document.createElement('a');
            link.href = url;

            if (link.download !== undefined) {
                //Set HTML5 download attribute. This will prevent file from opening if supported.
                //var fileName = sUrl.substring(sUrl.lastIndexOf('/') + 1, sUrl.length);
                link.download = fileName;
            }

            //Dispatching click event.
            if (document.createEvent) {
                var e = document.createEvent('MouseEvents');
                e.initEvent('click', true, true);
                link.dispatchEvent(e);
                return true;
            }
        }

        // Force file download (whether supported by server).
        var query = '?download';

        window.open(url + query, '_self');
    };

    casimodo.downloadFile.isEdge = navigator.userAgent.toLowerCase().indexOf('edge') > -1;
    casimodo.downloadFile.isChrome = navigator.userAgent.toLowerCase().indexOf('chrome') > -1;
    casimodo.downloadFile.isSafari = navigator.userAgent.toLowerCase().indexOf('safari') > -1;

})(casimodo || (casimodo = {}));

Number.prototype.zeroPad = function (numZeros) {
    var n = Math.abs(this);
    var zeros = Math.max(0, numZeros - Math.floor(n).toString().length);
    var zeroString = Math.pow(10, zeros).toString().substr(1);
    if (this < 0) {
        zeroString = '-' + zeroString;
    }

    return zeroString + n;
};

// KABU TODO: REMOVE when "String.prototype.endsWith" is available in ES6.
// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
if (typeof String.prototype.endsWith !== 'function') {

    String.prototype.endsWith = function (suffix) {
        return this.indexOf(suffix, this.length - suffix.length) !== -1;
    };
}

// KABU TODO: REMOVE when "String.prototype.startsWith" is available in ES6.
// https://developer.mozilla.org/de/docs/Web/JavaScript/Reference/Global_Objects/String/startsWith
if (typeof String.prototype.startsWith !== 'function') {

    String.prototype.startsWith = function (prefix) {
        if (!this || !prefix || prefix.length > this.length)
            return false;

        var index = -1;
        while (++index < prefix.length) {
            if (this.charCodeAt(index) !== prefix.charCodeAt(index)) {
                return false;
            }
        }

        return true;
    };
}

// Polyfill for Array.prototype.find
// Source: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/find#Polyfill
if (!Array.prototype.find) {
    Object.defineProperty(Array.prototype, 'find', {
        enumerable: false,
        configurable: true,
        writable: true,
        value: function (predicate) {
            if (this === null) {
                throw new TypeError('Array.prototype.find called on null or undefined');
            }
            if (typeof predicate !== 'function') {
                throw new TypeError('predicate must be a function');
            }
            var list = Object(this);
            var length = list.length >>> 0;
            var thisArg = arguments[1];
            var value;

            for (var i = 0; i < length; i++) {
                if (i in list) {
                    value = list[i];
                    if (predicate.call(thisArg, value, i, list)) {
                        return value;
                    }
                }
            }
            return undefined;
        }
    });
}

// Source: http://stackoverflow.com/questions/3241881/jquery-index-of-element-in-array-where-predicate
if (!Array.prototype.findIndex) {
    Array.prototype.findIndex = function (predicate) {
        if (this === null) {
            throw new TypeError('Array.prototype.findIndex called on null or undefined');
        }
        if (typeof predicate !== 'function') {
            throw new TypeError('predicate must be a function');
        }
        var list = Object(this);
        var length = list.length >>> 0;
        var thisArg = arguments[1];
        var value;

        for (var i = 0; i < length; i++) {
            value = list[i];
            if (predicate.call(thisArg, value, i, list)) {
                return i;
            }
        }
        return -1;
    };
}

String.isNullOrWhiteSpace = function (value) {
    return !value || value.length === 0 || /^\s*$/.test(value);
};

// Polyfill for Promise.prototype.finally
// Sources: https://hospodarets.com/promise.prototype.finally
(function () {
    // based on https://github.com/matthew-andrews/Promise.prototype.finally

    // Get a handle on the global object
    let globalObject;
    if (typeof global !== 'undefined') {
        globalObject = global;
    } else if (typeof window !== 'undefined' && window.document) {
        globalObject = window;
    }

    // check if the implementation is available
    if (typeof Promise.prototype['finally'] === 'function') {
        return;
    }

    // implementation
    globalObject.Promise.prototype['finally'] = function (callback) {
        const constructor = this.constructor;

        return this.then(function (value) {
            return constructor.resolve(callback()).then(function () {
                return value;
            });
        }, function (reason) {
            return constructor.resolve(callback()).then(function () {
                throw reason;
            });
        });
    };
}());