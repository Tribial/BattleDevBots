import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { CookieService } from 'ngx-cookie-service';
import { Store } from '@ngrx/store';
import { AppState } from '../../store/state/app.state';
import { LoginModel } from '../../models/login-model.model';
import * as UserActions from '../../store/actions/user.actions';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private auth: LoginModel;

  constructor(public cookieService: CookieService, private store: Store<AppState>, private _router: Router) { 
    store.select('auth').subscribe(data => this.auth = data);
  }

  public isAuthenticated() : boolean {
    const token = this.cookieService.get("jwt_auth");
    if(this.auth.tokens.token === '' || this.auth.tokens.token === undefined) {
      if(token === '' || token === undefined) {
        return false;
      }
    }
    let now = new Date()
    if(this.auth.tokens.tokenExpirationDate < now) {
      return false;
    }

    return true;
  }

  public handleUnauthorized() {
    this.cookieService.delete('jwt_auth', '/');
    this.cookieService.delete('r_jwt_auth', '/');
    this.store.dispatch(new UserActions.RemoveToken());
    this._router.navigate(['account','login'])
  }
}
