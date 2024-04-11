import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, inject } from "@angular/core"
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { BusyStateService } from "@lib/services"

@Component({
    selector: "app-progress-bar",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatProgressBarModule],
    template: `
        <mat-progress-bar *ngIf="busyService.isBusy()" mode="query" />
    `
})
export class ProgressBarComponent {
    readonly busyService = inject(BusyStateService)
}
