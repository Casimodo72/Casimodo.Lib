namespace kmodo {

    export interface CompanySelectorOptions {
        changed?: (companyId: string) => void,
        companyId?: string;
    }

    export function createCompanySelector($elem: JQuery, options?: CompanySelectorOptions): kendo.ui.DropDownList {

        let opts: kendo.ui.DropDownListOptions = {
            height: 500,
            minLength: 1,
            // KABU TODO: Current kendo version has no clearButton.
            //clearButton: true,
            filter: "startswith",
            optionLabel: {
                Id: null,
                NameShortest: ""
            },
            optionLabelTemplate: "<div></div>",
            dataValueField: "Id",
            dataTextField: "NameShortest",
            autoBind: true,
            valuePrimitive: false,
            valueTemplate: "<div class='selected-value' style='margin-left:6px;display:flex'><div style='width: 15px; height: 15px; margin-top: 5px; margin-right: 6px; background-color: #:Color#'></div>#:NameShortest#</div>'",
            template: "<div style='display:flex'><div style='width:15px;height:15px;margin-top:4px;margin-right:6px;background-color: #:Color#'></div>#:NameShortest#</div>",
            dataBound: function (e) {
                // Set initial CompanyId if provided.
                if (options && options.companyId)
                    e.sender.value(options.companyId);
                // this.trigger("change");
            },
            dataSource: {
                type: "odata-v4",
                schema: { model: { id: 'Id' } },
                transport: {
                    parameterMap: function (data, type) { return kmodo.parameterMapForOData(data, type, null); },
                    read: {
                        url: "/odata/Companies/Query()?$select=Id,NameShortest,Color&$orderby=NameShortest",
                        dataType: "json"
                    }
                },
                pageSize: 0,
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true
            } as kendo.data.DataSourceOptions,
            change: function (e) {
                if (options && options.changed) {
                    options.changed(e.sender.dataItem().Id);
                }
            }
        };

        return $elem.kendoDropDownList(opts).data("kendoDropDownList") as kendo.ui.DropDownList;
    }

    export interface MoTagFilterSelectorOptions {
        filters?: kendo.data.DataSourceFilters | kendo.data.DataSourceFilterItem,
        changed?: (tagIds: string[]) => void,
        autoBind?: boolean;
    }

    export function createMoTagFilterSelector($elem: JQuery, options: MoTagFilterSelectorOptions): kendo.ui.MultiSelect {

        let opts: kendo.ui.MultiSelectOptions = {
            // TODO: Current kendo version has no clearButton.
            // clearButton: true,
            autoClose: false,
            dataValueField: "Id",
            dataTextField: "DisplayName",
            autoBind: options.autoBind,
            dataSource: {
                type: "odata-v4",
                schema: { model: { id: 'Id' } },
                transport: {
                    parameterMap: function (data, type) { return kmodo.parameterMapForOData(data, type, null); },
                    read: {
                        url: "/odata/MoTags/Query()?$select=Id,DisplayName",
                        dataType: "json"
                    }
                },
                filter: options.filters,
                pageSize: 0,
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true
            },
            change: (e) => {
                if (options.changed) {
                    let ids = e.sender.dataItems().map(x => x.Id) as string[];
                    options.changed(ids);
                }
            }
        };

        return $elem.kendoMultiSelect(opts).data("kendoMultiSelect") as kendo.ui.MultiSelect;
    }

    export function enableContextMenuItems2(e: kendo.ui.ContextMenuOpenEvent, names: string | string[], enabled: boolean): void {
        let $items = getContextMenuItems2(e, names);
        e.sender.enable($items, enabled);
        if (enabled)
            $items.show(); //.removeClass("k-hidden");
        else
            $items.hide(); //.addClass("k-hidden");
    }

    function getContextMenuItems2(e: kendo.ui.ContextMenuOpenEvent, names: string | string[]): JQuery {
        let namesArray: string[];
        if (typeof names === "string")
            namesArray = names.split(",").map(function (x) { return x.trim(); });
        else
            namesArray = names;

        let query = "";
        for (let i = 0; i < namesArray.length; i++) {
            if (i > 0)
                query += ", ";
            query += "li[data-name='" + namesArray[i] + "']";
        }

        return $(e.item).find(query);
    }

    export function enableContextMenuItems(menu: kendo.ui.ContextMenu, names: string | string[], enabled: boolean): void {
        let $items = getContextMenuItems(menu, names);
        menu.enable($items, enabled);
        if (enabled)
            $items.show(); //.removeClass("k-hidden");
        else
            $items.hide(); //.addClass("k-hidden");
    }

    function getContextMenuItems(menu: kendo.ui.ContextMenu, names: string | string[]): JQuery {
        let namesArray: string[];
        if (typeof names === "string")
            namesArray = names.split(",").map(function (x) { return x.trim(); });
        else
            namesArray = names;

        let query = "";
        for (let i = 0; i < namesArray.length; i++) {
            if (i > 0)
                query += ", ";
            query += "li[data-name='" + namesArray[i] + "']";
        }

        return menu.element.find(query);
    }

    export function useHeaderRequestVerificationToken(xhr: XMLHttpRequest): void {
        if (xhr) {
            let requestVerficatonToken = $(':input[name="__RequestVerificationToken"]').first().val();
            if (requestVerficatonToken) {
                xhr.addEventListener("readystatechange", function (e) {
                    if (xhr.readyState == 1 /* OPENED */) {
                        xhr.setRequestHeader("RequestVerificationToken", requestVerficatonToken);
                    }
                });
            }
        }
    }

    export function kendoDataSourceODataErrorHandler(e: kendo.data.DataSourceErrorEvent): void {

        const message = cmodo.getODataErrorMessageFromJQueryXHR(e.xhr);

        cmodo.showError(message);

        // KABU TODO: ELIMINATE and move into the view models.
        const $errorBox = $(".km-validation-errors-box").first();
        if ($errorBox.length) {
            $errorBox.empty();
            const template = kendo.template("<li>#:message #</li>");
            $errorBox.append(template({
                message: message
            }));
        }
    }

    // KABU TODO: REMOVE? KEEP: maybe we can use this in the future.
    /*
    export function getShowPhotoCellTemplate(uri: string): string {
        return "<a class='kendomodo-button-show-image k-button' href='#' data-file-uri='" + uri + "'><span class='casimodo-icon-show-image'></span></a>";
        // return "<div data-file-uri='" + uri + "' class='kendomodo-button-show-image'>&nbsp;</div>";
    }
    */

    export function toggleButton($btn: JQuery, active: boolean): void {
        if (active)
            $btn.addClass("km-active-toggle-button");
        else
            $btn.removeClass("km-active-toggle-button");
        $btn.removeClass("k-state-focused");
    }

    export function toggleScopeOption(kendoEvent: kendo.ViewEvent, scope: kendo.data.ObservableObject, propName: string): any {
        // KABU TODO: Eval which type of event we actually get here.
        kendoEvent.preventDefault();
        let $elem = kmodo.getEventTarget(kendoEvent);
        let value = !!scope[propName];
        value = !value;
        scope.set(propName, value);
        kmodo.toggleButton($elem, value);

        return value;
    }

    export function getEventTarget(e: any): JQuery {
        return $(e.currentTarget || e.target || e.sender || null);
    }

    export function toDisplayDateTime(value: string): string {
        return value ? kendo.toString(new Date(value), "dd.MM.yyyy  HH:mm") : null;
    }

    export function toDisplayDate(value: string): string {
        return value ? kendo.toString(new Date(value), "dd.MM.yyyy") : null;
    }

    export function toDisplayTime(value: string): string {
        return value ? kendo.toString(new Date(value), "HH:mm") : null;
    }

    export function guid(): string {
        return kendo.guid();

        // KABU TODO: Maybe use instead: http://slavik.meltser.info/?p=142
        /*
        function _p8(s) {
            let p = (Math.random().toString(16) + "000000000").substr(2, 8);
            return s ? "-" + p.substr(0, 4) + "-" + p.substr(4, 4) : p;
        }
        return _p8() + _p8(true) + _p8(true) + _p8();
        */
    }

    export function last(array: kendo.data.ObservableArray): any {
        if (!array)
            return undefined;

        return array.length !== 0 ? array[array.length - 1] : undefined;
    }

    export function extendDeep(target: any, source: any): any {
        let sourceValue;
        for (let prop in source) {
            if (!source.hasOwnProperty(prop))
                continue;

            sourceValue = source[prop];

            if (!target.hasOwnProperty(prop))
                target[prop] = sourceValue;
            else if (typeof sourceValue === "object") {
                if (Array.isArray(sourceValue))
                    throw new Error("Extending arrays is not supported.");

                target[prop] = extendDeep(target[prop], sourceValue);
            }
        }

        return target;
    }
}