﻿
namespace cmodo {

    export function getActionAuth(queryItems: any[]): Promise<AuthActionManager> {
        return cmodo.webApiPost("/api/GetActionAuth", queryItems, { isDataFixupDisabled: true })
            .then(function (response) {
                return new AuthActionManager(response.result);
            });
    };

    interface AuthPermData {
        Action: string,
        VRole: string
    }

    interface AuthPartData {
        Part: string,
        Group: string,
        Permissions: AuthPermData[]
    }

    export interface AuthQuery {
        Part: string;
        Group: string;
        VRole: string;
    }

    export class AuthActionManager {
        public items: AuthPartData[];
        public userId: string;
        public userName: string;
        public userRoles: string[];

        constructor(data: any) {
            this.items = data.Items;
            this.userId = data.UserId;
            this.userName = data.UserName;
            this.userRoles = data.UserRoles;
        }

        hasUserRole(role: string) {
            return this.userRoles.indexOf(role) !== -1;
        }

        part(name: string, group: string) {
            group = group || null;
            return new AuthPart(this, this.items.find(x => x.Part === name && (group === "*" || x.Group === group)));
        }

    }

    export class AuthPart {

        public container: AuthActionManager;
        public part: AuthPartData;

        constructor(container: AuthActionManager, part: AuthPartData) {
            this.container = container;
            this.part = part;
        }

        can(action, vrole: string) {
            if (!this.part)
                return false;

            vrole = vrole || null;

            var permissions = this.part.Permissions;
            var perm: AuthPermData;
            for (let i = 0; i < permissions.length; i++) {
                perm = permissions[i];
                if (perm.Action === action && perm.VRole === vrole)
                    return true;
            }

            return false;
        }
    }

    export class AuthContext extends ComponentBase {
        manager: AuthActionManager;
        items: any[];
        userId: string;
        userName: string;

        constructor() {
            super();

            this.manager = null;
            this.items = [];
        }

        read(): Promise<AuthActionManager> {
            var self = this;
            return Promise.resolve()
                .then(() => getActionAuth(self.items))
                .then(function (manager: AuthActionManager) {
                    self.manager = manager;
                    self.userId = manager.userId;
                    self.userName = manager.userName;

                    self.trigger("read", { sender: self, auth: manager });

                    return manager;
                });
        }

        hasUserRole(role: string): boolean {
            if (!this.manager)
                return false;

            return this.manager.hasUserRole(role);
        }

        addQueries(queries: AuthQuery[]): void {
            for (let i = 0; i < queries.length; i++) {
                this.items.push(queries[i]);
            }
        }
    }

    export var authContext = new AuthContext();
}