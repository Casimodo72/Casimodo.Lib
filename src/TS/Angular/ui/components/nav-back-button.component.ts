import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, signal } from "@angular/core"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { AppNavigationService } from "@lib/services/navigation.service"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-nav-back-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule],
    styles: [" :host { display: contents; } "],
    template: `
@if (canNavigateBack()) {
    @if (minimal()) {
        <button mat-icon-button (click)="navigateBack()">
            <mat-icon>arrow_back_ios</mat-icon>
        </button>
    }
    @else {
        <button mat-button (click)="navigateBack()">
            <mat-icon>arrow_back_ios</mat-icon>Zurück
        </button>
    }
}
`
})
export class NavBackButtonComponent implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #navigationService = inject(AppNavigationService)
    readonly canNavigateBack = signal(false)

    readonly boundaryRouteId = input<string | undefined>()
    readonly minimal = input<boolean | undefined>()

    ngOnInit(): void {
        this.#navigationService.state
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(() => {
                this.canNavigateBack.set(this.#navigationService.canNavigateBack(this.boundaryRouteId()))
            })
    }

    navigateBack() {
        this.#navigationService.navigateBack(this.boundaryRouteId())
    }
}
