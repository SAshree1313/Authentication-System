export declare function listDevices(token: string): Promise<any>;
export declare function renameDevice(credentialId: string, deviceName: string, token: string): Promise<any>;
export declare function deleteDevice(credentialId: string, token: string): Promise<any>;
export declare function addDeviceBegin(token: string): Promise<any>;
export declare function addDeviceComplete(challengeId: string, optionsFromServer: any, deviceName: string | undefined, token: string): Promise<any>;
