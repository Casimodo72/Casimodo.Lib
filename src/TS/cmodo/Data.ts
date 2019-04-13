

namespace cmodo {

    export function extendTransportedData(items: any[]) {
        // NOP. Consumer app can provide its own implementation.
    }

    export class EntityMappingService {
        getTypeKey(type: string): string {
            throw new Error("Not implemeted");
        }

        getMoTypeKey(type: string): string {
            throw new Error("Not implemeted");
        }

        getDisplayNameById(type: string, id: string): string {
            throw new Error("Not implemeted");
        }

        getIdByCode(type: string, id: string): string {
            throw new Error("Not implemeted");
        }

        createLinksForTags(ownerTypeId: string, ownerId: string, tags: any[]): any[] {
            throw new Error("Not implemeted");
        }
    }

    export let entityMappingService = new EntityMappingService();
}