import { Component, ChangeDetectionStrategy } from "@angular/core"
import { RouterModule } from "@angular/router"

@Component({
    standalone: true,
    imports: [RouterModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    selector: "app-noop-router-outlet",
    template: "<router-outlet />",
    styles: [":host { height: 100%; }"]
})
export class NoopRouterOutletComponent {
}
