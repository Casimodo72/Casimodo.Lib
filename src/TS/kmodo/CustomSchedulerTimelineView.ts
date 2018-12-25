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
            let that = this;

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
            let options = this.options,
                settings = extend({}, (kendo as any).Template, options.templateSettings);

            this.eventTemplate = this._eventTmpl(options.eventTemplate, EVENT_WRAPPER_STRING);
            this.majorTimeHeaderTemplate = kendo.template(options.majorTimeHeaderTemplate, settings);
            this.dateHeaderTemplate = kendo.template(options.dateHeaderTemplate, settings);
            this.slotTemplate = kendo.template(options.slotTemplate, settings);
            this.groupHeaderTemplate = kendo.template(options.groupHeaderTemplate, settings);
        },

        _positionEvent: function (eventObject) {

            let snap = true; // was: false

            let isSmallWeekendEnabled: boolean = (cmodo.run as any).schedulerOptions.isSmallWeekendEnabled;

            let isWeekend = false;
            if (isSmallWeekendEnabled) {
                // Strange: The eventObject.start is not a Date anymore in my new laptop environment.
                let start = typeof eventObject.start === "number" ? new Date(eventObject.start) : eventObject.start;

                let dayIndex = start.getDay();
                isWeekend = dayIndex === 6 || dayIndex === 7;

                // KABU TOOD: REMOVE: Not needed.
                //if (isWeekend) {
                //    let endSlot = eventObject.slotRange.end;
                //    endSlot.clientWidth = 60;
                //    endSlot.offsetWidth = 60;
                //}  
            }

            let rect = eventObject.slotRange.innerRect(eventObject.start, eventObject.end, snap);

            let left = this._adjustLeftPosition(rect.left);

            let width = rect.right - rect.left - 2;

            if (width < 0) {
                width = 0;
            }

            if ((!isSmallWeekendEnabled || !isWeekend) && width < this.options.eventMinWidth) {
                let slotsCollection = eventObject.slotRange.collection;
                let lastSlot = slotsCollection._slots[slotsCollection._slots.length - 1];
                let offsetRight = lastSlot.offsetLeft + lastSlot.offsetWidth;

                width = this.options.eventMinWidth;

                if (offsetRight < left + width) {
                    width = offsetRight - rect.left - 2;
                }
            }

            let eventHeight = this.options.eventHeight + 1;

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
            let eventHeight = this.options.eventHeight + 2;
            let eventBottomOffset = this._getBottomRowOffset();
            let isVerticallyGrouped = this._isVerticallyGrouped();

            groupsCount = isVerticallyGrouped ? groupsCount : 1;

            for (let groupIndex = 0; groupIndex < groupsCount; groupIndex++) {
                let eventGroup = eventGroups[groupIndex];
                //let eventsCount = eventGroup.maxRowCount;
                let rowsCount = isVerticallyGrouped ? eventGroup.maxRowCount : maxRowCount;

                //rowsCount = rowsCount ? rowsCount : 1;

                // If there are not events in this group, then don't set the height.
                if (rowsCount) {
                    let rowHeight = ((eventHeight) * rowsCount) + eventBottomOffset;

                    if (!extendedOptions.hideTimesHeader) {
                        let timesRow = $(this.times.find("tr")[groupIndex]);
                        timesRow.height(rowHeight);
                    }

                    let row = $(this.content.find("tr")[groupIndex]);
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

