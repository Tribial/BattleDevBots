export class TokenModel {
    public token: string = "";
    public refreshToken: string = "";
    public tokenExpirationDate: Date = new Date();
    public refreshTokenExpirationDate: Date = new Date();
}
