import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { CookieService } from 'ngx-cookie-service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(public jwtHelper: JwtHelperService, public cookieService: CookieService) { }

  public isAuthenticated() : boolean {
    const token = this.cookieService.get("jwt_auth");
    if(token == null || token == '') {
      return false;
    }
    var result: boolean;

    try {
      result = !this.jwtHelper.isTokenExpired(token);
    } catch {
      result = false;
    }

    return result;
  }
}
