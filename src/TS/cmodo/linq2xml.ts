
namespace cmodo {

    abstract class XContainer {
        abstract elements(name: string): Enumerable.IEnumerable<XElement>;
        abstract elem(name: string): XElement;

        static _getElements(element: Element,
            name: string,
            first: boolean,
            required: boolean): Enumerable.IEnumerable<XElement> {

            const xelements: XElement[] = [];
            let elem: Element;
            for (let i = 0; i < element.children.length; i++) {
                elem = element.children[i];

                if (name != null && elem.localName !== name)
                    continue;

                xelements.push(XElement._create(elem));

                if (first)
                    break;
            }

            if (required && name !== null && xelements.length === 0) {
                throw new Error(`Element '${name}' not found in children of element '${element.localName}'.`);
            }

            return Enumerable.from(xelements);
        }
    }

    export class XDocument extends XContainer {
        private _doc: Document;

        static parse(xml: string): XDocument {
            return XDocument.from(new DOMParser().parseFromString(xml, "application/xml"));
        }

        static from(document: Document): XDocument {
            const xdoc = new XDocument();
            xdoc._doc = document;
            return xdoc;
        }

        documentElement(): XElement {
            return XElement._create(this._doc.documentElement);
        }

        // override (XContainer)
        elements(name: string = null): Enumerable.IEnumerable<XElement> {
            return XContainer._getElements(this._doc.documentElement, name, false, false);
        }

        // override (XContainer)
        elem(name: string, required: boolean = false): XElement {
            return XContainer._getElements(this._doc.documentElement, name, true, required).firstOrDefault();
        }
    }

    export class XElement extends XContainer {
        private _elem: Element;

        static _create(elem: Element): XElement {
            const xelem = new XElement();
            xelem._elem = elem;
            return xelem;
        }

        // override (XContainer)
        elements(name: string = null): Enumerable.IEnumerable<XElement> {
            return XContainer._getElements(this._elem, name, false, false);
        }

        // override (XContainer)
        elem(name: string, required: boolean = false): XElement {
            return XContainer._getElements(this._elem, name, true, required).firstOrDefault();
        }

        elemValue(name: string, required: boolean = false): string {
            const xelem = XContainer._getElements(this._elem, name, true, required).firstOrDefault();
            if (!xelem)
                return null;

            return xelem._elem.nodeValue;
        }

        private _getAttributeValue(name: string, required: boolean = true): string {
            const value = this._elem.getAttribute(name);
            if (value !== null)
                return value;

            if (required) {
                throw new Error(`Attribute '${name}' not found on element '${this._elem.localName}'.`);
            }

            return null;
        }

        attrValue(name: string, required: boolean = true) {
            return this._getAttributeValue(name, required);
        }

        attrAsNumber(name: string, required: boolean = true) {
            const value = this._getAttributeValue(name, required);
            if (!value)
                return null;

            return Number.parseFloat(value);
        }

        attrAsBool(name: string, required: boolean = true) {
            const value = this._getAttributeValue(name, required);
            if (!value)
                return null;

            return value === "true" ? true : false;
        }
    }
}