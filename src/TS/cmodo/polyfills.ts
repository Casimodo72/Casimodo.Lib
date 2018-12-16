
// KABU TODO: REMOVE
// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
//if (typeof String.prototype.endsWith !== 'function') {
//    String.prototype.endsWith = function (suffix) {
//        return this.indexOf(suffix, this.length - suffix.length) !== -1;
//    };
//}

// KABU TODO: REMOVE
// https://developer.mozilla.org/de/docs/Web/JavaScript/Reference/Global_Objects/String/startsWith
//if (typeof String.prototype.startsWith !== 'function') {
//    String.prototype.startsWith = function (prefix) {
//        if (!this || !prefix || prefix.length > this.length)
//            return false;
//        var index = -1;
//        while (++index < prefix.length) {
//            if (this.charCodeAt(index) !== prefix.charCodeAt(index)) {
//                return false;
//            }
//        }
//        return true;
//    };
//}

// KABU TODO: REMOVE: Not used. Eliminate. Move to a lib.
//if (!(Number.prototype as any).zeroPad) {
//    (Number.prototype as any).zeroPad = function (numZeros) {
//        var n = Math.abs(this);
//        var zeros = Math.max(0, numZeros - Math.floor(n).toString().length);
//        var zeroString = Math.pow(10, zeros).toString().substr(1);
//        if (this < 0) {
//            zeroString = '-' + zeroString;
//        }

//        return zeroString + n;
//    };
//}

// Polyfill for Array.prototype.find
// Source: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/find#Polyfill
//if (!Array.prototype.find) {
//    Object.defineProperty(Array.prototype, 'find', {
//        enumerable: false,
//        configurable: true,
//        writable: true,
//        value: function (predicate) {
//            if (this === null) {
//                throw new TypeError('Array.prototype.find called on null or undefined');
//            }
//            if (typeof predicate !== 'function') {
//                throw new TypeError('predicate must be a function');
//            }
//            var list = Object(this);
//            var length = list.length >>> 0;
//            var thisArg = arguments[1];
//            var value;

//            for (let i = 0; i < length; i++) {
//                if (i in list) {
//                    value = list[i];
//                    if (predicate.call(thisArg, value, i, list)) {
//                        return value;
//                    }
//                }
//            }
//            return undefined;
//        }
//    });
//}

// Source: http://stackoverflow.com/questions/3241881/jquery-index-of-element-in-array-where-predicate
//if (!Array.prototype.findIndex) {
//    Array.prototype.findIndex = function (predicate) {
//        if (this === null) {
//            throw new TypeError('Array.prototype.findIndex called on null or undefined');
//        }
//        if (typeof predicate !== 'function') {
//            throw new TypeError('predicate must be a function');
//        }
//        var list = Object(this);
//        var length = list.length >>> 0;
//        var thisArg = arguments[1];
//        var value;

//        for (let i = 0; i < length; i++) {
//            value = list[i];
//            if (predicate.call(thisArg, value, i, list)) {
//                return i;
//            }
//        }
//        return -1;
//    };
//}

// Polyfill for Promise.prototype.finally
// Sources: https://hospodarets.com/promise.prototype.finally
(function () {
    let global = undefined;
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