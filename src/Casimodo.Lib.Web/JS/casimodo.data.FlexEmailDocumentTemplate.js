"use strict";
var casimodo;
(function (casimodo) {
    (function (data) {

        var FlexEmailDocumentTemplatePropManager = function () {
            var self = this;

            this.props = [];
            this.emailProps = [];

            this.getValue = function (name) {
                return _getAnyValue(self.props, name);
            };

            this.getEmailValue = function (name) {
                return _getAnyValue(self.emailProps, name);
            };

            function _getAnyValue(props, name) {
                for (var i = 0; i < props.length; i++) {
                    if (props[i].name === name)
                        return props[i].value;
                }

                return null;
            }

            this.setTemplate = function (template) {
                self.props = self.parse(template.PropertiesText);
                self.emailProps = self.parse(template.EmailPropertiesText);
            };

            this.getInputs = function () {
                return self.props.filter(x => x.isInput);
            };

            this.parse = function (text) {
                var props = [];

                if (String.isNullOrWhiteSpace(text))
                    return props;

                var elements = (new DOMParser()).parseFromString("<root>" + text + "</root>", "text/xml").documentElement.children;
                var i, elem, name, value, type, label, kind;
                for (i = 0; i < elements.length; i++) {
                    elem = elements[i];
                    if (elem.nodeName !== "prop")
                        continue;

                    name = elem.getAttribute("name");
                    if (String.isNullOrWhiteSpace(name))
                        continue;

                    value = elem.innerHTML;

                    kind = elem.getAttribute("kind");

                    var item = { name: name, value: value, kind: kind };

                    if (elem.getAttribute("input") === "true") {
                        item.isInput = true;
                        item.type = elem.getAttribute("type") || "string";
                        item.label = elem.getAttribute("label") || name;
                    }

                    props.push(item);
                }

                return props;
            };
        };
        data.FlexEmailDocumentTemplatePropManager = FlexEmailDocumentTemplatePropManager;

    })(casimodo.data || (casimodo.data = {}));
})(casimodo || (casimodo = {}));