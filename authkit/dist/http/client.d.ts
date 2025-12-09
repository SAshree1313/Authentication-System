declare class HttpClient {
    baseUrl: string;
    setBase(url: string): void;
    private headers;
    post(path: string, body: any, token?: string): Promise<any>;
    put(path: string, body: any, token?: string): Promise<any>;
    get(path: string, token?: string): Promise<any>;
    del(path: string, token?: string): Promise<any>;
}
export declare const http: HttpClient;
export {};
