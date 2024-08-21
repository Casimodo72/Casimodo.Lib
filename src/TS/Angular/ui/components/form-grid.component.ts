/* eslint-disable @angular-eslint/no-host-metadata-property */
import { CommonModule } from "@angular/common"
import { Component, ChangeDetectionStrategy, input, computed } from "@angular/core"

@Component({
    selector: "app-form-grid",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule],
    host: {
        "[class]": "classNames()"
    },
    template: "<ng-content/>"
})
export class FormGridComponent {
    // Tailwind needs the class names to be in our source code; it won't generate
    // the dynamic class names otherwise.
    // See https://tailwindcss.com/docs/content-configuration#dynamic-class-names
    static readonly gridColsVariants = {
        1: "lg:grid-cols-1",
        2: "lg:grid-cols-2",
        3: "lg:grid-cols-3",
        4: "lg:grid-cols-4",
        5: "lg:grid-cols-5",
    }
    readonly cols = input<number>(1)

    // TODO: REMOVE: protected styles = computed(() => `grid-template-columns: repeat(${this.cols()}, minmax(0, 1fr));`)

    protected classNames = computed(() => `app-form-grid ${(FormGridComponent.gridColsVariants as any)[this.cols()] ?? ""}`)
}
