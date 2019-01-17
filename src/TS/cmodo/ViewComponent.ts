
namespace cmodo {

    export interface ComponentAuthSettings {
        userId?: string;
        canView: boolean;
        canCreate: boolean;
        canModify: boolean;
        canDelete: boolean;
        canManage?: boolean;
    }

    export abstract class ViewComponent extends ComponentBase {
        protected _options: any;
        public keyName: string;
        protected auth: ComponentAuthSettings;
        scope: any;
        component: any;
        protected extension: any;

        constructor(options: any) {
            super();

            this._options = options || {};

            this.keyName = "Id";
            if (typeof this._options.dataKeyName !== "undefined")
                this.keyName = this._options.dataKeyName;

            this.auth = {
                canView: true,
                canCreate: false,
                canModify: false,
                canDelete: false
            };
            this.scope = {
                item: null
            };

            this.component = null;
        }

        init(): any {
            return this;
        }

        refresh(): Promise<void> {
            return Promise.resolve();
        }

        createView(): void {
            // NOP
        }

        protected setComponent(value: any): void {
            this.component = value;
        }

        // KABU TODO: REMOVE?
        //hasChanges() {
        //    // NOP
        //    return false;
        //}

        public clear(): void {

            this.trigger("clear", { sender: this });
        }

        executeCustomCommand(cmd): void {
            if (!this.extension)
                return;

            if (!this.extension.actions[cmd.name])
                return;

            this.extension.actions[cmd.name](cmd);
        }
    }
}