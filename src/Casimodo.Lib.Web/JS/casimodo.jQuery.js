"use strict";
var casimodo;
(function (casimodo) {
    (function (jQuery) {
        jQuery.removeContentTextNodes = function ($elem) {
            $elem.contents().filter(function () {
                return this.nodeType === 3;
            }).remove();
        };
    })(casimodo.jQuery || (casimodo.jQuery = {}));
})(casimodo || (casimodo = {}));

$.fn.visible = function () {
    return this.css('visibility', 'visible');
};

$.fn.invisible = function (collapse) {
    if (collapse)
        return this.css('visibility', 'collapse');
    else
        return this.css('visibility', 'hidden');
};

$.fn.listHandlers = function (events) {
    this.each(function (i) {
        var elem = this,
               // dEvents = $(this).data('events');
        dEvents = $._data($(this).get(0), "events");
        if (!dEvents) { return; }
        $.each(dEvents, function (name, handler) {
            if ((new RegExp('^(' + (events === '*' ? '.+' : events.replace(',', '|').replace(/^on/i, '')) + ')$', 'i')).test(name)) {
                $.each(handler,
                        function (i, handler) {
                            //console.info(elem);
                            console.info(elem, '\n' + i + ': [' + name + '] : ' + handler.handler);
                        });
            }
        });
    });
};

// KABU TODO: REMOVE? Couldn't find a place where we use this anymore.
if (typeof jQuery.fn.reverse !== 'function') {
    jQuery.fn.reverse = function () {
        var list = $(this.get().reverse());
        return list;
    };
}