
export interface IEnumBase {
    Id: string
    Index: number
    Name: string
    DisplayName: string
}

export class EnumBase implements IEnumBase {
    Id!: string
    Index!: number
    Name!: string
    DisplayName!: string
}
