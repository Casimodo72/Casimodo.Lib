import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, signal } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { IconComponent } from "./button-icon.component"
import { CommonModule } from "@angular/common"

type ButtonColor = undefined | "primary" | "accent" | "warn"

type ButtonType = undefined | "add" | "delete" | "cancel" | "clear" | "edit" | "save" |
    "refresh" | "question" | "info" | "logout" | "forward" | "backward" | "ok" | "today" |
    "open-dropdown" | "close" | "open-menu"

type ButtonVisual = undefined | "stroked" | "raised" | "icon"

@Component({
    selector: "app-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatButtonModule, IconComponent],
    template: `
        @if (!visual && !effectiveText()) {
            <button mat-icon-button type="button" [color]="color" [disabled]="disabled"
                attr.aria-label="{{type}}">
                @if (type) {
                    <app-icon [type]="type" />
                }
            </button>
        }
        @else if (!visual && effectiveText()) {
            <button mat-stroked-button type="button" [ngClass]="{ 'w-full': stretch}"
                [color]="color"
                [disabled]="disabled"
                attr.aria-label="{{type}}">
                <div class="flex items-center gap-1">
                    @if (type) {
                        <app-icon [type]="type" />
                    }
                    @if (effectiveText()) {
                        <span style="line-height: normal">{{effectiveText()}}</span>
                    }
                    <ng-container *ngTemplateOutlet="contentOutlet" />
                </div>
            </button>
        }
        @else if (visual) {
             <!-- TODO: There's no @case with multiple values in Angular new control flow (yet?)
                     See (at the end) https://github.com/angular/angular/issues/14659
                -->
            @switch (visual) {
                @case (undefined) {
                    <button mat-icon-button type="button" [color]="color" [disabled]="disabled"
                        attr.aria-label="{{type}}">
                        @if (type) {
                            <app-icon [type]="type" />
                        }
                    </button>
                }
                @case ("icon") {
                    <button mat-icon-button type="button" [color]="color" [disabled]="disabled"
                        attr.aria-label="{{type}}">
                        @if (type) {
                            <app-icon [type]="type" />
                        }
                    </button>
                }
                @case ("stroked") {
                    <button mat-stroked-button type="button" [color]="color" [disabled]="disabled"
                        attr.aria-label="{{type}}">
                        @if (type) {
                            <app-icon [type]="type" />
                        }
                        @if (effectiveText()) {
                            <span>{{effectiveText()}}</span>
                        }
                        <ng-container *ngTemplateOutlet="contentOutlet" />
                    </button>
                }
                @case ("raised") {
                    <button mat-raised-button type="button" [color]="color" [disabled]="disabled"
                        attr.aria-label="{{type}}">
                        @if (type) {
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
            <button mat-raised-button type="button" [disabled]="disabled"
                attr.aria-label="{{type}}">
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
    @Input() type: ButtonType
    @Input() visual?: ButtonVisual
    @Input() color?: ButtonColor
    @Input() defaultText?: boolean
    @Input() text?: string | null
    @Input() stretch?: boolean
    @Input() disabled?: boolean

    readonly effectiveText = signal("")

    ngOnChanges(changes: SimpleChanges) {
        if (changes["text"]) {
            this.effectiveText.set(changes["text"].currentValue)
        }
        if (changes["useDefaultText"]?.currentValue) {
            this.effectiveText.set(this.#getTextByType(this.type))
        }
    }

    #getTextByType(type: ButtonType) {
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
