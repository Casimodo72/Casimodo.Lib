import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core"

type IconType = "add" | "delete" | "cancel" | "clear" | "edit" | "save" | "refresh" |
    "info" | "question" | "logout" | "forward" | "backward" | "star" | "checked" | "ok" |
    "today" | "duration" | "open-dropdown" | "close" | "open-menu" |
    "sort-arrow-upward" | "sort-arrow-downward" |
    "increase-arrow-upward" | "decrease-arrow-downward" | "decrease-all-arrow-downward" |
    "circle"

// Material symbols: https://developers.google.com/fonts/docs/material_symbols

@Component({
    selector: "app-icon",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule],
    // TODO: Find a more sane way to display icons. E.g. if used
    // inline with text: it is a PITA to align stuff nicely.
    styles: [`
        :host { @apply flex items-center }
    `],
    template: `
    <span class="h-full material-symbols-outlined" [ngStyle]="styles()">{{icon()}}</span>
`
})
export class IconComponent {//implements OnChanges {
    readonly state = input(false)
    readonly size = input<string>()
    readonly bold = input(false)
    readonly icon = input.required<string, IconType>(
        {
            alias: "type",
            transform: (value: IconType) => this.#mapIcon(value),
        })

    readonly styles = computed<any>(() => {
        return {
            "font-weight": this.bold() ? "bold" : "",
            "font-size": this.size()
        }
    })

    #mapIcon(type: IconType | undefined | null): string {
        if (!type) return "help_outline"

        switch (type) {
            case "add": return type
            case "delete": return "delete_outline"
            case "cancel": return "cancel"
            case "clear": return "clear"
            case "edit": return "edit"
            case "save": return "save"
            case "logout": return "logout"
            case "refresh": return "refresh"
            case "question": return "help_outline"
            case "info": return "info"
            case "forward": return "arrow_forward_ios"
            case "backward": return "arrow_back_ios"
            case "ok": return "done"
            case "today": return "today"
            case "duration": return "timelapse"
            case "open-dropdown": return "keyboard_arrow_down"
            case "open-menu": return "more_horiz"
            case "circle": return "circle"
            case "checked": return this.state() ? "task_alt" : "circle"
            case "star": return this.state() ? "star" : "star_outline"
            case "decrease-arrow-downward":
            case "sort-arrow-downward":
                return "stat_minus_1_outline"
            case "decrease-all-arrow-downward": return "stat_minus_3_outline"
            case "increase-arrow-upward":
            case "sort-arrow-upward":
                return "stat_1_outline"
            default: return "help_outline"
        }
    }
}
