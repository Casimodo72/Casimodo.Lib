import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core"
import { CommonModule } from "@angular/common"

import { StandardTextAreaInputComponent } from "@lib/ui/components/standard-text-area-input.component"
import type { IEntityCore } from "@lib/data"

@Component({
    selector: "app-entity-modification-info",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        StandardTextAreaInputComponent

    ],
    styles: [" :host { display: block; } "],
    template: `
@if (entity(); as entity) {
    <div class="flex-column gap-1 app-info-xs">
        @if (anyEntity()?.DeletedOn && anyEntity(); as anyEntity) {
            <div>
                Gelöscht
                @if (anyEntity.DeletedBy) {
                    von {{anyEntity.DeletedBy}}
                }
                am {{anyEntity.DeletedOn | date:'shortDate'}} um {{anyEntity.DeletedOn | date:'shortTime'}}
            </div>
        }

        @if (isModified()) {
            <div>
                Bearbeitet
                @if (entity.ModifiedBy) {
                    von {{entity.ModifiedBy}}
                }
                am {{entity.ModifiedOn | date:'shortDate'}} um {{entity.ModifiedOn | date:'shortTime'}}
            </div>
        }

        @if (entity.CreatedOn) {
            <div>
                Erstellt
                @if (entity.CreatedBy) {
                    von {{entity.CreatedBy}}
                }
                am {{entity.CreatedOn | date:'shortDate'}} um {{entity.CreatedOn | date:'shortTime'}}
            </div>
        }
    </div>
}
`
})
export class EntityModificationInfoComponent {
    readonly entity = input<Partial<IEntityCore> | null | undefined>()

    readonly anyEntity = computed<any>(() => this.entity())
    readonly isModified = computed(() => {
        const entity = this.entity()
        if (!entity) return false

        return entity.ModifiedOn &&
            entity.CreatedOn &&
            entity.ModifiedOn.getTime() !== entity.CreatedOn.getTime()
    })
}
