import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component } from "@angular/core"
import { MatRippleModule } from "@angular/material/core"

@Component({
    selector: "app-tile",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatRippleModule],
    template: `
        <div class="app-tile" matRipple>
            <ng-content></ng-content>
        </div>
    `,
    styles: [`
        :host {
            display: block;
            @apply rounded;
            border-style: solid;
            border-color: gray;
            border-width: 1px;
            cursor: pointer;
            position: relative;

            >.app-tile {
                width: 100%;
                height: 100%;
                @apply flex flex-col justify-center px-2 py-1
            }

            &:hover::before {
                position: absolute;
                left: 0px;
                top: 0px;
                width: 100%;
                height: 100%;
                @apply rounded;
                content: "";
                // TODO: Again, how to use material's color variables here?
                background-color: white;
                opacity: 0.08;
            }
        }
    `]
})
export class TileComponent {
}
