namespace kmodo {

    //interface MoFileSaveDialogEntitiesContext {
    //    companyId: string;
    //    contractId: string;
    //    projectId: string;
    //    projectSegmentId: string;
    //}

    interface ViewArgsItem {
        imageDataUrl: string;
        context?: any;
    }

    interface ViewArgs extends ViewComponentArgs {
        item: ViewArgsItem;
    }

    interface ViewModel extends ComponentViewModel {
        fileName: string;
    }

    export interface MoFileSaveDialogOptions extends ViewComponentOptions {
        fileName?: string;
        owners: MoFileTreeOwnerDefinition[];
    }

    export abstract class MoFileSaveDialog extends ViewComponent {
        protected args: ViewArgs;
        protected fileExplorer: MoFileExplorerComponent;
        private _dialogWindow: kendo.ui.Window;
        _options: MoFileSaveDialogOptions;

        constructor(options: MoFileSaveDialogOptions) {
            super(options);

            this.$view = null;

            this.scope = kendo.observable({
                fileName: this._options.fileName || "Neue Datei"
            });

            this._isViewInitialized = false;
        }

        getModel(): ViewModel {
            return super.getModel() as ViewModel;
        }

        abstract refresh(): Promise<void>;

        createView(): void {
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

            this.$view = $("#view-" + this._options.id);

            this._dialogWindow = kmodo.findKendoWindow(this.$view);
            this._initDialogWindowTitle();

            kendo.bind(this.$view.find(".file-name-input"), this.getModel());

            const $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", () => {

                const targetFolder = this.fileExplorer.getCurrentFolder();
                if (!targetFolder)
                    return;

                this.args.isCancelled = false;
                this.args.isOk = true;

                kmodo.progress(true, this.$view);

                cmodo.webApiPost("/api/Mos/SaveImageDataToFolder",
                    {
                        FileName: this.getModel().fileName,
                        Data: this.args.item.imageDataUrl,
                        FolderId: targetFolder.Id
                    })
                    .then((responseData) => {
                        kmodo.progress(false, this.$view);
                        cmodo.showInfo("Die Datei wurde gespeichert.");
                        this._dialogWindow.close();
                    })
                    .catch(() => {
                        kmodo.progress(false, this.$view);
                        cmodo.showError("Die Datei konnte nicht gespeichert werden.");
                    });
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", () => {
                this.args.isCancelled = true;
                this.args.isOk = false;

                this._dialogWindow.close();
            });

            // Mo file explorer component.
            this.fileExplorer = new kmodo.MoFileExplorerComponent({
                $area: this.$view.find(".mo-file-system-view").first(),
                isFileSystemTemplateEnabled: false,
                areFileSelectorsVisible: false,
                isRecycleBinEnabled: false,
                isUploadEnabled: false,
                owners: this._options.owners
            });

            this.fileExplorer.createView();
        }

        _initDialogWindowTitle() {
            let title = "";

            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._options.isLookup)
                    title += " wählen";
            }

            this._dialogWindow.title(title);
        }
    }
}