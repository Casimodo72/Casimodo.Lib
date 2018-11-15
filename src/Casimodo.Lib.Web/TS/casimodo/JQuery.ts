
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

//$.fn.visible = function () {
//    return this.css('visibility', 'visible');
//};

//$.fn.invisible = function (collapse) {
//    if (collapse)
//        return this.css('visibility', 'collapse');
//    else
//        return this.css('visibility', 'hidden');
//};

// KABU TODO: REMOVE? Not used anymore.
//$.fn.listHandlers = function (events) {
//    this.each(function (i) {
//        var elem = this,
//            // dEvents = $(this).data('events');
//            dEvents = $._data($(this).get(0), "events");
//        if (!dEvents) { return; }
//        $.each(dEvents, function (name, handler) {
//            if ((new RegExp('^(' + (events === '*' ? '.+' : events.replace(',', '|').replace(/^on/i, '')) + ')$', 'i')).test(name)) {
//                $.each(handler,
//                    function (i, handler) {
//                        //console.info(elem);
//                        console.info(elem, '\n' + i + ': [' + name + '] : ' + handler.handler);
//                    });
//            }
//        });
//    });
//};

// KABU TODO: REMOVE? Couldn't find a place where we use this anymore.
//if (typeof jQuery.fn.reverse !== 'function') {
//    jQuery.fn.reverse = function () {
//        var list = $(this.get().reverse());
//        return list;
//    };
//}
