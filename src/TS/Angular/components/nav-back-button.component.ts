import { ChangeDetectionStrategy, Component, DestroyRef, Input, OnInit, inject, signal } from "@angular/core"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { AppNavigationService } from "@lib/services/navigation.service"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"
import { CommonModule } from "@angular/common"

@Component({
    selector: "app-nav-back-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatButtonModule, MatIconModule],
    styles: [" :host { display: contents; } "],
    template: `
    @if (canNavigateBack()) {
        @if (!minimal) {
            <button mat-button (click)="navigateBack()" [disabled]="!canNavigateBack()">
                <mat-icon>arrow_back_ios</mat-icon>Zurück
            </button>
        }
        @if (minimal) {
            <button mat-icon-button (click)="navigateBack()" [disabled]="!canNavigateBack()">
                <mat-icon>arrow_back_ios</mat-icon>
            </button>
        }
    }
    `
})
export class NavBackButtonComponent implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #navigationService = inject(AppNavigationService)
    readonly canNavigateBack = signal(false)

    @Input() boundaryRouteId: string | undefined
    @Input() minimal: boolean | undefined

    ngOnInit(): void {
        this.#navigationService.state
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(() => {
                this.canNavigateBack.set(this.#navigationService.canNavigateBack(this.boundaryRouteId))
            })
    }

    navigateBack() {
        this.#navigationService.navigateBack(this.boundaryRouteId)
    }
}
