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
            //var imageDataUrl = this.args.item.imageDataUrl;

            var companyId = null;
            var projectId = null;
            var projectSegmentId = null;
            var contractId = null;

            var ctx = this.args.item.context;
            if (ctx) {
                companyId = ctx.companyId;
                projectId = ctx.projectId;
                projectSegmentId = ctx.projectSegmentId;
                contractId = ctx.contractId;
            }

            this.fileExplorer.clearAllOwnerValues();
            if (companyId) {
                if (projectId)
                    this.fileExplorer.setOwnerValues("ProjectSeries", { Id: projectId, Name: "Projekt (alle Segmente)", CompanyId: companyId });
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
            var self = this;

            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            this.$view = $("#view-" + this._options.id);

            this._dialogWindow = kmodo.findKendoWindow(this.$view);
            this._initDialogWindowTitle();

            kendo.bind(this.$view.find(".file-name-input"), this.getModel());

            var $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", function () {

                var targetFolder = self.fileExplorer.getCurrentFolder();
                if (!targetFolder)
                    return;

                //self.args.buildResult();
                self.args.isCancelled = false;
                self.args.isOk = true;

                kmodo.progress(true, self.$view);

                cmodo.webApiPost("/api/Mos/SaveImageDataToFolder",
                    {
                        FileName: self.getModel().fileName,
                        Data: self.args.item.imageDataUrl,
                        FolderId: targetFolder.Id
                    })
                    .then(function (responseData) {
                        kmodo.progress(false, self.$view);
                        cmodo.showInfo("Die Datei wurde gespeichert.");
                        self._dialogWindow.close();
                    })
                    .catch(function () {
                        kmodo.progress(false, self.$view);
                        cmodo.showError("Die Datei konnte nicht gespeichert werden.");
                    });
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
                self.args.isCancelled = true;
                self.args.isOk = false;

                self._dialogWindow.close();
            });

            // The actual Mo file explorer component.
            this.fileExplorer = new kmodo.MoFileExplorerViewModel({
                $area: this.$view.find(".mo-file-system-view").first(),
                isFileSystemTemplateEnabled: false,
                areFileSelectorsVisible: false,
                isRecycleBinEnabled: false,
                isUploadEnabled: false,
                owners: function () {
                    var items = [];

                    items.push({
                        Name: "Projekt (für alle Segmente)",
                        Kind: "ProjectSeries",
                        TypeId: cmodo.entityMappingService.getTypeKey("ProjectSeries"),
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

            this.fileExplorer.initComponent();
        };

        _initDialogWindowTitle() {
            var title = "";

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