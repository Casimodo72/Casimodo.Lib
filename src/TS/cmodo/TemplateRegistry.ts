
namespace cmodo {
    export class TemplateRegistry {
        private compiledTemplates: any = {};
        private templates: any = {};
        public compile: (template: string) => (data: any) => string = null;

        constructor() {
            this.addDefaultTemplates();
        }

        get(name: string): (data: any) => string {

            let compiledTemplateFunc = this.compiledTemplates[name];
            if (typeof compiledTemplateFunc === "function")
                return compiledTemplateFunc;

            const template = this.templates[name];
            if (typeof template === "undefined")
                return _emptyTemplateFunc;

            compiledTemplateFunc = this.compileTemplate(template as string);
            this.compiledTemplates[name] = compiledTemplateFunc;

            return compiledTemplateFunc;
        }

        add(name: string, template: string): void {
            this.templates[name] = template;
        }

        private compileTemplate(template: string): (template: string) => string {
            if (this.compile)
                return this.compile(template);
            else
                return _emptyTemplateFunc;
        }

        private addDefaultTemplates(): void {
            this.add("Empty", "");

            this.add("MoTreeView", `#const isManager=cmodo.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style=''#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#};##if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}#`);

            this.add("MoTreeViewOld", `#const isManager=cmodo.hasMoManagerPermissionOnly(item);if(item.Role==="RecycleBin") {#<span class ='kmodo-icon icon-delete'></span>#} else {#<span#if(isManager) {# style='font-weight:bold'#}#>#if(isManager) {#<span class ="mo-perm-manager">M</span>#}# #:item.Name #</span>#if(item.files.length) {#&nbsp; <sup>#: item.files.length#</sup>#}}#`);

            this.add("AllRowsCheckBoxSelectorGridCell", `#const randomId = cmodo.guid();#<input id='cb-all-#:randomId#' class='k-checkbox all-list-items-selector' type='checkbox' /><label class='k-checkbox-label' for='cb-all-#:randomId#' />`);

            this.add("RowCheckBoxSelectorGridCell", `#const randomId = cmodo.guid();#<input id='cb-#:randomId#' class='k-checkbox list-item-selector' type='checkbox' /><label class='k-checkbox-label list-item-selector' for='cb-#:randomId#' style='display:none'/>`);

            this.add("RowRemoveCommandGridCell", `<div class="list-item-remove-command"><span class="k-icon k-delete"></span></div>`);
        }
    }

    function _emptyTemplateFunc(data: any): string {
        return "";
    }

    export const templates: TemplateRegistry = new TemplateRegistry();
}