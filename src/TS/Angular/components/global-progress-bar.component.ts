import { ChangeDetectionStrategy, Component, inject } from "@angular/core"

import { MatProgressBarModule } from "@angular/material/progress-bar"

import { BusyStateService } from "@lib/services"

@Component({
    selector: "app-global-progress-bar",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatProgressBarModule],
    template: `
<div style="min-height: 6px">
    @if (busyService.isBusy()) {
        <mat-progress-bar mode="query" />
    }
</div>
    `
})
export class GlobalProgressBarComponent {
    readonly busyService = inject(BusyStateService)
}
