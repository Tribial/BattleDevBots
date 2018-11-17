import { LoginModel } from "../../models/login-model.model";
import { CookieService } from 'ngx-cookie-service';
import { TokenModel } from "../../models/token-model.model";
import * as UserActions from "../actions/user.actions";

var cookieService : CookieService = new CookieService(document);

const initialState: LoginModel = {
    id: parseInt(cookieService.get("user_id")),
    username: cookieService.get("user_username"),
    email: cookieService.get("user_email"),
    isAdmin: cookieService.get("user_isAdmin") == "1",
    tokens: {
        token: cookieService.get("token"),
        refreshToken: cookieService.get("refreshToken"),
        tokenExpirationDate: new Date(cookieService.get("token_expDate")),
        refreshTokenExpirationDate: new Date(cookieService.get("refreshToken_expDate"))
    }
}

export function userReducer(state: LoginModel = initialState, action: UserActions.Actions) {
    switch(action.type) {
        case UserActions.SET_TOKEN:
            return action.payload;
        default:
            return state;
        case UserActions.REMOVE_TOKEN:
            return initialState;
    }
}
