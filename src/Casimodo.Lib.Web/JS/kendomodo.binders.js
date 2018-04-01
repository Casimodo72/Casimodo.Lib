"use strict";

// visibleWithFade
kendo.data.binders.visibleWithFade = kendo.data.Binder.extend({
    refresh: function () {
        var value = this.bindings.visibleWithFade.get();
        var $elem = $(this.element);
        if (value === $elem.is(":visible"))
            return;

        var speed = $elem.data("fade-speed") || 400;

        if (value) {
            $elem.fadeIn(speed);
        } else {
            $elem.fadeOut(speed);
        }
    }
});

// Kendo binder for GeoAssistant's PersonGender, which is an enum.
kendo.data.binders.personGender = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        var value = this.bindings["personGender"].get();
        $(this.element).html(value
            ? value === "Male" ? "männlich" : "weiblich"
            : "");
    }
});

// Kendo binder for DateTimeOffset/DateTime.
kendo.data.binders.datetime = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        $(this.element).text(kendomodo.toDisplayDateTime(this.bindings["datetime"].get()));
    }
});

// Kendo binder for date.
kendo.data.binders.date = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        $(this.element).text(kendomodo.toDisplayDate(this.bindings["date"].get()));
    }
});

// Kendo binder for time.
kendo.data.binders.time = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        $(this.element).text(kendomodo.toDisplayTime(this.bindings["time"].get()));
    }
});

// Kendo binder for boolean. Displays "Yes", "No" or "";
kendo.data.binders.yesno = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        $(this.element).text(casimodo.toDisplayBool(this.bindings["yesno"].get()));
    }
});

// Kendo textToHtml binder.
kendo.data.binders.textToHtml = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        $(this.element).html(kendo.htmlEncode(this.bindings["textToHtml"].get()));
    }
});

// Kendo css class binder. Source: https://gist.github.com/rally25rs/d37693f8a6c78386b3ed
kendo.data.binders.class = kendo.data.Binder.extend({
    init: function (target, bindings, options) {
        kendo.data.Binder.fn.init.call(this, target, bindings, options);

        // get list of class names from our complex binding path object
        this._lookups = [];
        for (var key in this.bindings.class.path) {
            this._lookups.push({
                key: key,
                path: this.bindings.class.path[key]
            });
        }
    },

    refresh: function () {
        var lookup,
                value;

        for (var i = 0; i < this._lookups.length; i++) {
            lookup = this._lookups[i];

            // set the binder's path to the one for this lookup,
            // because this is what .get() acts on.
            this.bindings.class.path = lookup.path;
            value = this.bindings.class.get();

            // add or remove CSS class based on if value is truthy
            if (value) {
                $(this.element).addClass(lookup.key);
            } else {
                $(this.element).removeClass(lookup.key);
            }
        }
    }
});
