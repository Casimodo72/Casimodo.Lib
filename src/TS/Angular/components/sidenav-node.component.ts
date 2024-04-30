import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core"
import { CommonModule } from "@angular/common"

import { MatListModule } from "@angular/material/list"
import { MatIconModule } from "@angular/material/icon"

import { SidenavService, NavNode } from "@lib/services"

@Component({
    selector: "app-sidenav-node",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatListModule, MatIconModule],
    styles: [`
        div.app-nav-item-container {
            @apply flex items-center;
        }

        span.app-nav-item-title {
            @apply ml-2;
        }
    `],
    template: `
@if (node(); as node) {
    <mat-list-item [ngStyle]="{'padding-left': (((node.depth ?? 0) + 1) * 16) + 'px'}">
        <div class="app-nav-item-container" (click)="node.clicked && node.clicked()">
            @if (node.icon) {
                <mat-icon mat-list-icon>{{node.icon}}</mat-icon>
            }
            @else {
                <!-- TODO: We need a placeholder for nodes without an icon -->
                <mat-icon mat-list-icon>fiber_manual_record</mat-icon>
            }

            @if (!node.iconOnly && node.title && sidenavService.isTextVisible()) {
                <span class="app-nav-item-title">
                    {{node.title}}
                </span>
            }
        </div>
    </mat-list-item>

    <mat-nav-list *ngIf="node.children?.length">
        <app-sidenav-node *ngFor="let childNode of node.children" [node]="childNode" F />
    </mat-nav-list>
}
`
})
export class SidenavNodeComponent {
    readonly sidenavService = inject(SidenavService)
    readonly node = input<NavNode | null>(null)
}
