export declare function performRegistration(optionsFromServer: any): Promise<{
    id: string;
    rawId: string;
    type: string;
    response: {
        clientDataJSON: string;
        attestationObject: string;
    };
}>;
export declare function performLogin(optionsFromServer: any): Promise<{
    id: string;
    rawId: string;
    type: string;
    response: {
        clientDataJSON: string;
        authenticatorData: string;
        signature: string;
        userHandle: string | null;
    };
}>;
