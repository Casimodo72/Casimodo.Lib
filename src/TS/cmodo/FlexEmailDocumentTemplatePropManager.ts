namespace cmodo {

    export interface FlexTemplate {
        PropertiesText: string;
        EmailPropertiesText: string;
    }

    export interface FlexTemplateProp {
        kind: string;
        name: string;
        value: string;
        isInput: boolean;
        type: string | null;
        label: string | null;
    }

    export class FlexEmailDocumentTemplatePropManager {
        props: FlexTemplateProp[];
        emailProps: FlexTemplateProp[];

        constructor() {
            this.props = [];
            this.emailProps = [];
        }

        getValue(name: string): any {
            return this._getAnyValue(this.props, name);
        }

        getEmailValue(name: string): any {
            return this._getAnyValue(this.emailProps, name);
        }

        private _getAnyValue(props: FlexTemplateProp[], name: string): any {
            for (let i = 0; i < props.length; i++) {
                if (props[i].name === name)
                    return props[i].value;
            }

            return null;
        }

        setTemplate(template: FlexTemplate): void {
            this.props = this.parse(template.PropertiesText);
            this.emailProps = this.parse(template.EmailPropertiesText);
        }

        getInputs(): FlexTemplateProp[] {
            return this.props.filter(x => x.isInput);
        }

        parse(text: string): FlexTemplateProp[] {
            var props: FlexTemplateProp[] = [];

            if (cmodo.isNullOrWhiteSpace(text))
                return props;

            var elements = (new DOMParser()).parseFromString("<root>" + text + "</root>", "text/xml").documentElement.children;
            var elem, name, value, kind;
            for (let i = 0; i < elements.length; i++) {
                elem = elements[i];
                if (elem.nodeName !== "prop")
                    continue;

                name = elem.getAttribute("name");
                if (cmodo.isNullOrWhiteSpace(name))
                    continue;

                value = elem.innerHTML;

                kind = elem.getAttribute("kind");

                var item: FlexTemplateProp = {
                    name: name,
                    value: value,
                    kind: kind,
                    isInput: false,
                    type: null,
                    label: null
                };

                if (elem.getAttribute("input") === "true") {
                    item.isInput = true;
                    item.type = elem.getAttribute("type") || "string";
                    item.label = elem.getAttribute("label") || name;
                }

                props.push(item);
            }

            return props;
        }
    }
}