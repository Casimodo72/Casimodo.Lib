
namespace cmodo {
    export function jQueryRemoveContentTextNodes($elem: JQuery) {
        $elem.contents().filter((index: number, element: Element) => {
            return element.nodeType === 3;
        }).remove();
    }
}

$.fn.extend({
    visible: function (): JQuery {
        return this.css('visibility', 'visible');
    }
});

$.fn.extend({
    invisible: function (collapse: boolean): JQuery {
        if (collapse)
            return this.css('visibility', 'collapse');
        else
            return this.css('visibility', 'hidden');
    }
});

// TODO: REMOVE?
//$.fn.visible = function () {
//    return this.css('visibility', 'visible');
//};

//$.fn.invisible = function (collapse) {
//    if (collapse)
//        return this.css('visibility', 'collapse');
//    else
//        return this.css('visibility', 'hidden');
//};
