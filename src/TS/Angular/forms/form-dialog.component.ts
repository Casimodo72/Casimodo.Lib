import { ChangeDetectionStrategy, Component, Inject, OnInit, ViewChild, ViewContainerRef, computed, inject, signal } from "@angular/core"
import { CommonModule } from "@angular/common"
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { NotificationService } from "@lib/services"
import { FormMode, IFormDialogArgs, IFormComponent, FormConfig, IFormResult, FormResultStatus, IEntityFormModel } from "./formModels"
import { DialogService } from "@lib/dialogs"

// TODO: Check if dynamic components + passing data can be improved with Angular 16.2.
//   See https://blog.ninja-squad.com/2023/08/09/what-is-new-angular-16.2/

@Component({
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatDialogModule, MatButtonModule],
    styles: [`
        :host {
            // TODO: REMOVE: width: var(--max-app-width);
            height: 100%;
        }
    `],
    templateUrl: "./form-dialog.component.html"
})
export class FormDialogComponent implements OnInit {
    readonly #notificationService = inject(NotificationService)
    readonly #dialogService = inject(DialogService)
    readonly vm = signal<IEntityFormModel | null>(null)
    readonly mode = signal<FormMode | "">("")
    readonly title = computed(() => {
        const vm = this.vm()
        const title = vm?.title() ?? ""
        const dataDisplayName = vm?.dataDisplayName() ?? ""
        const actionDisplayName = this.actionDisplayName()

        return title
            ? title
            // Produces ["My entity name"] - ["add" | "edit"]
            : `${dataDisplayName} - ${actionDisplayName}`
    })
    readonly actionDisplayName = signal("")
    confirmButtonText = signal("")

    @ViewChild("target", { static: true, read: ViewContainerRef }) vcRef!: ViewContainerRef

    constructor(
        private readonly dialogRef: MatDialogRef<FormDialogComponent>,
        @Inject(MAT_DIALOG_DATA)
        private readonly args: IFormDialogArgs
    ) {
        this.mode.set(args.mode)

        this.actionDisplayName.set(
            args.mode === "modify"
                ? "bearbeiten"
                : "hinzufügen")

        this.confirmButtonText.set(
            args.mode === "modify"
                ? "Speichern"
                : "Hinzufügen")
    }

    async ngOnInit() {
        try {
            if (this.args.mode === "modify" && !this.args.data) {
                throw new Error("Form data is required in edit mode.")
            }

            const componentRef = this.vcRef.createComponent<IFormComponent>(this.args.component)
            const vm = componentRef.instance.getModel()

            const formConfig: FormConfig = {
                mode: this.args.mode,
                inputData: this.args.data,
                initialValue: this.args.initialValue
            }
            await vm.initialize(formConfig)

            if (vm.confirmButtonText) {
                this.confirmButtonText = vm.confirmButtonText
            }

            this.vm.set(vm)
        }
        catch (error) {
            this.#notificationService.showError(error)
        }
    }

    cancel() {
        const result: IFormResult = {
            hasSucceeded: false,
            status: "cancelled"
        }

        this.dialogRef.close(result)
    }

    async delete() {
        try {
            const message = "Sind Sie sicher, dass Sie diesen Eintrag löschen wollen?"
            // TODO: Let the model provide override the default deletionn confirmation message.
            const isDeletionConfirmed = await this.#dialogService.confirm(
                {
                    message: message
                })
            if (!isDeletionConfirmed) {
                return
            }

            const vm = this.vm()!
            await vm.submit(this.args.mode)
            const data = vm.resultData()

            const result: IFormResult = {
                hasSucceeded: true,
                status: "deleted",
                data: data
            }

            this.dialogRef.close(result)
        }
        catch (error) {
            this.#notificationService.showError(error)

            const result: IFormResult = {
                hasSucceeded: false,
                status: "failed"
            }

            this.dialogRef.close(result)
        }
    }

    async submit() {
        const vm = this.vm()!

        if (!await vm.validate()) {

            const errorMessage = vm.validationErrorMessage
                ? vm.validationErrorMessage
                : "Die Daten sind noch unvollständig/fehlerhaft. \n" +
                "Bereinigen Sie die Daten oder drücken Sie auf 'Abbrechen' " +
                "falls Sie die Eingabe verwerfen und das Formular schließen wollen."
            this.#dialogService.showInfo(
                {
                    title: "Speichern noch nicht möglich",
                    message: errorMessage
                })

            return
        }

        if (!vm.isModified()) {
            this.#dialogService.showInfo(
                {
                    title: "Speichern noch nicht möglich",
                    message: "Sie haben keine Änderungen vorgenommen. \n" +
                        "Nehmen Sie entweder Änderungen vor oder drücken Sie auf 'Abbrechen' " +
                        "falls Sie die Eingabe verwerfen das Formular schließen wollen."
                })

            return
        }

        const mode = this.args.mode

        try {
            if (mode !== "add" && mode !== "modify") {
                throw new Error(`Invalid form mode for form-submit: '${mode}'.`)
            }

            let resultType: FormResultStatus = "failed"

            if (mode === "add") {
                await vm.submit("add")
                resultType = "added"
            } else if (mode === "modify") {
                await vm.submit("modify")
                resultType = "modified"
            }

            const result: IFormResult = {
                hasSucceeded: true,
                status: resultType,
                data: vm.resultData()
            }

            this.dialogRef.close(result)
        }
        catch (error) {
            this.#notificationService.showError(error)

            const result: IFormResult = {
                hasSucceeded: false,
                status: "failed"
            }

            this.dialogRef.close(result)
        }
    }
}
