export type BaseResponse<T = any> = {
success?: boolean;
message?: string;
data?: T;
};


export type CredentialCreateOptionsFromServer = any; // Fido2NetLib types arrive as JSON
export type AssertionOptionsFromServer = any;


export type RegisterBeginResponse = { options: CredentialCreateOptionsFromServer; challengeId: string };
export type RegisterCompleteResponse = { userId: number; credentialId: string; token: string; recoveryCode?: string; success: boolean; message?: string };


export type LoginBeginResponse = { options: AssertionOptionsFromServer; challengeId: string };
export type LoginCompleteResponse = { userId: number; token: string; success: boolean; message?: string };


export type RecoveryBeginResponse = { challengeId: string; success: boolean };
export type RecoveryVerifyResponse = { challengeId: string; options: CredentialCreateOptionsFromServer };
export type RecoveryCompleteResponse = { success: boolean; newRecoveryCode?: string; message?: string };


export type DeviceDto = { credentialId: string; deviceName?: string | null; createdAt: string; lastUsedAt?: string | null };
export type DeviceListResponse = { devices: DeviceDto[]; success?: boolean };


export type UserProfile = { id: number; name: string; email: string; hasPasskey: boolean };