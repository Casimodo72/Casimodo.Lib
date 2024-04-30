import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { MatCardModule } from "@angular/material/card"

type MessageBoxType = "success" | "info" | "warning" | "error" | ""

@Component({
    selector: "app-message-box",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatCardModule],
    template: `
@if (type(); as type) {
    <mat-card class="px-4 py-3 mat-elevation-z1">
        <div [ngClass]="
            {
                'app-info': type === 'info',
                'app-warning': type === 'warning',
                'app-error': type === 'error',
                'app-success': type === 'success'
            }">
            <ng-content />
        </div>
    </mat-card>
}
`
})
export class MessageBoxComponent {
    readonly type = input.required<MessageBoxType>()
}
