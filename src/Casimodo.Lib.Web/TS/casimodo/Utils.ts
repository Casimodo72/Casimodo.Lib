﻿
namespace cmodo {

    export function guid() {
        // To be defined by the consumer app.
        throw new Error("guid() not implemented.");
    }

    export function guidEmpty(): string {
        return "00000000-0000-0000-0000-000000000000";
    }

    export function normalizeTimeToMinutes(value: Date): Date {
        // Sets seconds and milliseconds to zero.
        return value ? moment(value).startOf("minute").toDate() : null;
    }

    // KABU TODO: REMOVE? Not used
    /*
    function isPropPathNotNull (obj: any, path: string): boolean {

        if (!obj || !path || typeof path !== "string" || !path.length)
            return false;

        var value = cmodo.getValueAtPropPath(obj, path);

        return typeof value !== "undefined" && value !== null;
    }
    */

    export function isNullOrWhiteSpace(value: string): boolean {
        return !value || value.length === 0 || /^\s*$/.test(value);
    }

    /**
        Sets all empty or whitespace-only strings to null.
     */
    export function whiteSpacePropsToNull(item: any, propInfos: any): void {
        var propNames: string[] = Object.getOwnPropertyNames(propInfos),
            name: string,
            value: any;

        for (let i = 0; i < propNames.length; i++) {
            name = propNames[i];
            if (!item.hasOwnProperty(name))
                continue;

            value = item[name];
            if (value === null || typeof value !== "string" || !isNullOrWhiteSpace(value))
                continue;

            item[name] = null;
        }
    }

    export function removeFileNameExtension(fileName: string, fileExtension: string): string {

        if (fileName && fileExtension && fileName.endsWith("." + fileExtension)) {
            return fileName.substring(0, fileName.length - fileExtension.length - 1);
        }

        return fileName;
    }

    export function toDisplayBool(value: boolean): string {
        // KABU TODO: LOCALIZE
        if (value === true)
            return "ja";
        else if (value === false)
            return "nein";

        return "";
    }

    // KABU TODO: Not used
    //export function toDisplayTimeSpan(value: Date): string {
    //    return value ? "" + value.getHours() + ":" + value.getMinutes() : "";
    //}

    export function toODataZonedDateTimeEncode(value: Date, timezone: string): string {
        if (!value)
            return null;

        // KABU TODO: moment tz type declaration.
        var dateTime = (moment as any).tz(value, timezone);

        return encodeURIComponent(dateTime.toISOString());
    }

    export function toODataFilterValueEncode(value: Date | moment.Moment): string {
        if (!value)
            return null;

        return toODataValue(value, true);
    }

    export function toODataValue(value: string | Date | moment.Moment, encode: boolean): string {
        if (!value)
            return null;
        else if (!(value as any)._isAMomentObject && Object.prototype.toString.call(value) !== "[object Date]")
            throw new Error("Invalid argument: value must be of type Date or Moment.")

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

    export function getValueAtPropPath(obj: any, path: string) {

        if (obj === null || !path.length)
            return null;

        var steps = path.split('.');
        do { obj = obj[steps.shift()]; } while (steps.length && typeof obj !== "undefined" && obj !== null);

        if (typeof obj === "undefined")
            return null;

        return obj;
    }

    export function getDateDiff(date1: Date, date2: Date): number {
        return moment(date1).startOf('day').diff(moment(date2).startOf("day"), "days", true);
    }

    export function getNowDateDiff(date: Date): number {
        return moment().startOf('day').diff(moment(date).startOf("day"), "days", true);
    }

    export function trimLeft(text: string, trim: string): string {
        if (text.startsWith(trim))
            text = text.substring(trim.length);

        return text;
    }

    export function collapseWhitespace(text: string): string {
        if (!text) return text;
        // KABU TODO: collapse any other whitespace characters as well.
        text = text.replace(/\s+/g, ' ').trim();
        return text;
    }

    export function firstCharToUpper(text: string): string {
        var first = text.charAt(0).toUpperCase();
        if (first === text.charAt(0))
            return text;

        return first + text.slice(1);
    }

    // Source: http://stackoverflow.com/questions/728360/most-elegant-way-to-clone-a-javascript-object
    export function cloneDeep(obj: any | null | undefined): any {
        if (typeof obj === "undefined" || obj === null || "object" !== typeof obj) return obj;

        var copy: any;

        // Handle Date
        if (obj instanceof Date) {
            copy = new Date();
            copy.setTime(obj.getTime());
            return copy;
        }

        // Handle Array
        if (obj instanceof Array) {
            copy = [];
            for (let i = 0, len = obj.length; i < len; i++) {
                copy[i] = cloneDeep(obj[i]);
            }
            return copy;
        }

        // Handle Object
        if (obj instanceof Object) {
            copy = {};
            for (var attr in obj) {
                if (obj.hasOwnProperty(attr)) copy[attr] = cloneDeep(obj[attr]);
            }
            return copy;
        }

        throw new Error("Unable to copy the object: its type is not supported.");
    }

    export function fixupDataDeep(data: any) {
        if (!data) return;

        if (Array.isArray(data)) {
            for (let i = 0; i < data.length; i++)
                fixupDataDeep(data[i]);

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
    }

    // KABU TODO: Not used.

    export function parseURL(url: string): any {
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
    }

    var _userAgent = {
        isEdge: navigator.userAgent.toLowerCase().indexOf('edge') > -1,
        isChrome: navigator.userAgent.toLowerCase().indexOf('chrome') > -1,
        isSafari: navigator.userAgent.toLowerCase().indexOf('safari') > -1
    }

    // Source: http://pixelscommander.com/en/javascript/javascript-file-download-ignore-content-type/
    export function downloadFile(url: string, fileName: string): boolean {

        //iOS devices do not support downloading. We have to inform user about this.
        if (/(iP)/g.test(navigator.userAgent)) {
            alert('Your device do not support files downloading. Please try again in desktop browser.');
            return false;
        }

        //If in Chrome or Safari - download via virtual link click
        if (_userAgent.isChrome || _userAgent.isSafari) {
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

        return true;
    }

    export function _tryCleanupHtml(text: string) {
        try {
            var root = (new DOMParser()).parseFromString(text, "text/html").documentElement;
            var body = Array.from(root.children).find(x => x.localName === "body");
            if (body) {
                _tryCleanupHtmlCore(body);

                var html = body.innerHTML.replace(/\n/g, "").trim();

                return { ok: true, html: html };
            }

            return { ok: false };
        }
        catch (err) {
            return { ok: false };
        }
    }

    function _tryCleanupHtmlCore(parent: Element) {
        if (!parent.childNodes || !parent.childNodes.length)
            return;

        var node, name;
        var remove = [];
        var i;

        for (i = 0; i < parent.childNodes.length; i++) {
            node = parent.childNodes[i];
            name = node.localName;
            if (node.nodeType === Node.TEXT_NODE)
                continue;

            if (node.nodeType !== Node.ELEMENT_NODE) {
                remove.push(node);
            }
            else if (name === "br" || name === "hr") {
                remove.push(node);
            }
            else {
                if (name === "font")
                    node.face = "";

                _tryCleanupHtmlCore(node);
            }
        }

        for (i = 0; i < remove.length; i++) {
            parent.removeChild(remove[i]);
        }
    }
}