import { TokenModel } from "./token-model.model";

export class LoginModel {
    public id: Number = -1;
    public username: string = "";
    public email: string = "";
    public isAdmin: boolean = false;
    public tokens: TokenModel;
}
