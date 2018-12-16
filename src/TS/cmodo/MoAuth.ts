
namespace cmodo {

    export interface MoPermission {
        RoleId: string;
    }

    export interface Mo {
        Permissions?: MoPermission[];
    }

    // KABU TODO: IMPORTANT: Uses geoassistant.
    export function hasMoManagerPermissionOnly(mo: Mo): boolean {
        if (!mo)
            return false;

        if (typeof mo.Permissions === "undefined")
            // Be overly strict if no permission information was retrieved at all.
            return true;

        if (!mo.Permissions || !mo.Permissions.length)
            // Currently an empty set of permissions means: no restrictions.
            return false;

        // KABU TODO: 

        var manamegentPerm = mo.Permissions.find(x => x.RoleId === cmodo.authRoleSettings.roles.ManagerRoleId);
        if (manamegentPerm)
            // Currently a manager permission means: management only.
            return true;

        return false;
    }
}