import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { MatRippleModule } from "@angular/material/core"

@Component({
    selector: "app-selectable-tile",
    standalone: true,
    imports: [CommonModule, MatRippleModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
<div class="app-container" matRipple  [ngClass]="{'app-container-selectable': clickable()}">
    <div class="app-selection-indicator" [ngClass]="{'app-selected': selected()}"></div>
    <div class="app-content">
        <ng-content />
    </div>
</div>
    `,
    styles: [`
        :host {
            display: block;
            // TODO: Use material's color variables
            border: 1px solid gray;
            @apply rounded;
        }
        .app-container {
            height: 100%;
            display: flex;
            position: relative;
        }
        .app-container-selectable {
            cursor: pointer;
            &:hover::before {
                position: absolute;
                left: 0px;
                top: 0px;
                width: 100%;
                height: 100%;
                content: "";
                // TODO: How to use material's color variables here?
                background-color: white;
                opacity: 0.08;
            }
        }
        .app-selection-indicator {
            border: 1px solid transparent;
            @apply w-2 rounded-tl rounded-bl
        }
        .app-selected {
            // TODO: Use material's color variables
            background-color: #03a9f4;
        }
        .app-content {
            flex: 1 1 100%;
            width: 100%;
            min-width: 0;
            height: 100%;
            @apply flex flex-col justify-center px-2 py-1
        }
    `]
})
export class SelectableTileComponent {
    readonly selected = input(false)
    readonly clickable = input(true)
}
