import proj4 from "proj4"

export interface IUTMValue {
    zone: string;
    easting: number;
    northing: number;
}

export function convertLatLngToUTM(coords: google.maps.LatLngLiteral): IUTMValue {
    const lat = coords.lat
    const lng = coords.lng

    // Compute zone
    // Sources: https://gis.stackexchange.com/questions/13291/computing-utm-zone-from-lat-long-point
    // original https://www.wavemetrics.com/code-snippet/convert-latitudelongitude-utm

    let zoneNum = Math.floor((coords.lng + 180) / 6) + 1

    if (lat >= 56.0 && lat < 64.0 && lng >= 3.0 && lng < 12.0) {
        // TODO: what is this case actually?
        zoneNum = 32
    }
    // Special zones for Svalbard
    else if (lat >= 72.0 && lat < 84.0) {
        if (lng >= 0.0 && lng < 9.0) {
            zoneNum = 31
        } else if (lng >= 9.0 && lng < 21.0) {
            zoneNum = 33
        } else if (lng >= 21.0 && lng < 33.0) {
            zoneNum = 35
        } else if (lng >= 33.0 && lng < 42.0) {
            zoneNum = 37
        }
    }

    const zone: string = "" + zoneNum + getUTMLetterDesignator(lat)

    const utm = "+proj=utm +zone=" + zone
    const wgs84 = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs"

    const value = proj4(wgs84, utm, [coords.lng, coords.lat])

    return {
        zone: zone,
        easting: value[0],
        northing: value[1]
    }
}

function getUTMLetterDesignator(lat: number): string {
    // Source: https://www.wavemetrics.com/code-snippet/convert-latitudelongitude-utm

    let letter: string | undefined = undefined
    if (-80 <= lat && lat <= 84) {
        letter = "CDEFGHJKLMNPQRSTUVWXX"[Math.floor((lat + 80) / 8)]
    }

    if (!letter) {
        throw new Error("Error while computing letter designator of " +
            `latitude ${lat}: latitude is outside of the UTM limits.`)
    }

    return letter
}
