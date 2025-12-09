export declare function recoveryBegin(email: string): Promise<any>;
export declare function recoveryVerifyCode(challengeId: string, recoveryCode: string): Promise<any>;
export declare function recoveryComplete(challengeId: string, optionsFromServer: any, deviceName?: string): Promise<any>;
