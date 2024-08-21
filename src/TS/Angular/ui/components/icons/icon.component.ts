import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core"
import { MatIcon } from "@angular/material/icon"

import { IconType } from "./iconType"
// Material symbols: https://developers.google.com/fonts/docs/material_symbols

@Component({
    selector: "app-icon",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIcon],
    // TODO: Find a more sane way to display icons. E.g. if used
    // inline with text: it is a PITA to align stuff nicely.
    styles: [`
        :host { display: inline-block; }
    `],
    template: `
    <mat-icon class="material-symbols-outlined" style="vertical-align: middle" [ngStyle]="styles()">{{icon()}}</mat-icon>
`
})
export class IconComponent {
    readonly state = input(false)
    readonly size = input<string>()
    readonly bold = input(false)
    readonly type = input.required<IconType>()
    // readonly icon = input.required<string, IconType>(
    //     {
    //         alias: "type",
    //         transform: (value: IconType) => this.#mapIcon(value),
    //     })

    protected readonly icon = computed(() => this.#mapIcon(this.type(), this.state()))

    readonly styles = computed<any>(() => {
        return {
            "font-weight": this.bold() ? "bold" : "",
            "font-size": this.size(),
            "height": this.size(),
        }
    })

    #mapIcon(type: IconType | undefined | null, state: boolean): string {
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
            case "open-dropdown":
            case "expander-open":
                return "keyboard_arrow_down"
            case "expander-close": return "keyboard_arrow_up"
            case "open-menu": return "more_horiz"
            case "circle": return "circle"
            case "checked": return state ? "task_alt" : "circle"
            case "star": return state ? "star" : "star_outline"
            case "decrease-arrow-downward":
            case "sort-arrow-downward":
                return "stat_minus_1_outline"
            case "decrease-all-arrow-downward": return "stat_minus_3_outline"
            case "increase-arrow-upward":
            case "sort-arrow-upward":
                return "stat_1_outline"
            case "search": return "search"
            default: return "help_outline"
        }
    }
}
