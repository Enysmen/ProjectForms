export declare function roundTo2Decimals(number: number): number;
export declare function clearNewLines(text: string): string;
export declare function copyObject(dst: any, src: any): void;
export declare function copyCssClasses(dest: any, source: any): void;
export declare function capitalize(str: string): string;
export declare function notShortCircuitAnd(...args: Array<boolean>): boolean;
export declare const imageMimeTypes = "image/png, image/gif, image/jpeg, image/apng, image/avif, image/svg+xml, image/webp";
export declare function getAcceptedTypesByContentMode(contentMode: string): "" | "image/png, image/gif, image/jpeg, image/apng, image/avif, image/svg+xml, image/webp" | "video/*";
export declare function trimEmptyFields(object: {
    [index: string]: string;
}): void;
export declare function assign(...inputs: Array<any>): void;
