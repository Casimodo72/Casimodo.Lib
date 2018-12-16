(function () {

    // See also MultiDayView example: http://stackoverflow.com/questions/19487913/change-the-width-of-the-events-in-a-kendo-ui-scheduler

    let extend = $.extend;
    let extendedOptions = {
        hideTimesHeader: false
    };

    const EVENT_WRAPPER_STRING =
        '<div role="gridcell" aria-selected="false" ' +
        'data-#=ns#uid="#=uid#"' +
            'class="k-event"' +
        '>' +
        '{0}' +
        '</div>';

    let CustomTimelineView = (kendo.ui as any).TimelineWeekView.extend({

        init: function (element, options) {
            var that = this;

            (kendo.ui as any).TimelineWeekView.fn.init.call(that, element, options);
        },

        options: {
            title: "X Woche",
            selectedDateFormat: "{0:ddd dd.MM} - {1:ddd dd.MM}",
            selectedShortDateFormat: "{0:ddd dd.MM} - {1:ddd dd.MM}",
            dateHeaderTemplate: kendo.template("<span>#=kendo.format('{0:ddd dd.MM}', date)#</span>"),
            majorTick: 120
        },

        _templates: function () {
            var options = this.options,
                settings = extend({}, (kendo as any).Template, options.templateSettings);

            this.eventTemplate = this._eventTmpl(options.eventTemplate, EVENT_WRAPPER_STRING);
            this.majorTimeHeaderTemplate = kendo.template(options.majorTimeHeaderTemplate, settings);
            this.dateHeaderTemplate = kendo.template(options.dateHeaderTemplate, settings);
            this.slotTemplate = kendo.template(options.slotTemplate, settings);
            this.groupHeaderTemplate = kendo.template(options.groupHeaderTemplate, settings);
        },

        _positionEvent: function (eventObject) {

            var snap = true; // was: false

            var isSmallWeekendEnabled: boolean = (cmodo.run as any).schedulerOptions.isSmallWeekendEnabled;

            var isWeekend = false;
            if (isSmallWeekendEnabled) {
                // Strange: The eventObject.start is not a Date anymore in my new laptop environment.
                var start = typeof eventObject.start === "number" ? new Date(eventObject.start) : eventObject.start;

                var dayIndex = start.getDay();
                isWeekend = dayIndex === 6 || dayIndex === 7;

                // KABU TOOD: REMOVE: Not needed.
                //if (isWeekend) {
                //    var endSlot = eventObject.slotRange.end;
                //    endSlot.clientWidth = 60;
                //    endSlot.offsetWidth = 60;
                //}  
            }

            var rect = eventObject.slotRange.innerRect(eventObject.start, eventObject.end, snap);

            var left = this._adjustLeftPosition(rect.left);

            var width = rect.right - rect.left - 2;

            if (width < 0) {
                width = 0;
            }

            if ((!isSmallWeekendEnabled || !isWeekend) && width < this.options.eventMinWidth) {
                var slotsCollection = eventObject.slotRange.collection;
                var lastSlot = slotsCollection._slots[slotsCollection._slots.length - 1];
                var offsetRight = lastSlot.offsetLeft + lastSlot.offsetWidth;

                width = this.options.eventMinWidth;

                if (offsetRight < left + width) {
                    width = offsetRight - rect.left - 2;
                }
            }

            var eventHeight = this.options.eventHeight + 1;

            eventObject.element.css({
                //borderWidth: "1px", borderStyle: "solid", borderColor: "red",
                top: eventObject.slotRange.start.offsetTop + eventObject.rowIndex * (eventHeight) + "px",
                left: left,
                width: width
            });

            if (eventObject.rowIndex) {
                eventObject.element.css({ borderTopWidth: "1px" });
            }
        },

        _setRowsHeight: function (eventGroups, groupsCount, maxRowCount) {
            var eventHeight = this.options.eventHeight + 2;
            var eventBottomOffset = this._getBottomRowOffset();
            var isVerticallyGrouped = this._isVerticallyGrouped();

            groupsCount = isVerticallyGrouped ? groupsCount : 1;

            for (var groupIndex = 0; groupIndex < groupsCount; groupIndex++) {
                var eventGroup = eventGroups[groupIndex];
                //var eventsCount = eventGroup.maxRowCount;
                var rowsCount = isVerticallyGrouped ? eventGroup.maxRowCount : maxRowCount;

                //rowsCount = rowsCount ? rowsCount : 1;

                // If there are not events in this group, then don't set the height.
                if (rowsCount) {
                    var rowHeight = ((eventHeight) * rowsCount) + eventBottomOffset;

                    if (!extendedOptions.hideTimesHeader) {
                        var timesRow = $(this.times.find("tr")[groupIndex]);
                        timesRow.height(rowHeight);
                    }

                    var row = $(this.content.find("tr")[groupIndex]);
                    row.height(rowHeight);
                }
            }

            this._setContentWidth();
            this.refreshLayout();
            this._refreshSlots();
        },

        _getBottomRowOffset: function () {
            return 0;
        }

    });

    // Extend Kendo UI
    extend(true, kendo.ui, {
        CustomTimelineView: CustomTimelineView
    });

})();

