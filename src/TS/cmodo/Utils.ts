
namespace cmodo {

    export function guid(): string {
        // Intended to be overriden by the consumer app.
        throw new Error("guid() not implemented.");
    }

    export function guidEmpty(): string {
        return "00000000-0000-0000-0000-000000000000";
    }

    export function clearArray(items: Array<any>): void {
        items.splice(0, items.length);
    }

    export function last(array: Array<any>): any {
        if (!Array.isArray(array))
            throw new Error("Invalid argument: The given value must be an array.");

        return array.length !== 0 ? array[array.length - 1] : undefined;
    }

    export function normalizeTimeToMinutes(value: Date): Date {
        // Sets seconds and milliseconds to zero.
        return value ? moment(value).startOf("minute").toDate() : null;
    }

    export function isNullOrWhiteSpace(value: string): boolean {
        return !value || value.length === 0 || /^\s*$/.test(value);
    }

    /**
        Sets all empty or whitespace-only strings to null.
     */
    export function whiteSpacePropsToNull(item: any, propInfos: any): void {
        const propNames: string[] = Object.getOwnPropertyNames(propInfos);
        let name: string;
        let value: any;

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
        const dateTime = (moment as any).tz(value, timezone);

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

        const steps = path.split('.');
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
        const first = text.charAt(0).toUpperCase();
        if (first === text.charAt(0))
            return text;

        return first + text.slice(1);
    }

    // Source: http://stackoverflow.com/questions/728360/most-elegant-way-to-clone-a-javascript-object
    export function cloneDeep(obj: any | null | undefined): any {
        if (typeof obj === "undefined" || obj === null || "object" !== typeof obj) return obj;

        let copy: any;

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
            for (let attr in obj) {
                if (obj.hasOwnProperty(attr)) copy[attr] = cloneDeep(obj[attr]);
            }
            return copy;
        }

        throw new Error("Unable to copy the object: its type is not supported.");
    }

    export function fixupDataDeep(data: any) {
        if (!data || typeof data !== "object") return;

        if (Array.isArray(data)) {
            for (let i = 0; i < data.length; i++)
                fixupDataDeep(data[i]);

            return;
        }

        // Parse time members by convention: time members end with "On" and do not start with "Is".
        let value: any;
        for (const member of Object.keys(data)) {
            value = data[member];
            if (typeof value === "string" && value) {
                if ((member.endsWith("On") || member.endsWith("Time")) && !member.startsWith("Is")) {
                    data[member] = new Date(value);
                }
            } else if (typeof value === "object")
                fixupDataDeep(value);
        }
    }

    // TODO: REMOVE
    /*
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
    */

    // TODO: Not used.
    export function parseURL(url: string): any {
        // Source: http://www.abeautifulsite.net/parsing-urls-in-javascript/

        const parser = document.createElement('a');
        const searchObject: any = {};

        // Let the browser do the work
        parser.href = url;

        // Convert query string to object        
        const queries = parser.search.replace(/^\?/, '').split('&');
        let split;
        for (let i = 0; i < queries.length; i++) {
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

    const _userAgent = {
        isEdge: navigator.userAgent.toLowerCase().indexOf('edge') > -1,
        isChrome: navigator.userAgent.toLowerCase().indexOf('chrome') > -1,
        isSafari: navigator.userAgent.toLowerCase().indexOf('safari') > -1
    }

    // Source: http://pixelscommander.com/en/javascript/javascript-file-download-ignore-content-type/
    export function downloadFile(url: string, fileName: string): boolean {

        // iOS devices do not support downloading. We have to inform user about this.
        if (/(iP)/g.test(navigator.userAgent)) {
            alert('Your device do not support files downloading. Please try again in desktop browser.');
            return false;
        }

        // If in Chrome or Safari - download via virtual link click
        if (_userAgent.isChrome || _userAgent.isSafari) {
            // Creating new link node.
            const link = document.createElement('a');
            link.href = url;

            if (link.download !== undefined) {
                // Set HTML5 download attribute. This will prevent file from opening if supported.
                // const fileName = sUrl.substring(sUrl.lastIndexOf('/') + 1, sUrl.length);
                link.download = fileName;
            }

            //Dispatching click event.
            if (document.createEvent) {
                const e = document.createEvent('MouseEvents');
                e.initEvent('click', true, true);
                link.dispatchEvent(e);
                return true;
            }
        }

        // Force file download (whether supported by server).
        const query = '?download';

        window.open(url + query, '_self');

        return true;
    }

    // TODO: ELIMINATE usage
    export function _tryCleanupHtml(text: string) {
        try {
            const root = (new DOMParser()).parseFromString(text, "text/html").documentElement;
            const body = Array.from(root.children).find(x => x.localName === "body");
            if (body) {
                _tryCleanupHtmlCore(body);

                const html = body.innerHTML.replace(/\n/g, "").trim();

                return { ok: true, html: html };
            }

            return { ok: false };
        }
        catch (err) {
            return { ok: false };
        }
    }
    // TODO: ELIMINATE usage
    function _tryCleanupHtmlCore(parent: Element) {
        if (!parent.childNodes || !parent.childNodes.length)
            return;

        const toRemove = [];
        let node, name;
        let i: number;

        for (i = 0; i < parent.childNodes.length; i++) {
            node = parent.childNodes[i];
            name = node.localName;
            if (node.nodeType === Node.TEXT_NODE)
                continue;

            if (node.nodeType !== Node.ELEMENT_NODE) {
                toRemove.push(node);
            }
            else if (name === "br" || name === "hr") {
                toRemove.push(node);
            }
            else {
                if (name === "font")
                    node.face = "";

                _tryCleanupHtmlCore(node);
            }
        }

        for (i = 0; i < toRemove.length; i++) {
            parent.removeChild(toRemove[i]);
        }
    }

    export function getUrlParameter(name: string): string {
        return parseUrlParameters()[name];
    }

    export function parseUrlParameters(): Object {
        const result = {};

        let query = window.location.search.substring(1);
        if (!query)
            return result;

        let params = decodeURIComponent(query).split("&");

        for (var i = 0; i < params.length; i++) {
            let nameValuePair = params[i].split("=");
            result[nameValuePair[0]] = nameValuePair[1];
        }

        return result;
    }
}