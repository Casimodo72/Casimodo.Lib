
// Kendo MVVM binders ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// visibleWithFade
(kendo.data as any).binders.visibleWithFade = kendo.data.Binder.extend({
    refresh: function () {
        let self = this as any;
        let value = self.bindings.visibleWithFade.get();
        let $elem = $(self.element);
        if (value === $elem.is(":visible"))
            return;

        let speed = $elem.data("fade-speed") || 400;

        if (value) {
            $elem.fadeIn(speed);
        } else {
            $elem.fadeOut(speed);
        }
    }
});

// Kendo binder for GeoAssistant's PersonGender, which is an enum.
(kendo.data as any).binders.personGender = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        let value = self.bindings["personGender"].get();
        $(self.element).html(value
            ? value === "Male" ? "männlich" : "weiblich"
            : "");
    }
});

// Kendo binder for human readable event recurrence rule.
// Uses rrule.js.
(kendo.data as any).binders.recurrenceRuleText = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        let recurrenceRule = self.bindings["recurrenceRuleText"].get();
        if (!recurrenceRule)
            recurrenceRule = "FREQ=DAILY;INTERVAL=6";
        let value = "";
        if (recurrenceRule)
            value = (window as any).rrule.RRule.fromString(recurrenceRule).toText();

        $(self.element).text(value);
    }
});

// Kendo binder for DateTimeOffset/DateTime.
(kendo.data as any).binders.datetime = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        $(self.element).text(kmodo.toDisplayDateTime(self.bindings["datetime"].get()));
    }
});

// Kendo binder for date.
(kendo.data as any).binders.date = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        $(self.element).text(kmodo.toDisplayDate(self.bindings["date"].get()));
    }
});

// Kendo binder for time.
(kendo.data as any).binders.time = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        $(self.element).text(kmodo.toDisplayTime(self.bindings["time"].get()));
    }
});

// Kendo binder for boolean. Displays "Yes", "No" or "";
(kendo.data as any).binders.yesno = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        $(self.element).text(cmodo.toDisplayBool(self.bindings["yesno"].get()));
    }
});

// Kendo textToHtml binder.
(kendo.data as any).binders.textToHtml = kendo.data.Binder.extend({
    init: function (element, bindings, options) {
        kendo.data.Binder.fn.init.call(this, element, bindings, options);
    },
    refresh: function () {
        let self = this as any;
        $(self.element).html(kendo.htmlEncode(self.bindings["textToHtml"].get()));
    }
});

// Kendo css class binder. Source: https://gist.github.com/rally25rs/d37693f8a6c78386b3ed
(kendo.data as any).binders.class = kendo.data.Binder.extend({
    init: function (target, bindings, options) {
        kendo.data.Binder.fn.init.call(this, target, bindings, options);
        let self = this as any;
        // get list of class names from our complex binding path object
        self._lookups = [];
        for (let key in self.bindings.class.path) {
            self._lookups.push({
                key: key,
                path: self.bindings.class.path[key]
            });
        }
    },

    refresh: function () {
        let lookup,
            value;
        let self = this as any;
        for (let i = 0; i < self._lookups.length; i++) {
            lookup = self._lookups[i];

            // set the binder's path to the one for this lookup,
            // because this is what .get() acts on.
            self.bindings.class.path = lookup.path;
            value = self.bindings.class.get();

            // add or remove CSS class based on if value is truthy
            if (value) {
                $(self.element).addClass(lookup.key);
            } else {
                $(self.element).removeClass(lookup.key);
            }
        }
    }
});

// Kendo ListView selectedItem binder. Source: https://www.telerik.com/forums/two-way-binding-for-selected-item-in-listview
(kendo.data as any).binders.widget.selectedItem = kendo.data.Binder.extend({
    init: function (widget, bindings, options) {
        let self = this as any;
        //call the base constructor
        kendo.data.Binder.fn.init.call(this, widget.element[0], bindings, options);

        //listen for the change event of the widget
        $(self.element).data("kendoListView").bind("change", function (e) {
            self.change(e); //call the change function
        });
    },
    refresh: function () {
        let self = this as any,
            value = self.bindings.selectedItem.get(), //get the value from the View-Model
            listView = $(self.element).data("kendoListView"),
            row;

        if (value) {
            row = listView.items().filter("[data-uid='" + value.uid + "']");
        }

        if (row && row.length) { //update the widget
            listView.select(row);
        } else {
            listView.clearSelection();
        }

    },
    change: function (e) {
        let self = this as any;
        let listView = $(self.element).data("kendoListView"),
            selectedRow = listView.select(),
            item;

        item = listView.dataSource.getByUid(selectedRow.data("uid"));
        self.bindings.selectedItem.set(item); //update the ViewModel
    }
});

// Localization overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

kendo.ui.Grid.prototype.options.messages.commands.update = "Speichern";

kendo.ui.Pager.prototype.options.messages.allPages = "Alle";
kendo.ui.Pager.prototype.options.messages.display = "{0} - {1} von {2}";

(kendo.ui as any).FilterCell.prototype.options.messages.isFalse = "nein";
(kendo.ui as any).FilterCell.prototype.options.messages.isTrue = "ja";

// Validator overrides

$.extend(true, (kendo.ui as any).validator, {
    rules: {
        // This fixes Kendo's - in my eyes - incorrect handling of booleans on complex properties.
        // See: http://www.telerik.com/forums/using-kendo-validator-with-mvc-model-properties
        mvcrequired: function (input) {
            if (input.filter("[data-val-required]").length) {
                let value = input.val();
                return !(value === "" || !value);
            }
            return true;
        }
    },
    messages: {
        required: (input) => kendo.format("\"{0}\" ist erforderlich.", input.attr("data-display-name")),
        pattern: "{0} ist ungültig",
        min: "{0} muss größer oder gleich sein als {1}",
        max: "{0} muss kleiner oder gleich sein als {1}",
        step: "{0} ist ungültig",
        email: "{0} ist keine gültige E-Mail",
        url: "{0} ist keine gültige URL",
        date: "{0} ist kein gültiges Datum",
        dateCompare: 'End date should be greater than or equal to the start date',
        mvcrequired: function (input) {
            return input.attr("data-val-required");
        }
    }
});
