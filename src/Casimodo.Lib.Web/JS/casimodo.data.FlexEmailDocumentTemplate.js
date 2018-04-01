"use strict";
var casimodo;
(function (casimodo) {
    (function (data) {

        var FlexEmailDocumentTemplatePropManager = function () {
            var self = this;

            this.props = [];
            this.emailProps = [];
            this.documentProps = [];
            this.documentContentProps = [];

            this.getValue = function (name) {
                return _getAnyValue(self.props, name);
            };

            this.getEmailValue = function (name) {
                return _getAnyValue(self.emailProps, name);
            };

            this.getDocumentValue = function (name) {
                return _getAnyValue(self.documentProps, name);
            };

            this.getDocumentContentValue = function (name) {
                return _getAnyValue(self.documentContentProps, name);
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
                self.documentProps = self.parse(template.DocumentPropertiesText);
                self.documentContentProps = self.parse(template.DocumentContentPropertiesText);
            };

            this.parse = function (text) {
                var props = [];

                if (String.isNullOrWhiteSpace(text))
                    return props;

                var elements = (new DOMParser()).parseFromString("<root>" + text + "</root>", "text/xml").documentElement.children;
                var i, elem, name, value;
                for (i = 0; i < elements.length; i++) {
                    elem = elements[i];
                    if (elem.nodeName !== "prop")
                        continue;

                    name = elem.getAttribute("name");
                    if (String.isNullOrWhiteSpace(name))
                        continue;

                    value = elem.innerHTML;

                    props.push({ name: name, value: value });
                }

                return props;
            };
        };
        data.FlexEmailDocumentTemplatePropManager = FlexEmailDocumentTemplatePropManager;    

    })(casimodo.data || (casimodo.data = {}));
})(casimodo || (casimodo = {}));