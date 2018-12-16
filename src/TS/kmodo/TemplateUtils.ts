
namespace kmodo {

    export function getColorCellTemplate(color: string): string {
        // See http://www.mediaevent.de/css/transparenz.html
        if (!color || cmodo.isNullOrWhiteSpace(color))
            // KABU TODO: Which color to use for null?
            color = "rgba(220, 160, 140, 0.5)";

        return "<div style='width: 30px; background-color: " + color + "'>&nbsp;</div>";
    };
}