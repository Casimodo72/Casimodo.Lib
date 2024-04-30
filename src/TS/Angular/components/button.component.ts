import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, input, signal } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { IconComponent } from "./icon.component"
import { CommonModule } from "@angular/common"

type ButtonColor = undefined | "primary" | "accent" | "warn"

type ButtonType = "add" | "delete" | "cancel" | "clear" | "edit" | "save" |
    "refresh" | "question" | "info" | "logout" | "forward" | "backward" | "ok" | "today" |
    "open-dropdown" | "close" | "open-menu"

type ButtonVisual = undefined | "stroked" | "raised" | "icon"

@Component({
    selector: "app-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatButtonModule, IconComponent],
    template: `
@if (!visual() && !effectiveText()) {
    <button mat-icon-button type="button"
        [color]="color()"
        [disabled]="disabled() === true"
        attr.aria-label="{{type()}}"
    >
        @if (type(); as type) {
            <app-icon [type]="type" />
        }
    </button>
}
@else if (!visual() && effectiveText()) {
    <button mat-stroked-button type="button"
        [ngClass]="{ 'w-full': stretch()}"
        [color]="color()"
        [disabled]="disabled() === true"
        attr.aria-label="{{type()}}"
    >
        <div class="flex items-center gap-1">
            @if (type(); as type) {
                <app-icon [type]="type" />
            }
            @if (effectiveText()) {
                <span style="line-height: normal">{{effectiveText()}}</span>
            }
            <ng-container *ngTemplateOutlet="contentOutlet" />
        </div>
    </button>
}
@else if (visual()) {
    <!-- TODO: There's no @case with multiple values in Angular new control flow (yet?)
                See (at the end) https://github.com/angular/angular/issues/14659
        -->
    @switch (visual()) {
        @case (undefined) {
            <button mat-icon-button type="button"
                [color]="color()"
                [disabled]="disabled() === true"
                attr.aria-label="{{type()}}"
            >
                @if (type(); as type) {
                    <app-icon [type]="type" />
                }
            </button>
        }
        @case ("icon") {
            <button mat-icon-button type="button"
                [color]="color()"
                [disabled]="disabled() === true"
                attr.aria-label="{{type()}}"
            >
                @if (type(); as type) {
                    <app-icon [type]="type" />
                }
            </button>
        }
        @case ("stroked") {
            <button mat-stroked-button type="button"
                [color]="color()"
                [disabled]="disabled() === true"
                attr.aria-label="{{type()}}"
            >
                @if (type(); as type) {
                    <app-icon [type]="type" />
                }
                @if (effectiveText()) {
                    <span>{{effectiveText()}}</span>
                }
                <ng-container *ngTemplateOutlet="contentOutlet" />
            </button>
        }
        @case ("raised") {
            <button mat-raised-button type="button"
                [color]="color()"
                [disabled]="disabled() === true"
                attr.aria-label="{{type()}}"
            >
                @if (type(); as type) {
                    <app-icon [type]="type" />
                }
                @if (effectiveText()) {
                    <span>{{effectiveText()}}</span>
                }
                <ng-container *ngTemplateOutlet="contentOutlet" />
            </button>
        }
    }
}
@else {
    <button mat-raised-button type="button"
        [disabled]="disabled() === true"
        attr.aria-label="{{type()}}"
    >
        <span>[TODO: icon settings]</span>
    </button>
}
<ng-template #contentOutlet>
    <div class="flex items-center gap-1">
        <ng-content />
    </div>
</ng-template>
`
})
export class ButtonComponent implements OnChanges {
    readonly type = input<ButtonType | undefined>()
    readonly visual = input<ButtonVisual | undefined>()
    readonly color = input<ButtonColor | undefined>()
    readonly useDefaultText = input<boolean | undefined>()
    readonly text = input<string | null | undefined>()
    readonly stretch = input<boolean | undefined>()
    readonly disabled = input<boolean | undefined>()

    readonly effectiveText = signal("")

    ngOnChanges(changes: SimpleChanges) {
        // TODO: Check if ngOnChanges is still being called when using input signals.
        if (changes["text"]) {
            this.effectiveText.set(changes["text"].currentValue)
        }
        if (changes["useDefaultText"]?.currentValue === true) {
            this.effectiveText.set(this.#getTextByType(this.type()))
        }
    }

    #getTextByType(type: ButtonType | undefined) {
        switch (type) {
            case "add":
                return "Hinzufügen"
            case "delete":
                return "Löschen"
            case "cancel":
                return "Abbrechen"
            case "clear":
                return "Leeren"
            case "edit":
                return "Bearbeiten"
            case "save":
                return "Speichern"
            default:
                return ""
        }
    }
}
