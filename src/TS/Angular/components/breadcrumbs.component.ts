import { ChangeDetectionStrategy, Component, DestroyRef, Input, OnInit, inject, signal } from "@angular/core"
import { CommonModule } from "@angular/common"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"

import { MatIconModule } from "@angular/material/icon"
import { MatTooltipModule } from "@angular/material/tooltip"

import { Breadcrumb } from "@lib/navigation"
import { AppNavigationState, AppNavigationService } from "@lib/services/navigation.service"
import { NavBackButtonComponent } from "."

@Component({
    selector: "app-breadcrumbs",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatTooltipModule, NavBackButtonComponent],
    template: `
        <div class="flex items-center gap-1">
            @for (crumb of breadcrumbs(); track crumb.id; let last = $last) {

                <!-- TODO: Text is not v-centered in button :-( Using plain div instead.
                    <button mat-button *ngIf="crumb.canNavigate" class="app-mat-crumb-button" [matTooltip]="crumb.path"
                        (click)="navigateToCrumb(crumb)">
                        <span class="mat-small">{{crumb.label}}</span>
                    </button>
                -->

                <div *ngIf="crumb.canNavigate" class="app-crumb-button" [matTooltip]="crumb.path"
                    (click)="navigateToCrumb(crumb)">
                    {{crumb.label}}
                </div>

                <div *ngIf="!crumb.canNavigate" [matTooltip]="crumb.path"
                    [ngClass]="{'app-current-crumb-text': last, 'app-crumb-text': !last}">

                    <span *ngIf="!last">(</span>
                    {{crumb.label}}
                    <span *ngIf="!last">)</span>
                </div>

                <!-- TODO: The icon looks too fat. I tried to use a unicode character instead, but it had annoying right hand side padding. -->
                <mat-icon *ngIf="!last">chevron_right</mat-icon>
            }
        </div>
    `,
    styles: [`

        .app-crumb-button {
            @apply mx-1 text-sm font-normal;

            &:hover {
                cursor: pointer;
            }
        }

        .app-crumb-text {
            @apply text-sm font-normal
        }

        .app-current-crumb-text {
            // margin-bottom: 3px;
            @apply text-lg
        }
    `]
})
export class BreadcrumbsComponent implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #navigationService = inject(AppNavigationService)
    readonly breadcrumbs = signal<Breadcrumb[]>([])

    @Input() boundaryRouteId: string | undefined
    @Input() max: number | undefined

    ngOnInit(): void {
        this.#navigationService.state
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(state => {
                this.#updateBreadcrumbs(state)
            })
    }

    #updateBreadcrumbs(state: AppNavigationState): void {
        this.breadcrumbs.set(
            state.breadcrumbsContainer.items({
                inclusiveBounderyRouteId: this.boundaryRouteId,
                max: this.max
            }))
    }

    navigateToCrumb(crumb: Breadcrumb) {
        this.#navigationService.navigateToBreadcrumb(crumb)
    }
}
