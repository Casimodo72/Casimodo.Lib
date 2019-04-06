namespace kmodo {

    interface ViewModel extends ViewComponentModel {
        fileName: string;
    }

    interface ViewArgsItemContext {
        companyId: string;
        contractId: string;
        projectId: string;
        projectSegmentId: string;
    }

    interface ViewArgsItem {
        imageDataUrl: string;
        context: ViewArgsItemContext;
    }

    interface ViewArgs extends ViewComponentArgs {
        item: ViewArgsItem;
    }

    export class MoFileSaveDialog extends ViewComponent {
        protected args: ViewArgs;
        private fileExplorer: MoFileExplorerViewModel;
        private _dialogWindow: kendo.ui.Window;

        constructor(options: ViewComponentOptions) {
            super(options);

            this.$view = null;

            this.scope = kendo.observable({
                fileName: "Karte XYZ"
            });

            this._isComponentInitialized = false;
        }

        getModel(): ViewModel {
            return super.getModel() as ViewModel;
        }

        refresh(): Promise<void> {
            // KABU TODO: Do we want to use images or not?
            // const imageDataUrl = this.args.item.imageDataUrl;

            let companyId = null;
            let projectId = null;
            let projectSegmentId = null;
            let contractId = null;

            const ctx = this.args.item.context;
            if (ctx) {
                companyId = ctx.companyId;
                projectId = ctx.projectId;
                projectSegmentId = ctx.projectSegmentId;
                contractId = ctx.contractId;
            }

            this.fileExplorer.clearAllOwnerValues();
            if (companyId) {
                if (projectId)
                    this.fileExplorer.setOwnerValues("Project", { Id: projectId, Name: "Projekt (alle Segmente)", CompanyId: companyId });
                if (projectSegmentId)
                    this.fileExplorer.setOwnerValues("ProjectSegment", { Id: projectSegmentId, Name: "Projekt-Segment", CompanyId: companyId });
                if (contractId)
                    this.fileExplorer.setOwnerValues("Contract", { Id: contractId, Name: "Auftrag", CompanyId: companyId });
                this.fileExplorer.activateOwner();
            }

            // Return dummy promise to satisfy overridden refresh() method.
            return Promise.resolve();
        }

        createView(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

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

            // The actual Mo file explorer component.
            this.fileExplorer = new kmodo.MoFileExplorerViewModel({
                $area: this.$view.find(".mo-file-system-view").first(),
                isFileSystemTemplateEnabled: false,
                areFileSelectorsVisible: false,
                isRecycleBinEnabled: false,
                isUploadEnabled: false,
                owners: function () {
                    const items = [];

                    items.push({
                        Name: "Projekt (für alle Segmente)",
                        Kind: "Project",
                        TypeId: cmodo.entityMappingService.getTypeKey("Project"),
                        Id: null
                    });

                    items.push({
                        Name: "Projekt-Segment",
                        Kind: "ProjectSegment",
                        TypeId: cmodo.entityMappingService.getTypeKey("ProjectSegment"),
                        Id: null
                    });

                    // KABU TODO: IMPORTANT: How to get AUTH here?
                    //if (cmodo.authContext.manager.hasUserRole("Manager")) {
                    //items.push({
                    //    Name: "Auftrag",
                    //    Kind: "Contract",
                    //    TypeId: modo.entityMappingService.getTypeKey("Contract"),
                    //    // TODO: REMOVE?: IsManagement: true,
                    //    Id: null
                    //});
                    //}

                    return items;
                }
            });

            this.fileExplorer.createView();
        };

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
        };
    }
}