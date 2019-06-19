
namespace kmodo {

    export function findKendoWindow($context: JQuery): kendo.ui.Window {
        const $window = $context.closest("div [data-role=window]"); // TODO: REMOVE: , .k-popup-edit-form');

        return $window.data('kendoWindow');
    }

    export function getDefaultDialogWindowAnimation(): kendo.ui.WindowAnimation {
        return {
            open: {
                effects: "fadeIn",
                duration: 400
            },
            close: {
                effects: "fadeOut",
                duration: 400
            }
        };
    }

    let _modalWindowsCount = 0;

    export function setModalWindowBehavior(wnd: kendo.ui.Window) {
        // Used for setting of opacity the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        wnd.one("open", onModalWindowOpening);
        wnd.one("activate", onModalWindowActivated);
        wnd.one("close", onModalWindowClosed);
    }

    function onModalWindowOpening(e) {
        // Increase model window counter.
        // Used for setting of opacity the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        _modalWindowsCount++;

        if (_modalWindowsCount > 1) {
            $(document.body).addClass("km-opening-modal-window");
        }
    }

    function onModalWindowActivated(e) {
        // Used for setting of opacity of the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        if (_modalWindowsCount > 1) {
            $(document.body).removeClass("km-opening-modal-window");
        }
    }

    function onModalWindowClosed(e) {
        // Decrease model window counter.
        // Used for setting of opacity of the overlay used for modal windows.
        // This avoids an ugly flashing effect when opening modal windows.
        _modalWindowsCount--;
    };
}