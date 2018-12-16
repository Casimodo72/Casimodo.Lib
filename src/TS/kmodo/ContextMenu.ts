
namespace kmodo {
    export function getDefaultContextMenuAnimation(): kendo.ui.ContextMenuAnimation {
        return {
            open: {
                effects: "slideIn:down",
                duration: 72
            }
        };
    }
}