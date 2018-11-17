import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HttpParamsOptions, HttpParams } from '@angular/common/http/src/params';
import { Store } from '@ngrx/store';
import { AppState } from '../../store/state/app.state';
import { LoginModel } from '../../models/login-model.model';
import { ResponseModel } from '../../models/response-model';
import { Observable, Subscription } from 'rxjs';
import { Empty } from '../../models/empty.model';
import { CookieService } from 'ngx-cookie-service';

const api = 'https://localhost:44397/api/';
const httpOptions = {
  headers: new HttpHeaders({
    'Content-Type':  'application/json',
    'Access-Control-Allow-Origin': 'localhost:4200',
  })
};

@Injectable({
  providedIn: 'root'
})
export class HttpService {

  // private tokenModel: LoginModel;
  // private _subscriptions: Subscription[] = [];

  constructor(private _http: HttpClient, private store: Store<AppState>, private _cookieService: CookieService) { 
    //store.select('auth').subscribe(data => httpOptions.headers.set('Authorization', 'Bearer ' + data.tokens.token));
  }

  public login(login: string, password: string): Observable<ResponseModel<LoginModel>> {
    return this._http.post<ResponseModel<LoginModel>>(api + 'Users/Login', {emailOrUsername: login, password: password}, httpOptions);
  }

  public register(email: string, username: string, password: string, confirmPassword: string, masterPassword) {
    return this._http.post<ResponseModel<Empty>>(api + "Users/Register", {email, username, password, confirmPassword, masterPassword}, httpOptions);
  }

  public activate(userGuid: string) {
    return this._http.post<ResponseModel<Empty>>(api + "Users/Activate/" + userGuid, null, httpOptions);
  }

  public logout() {
    let jwt = this._cookieService.get('jwt_auth');
    httpOptions.headers.append('Authorization', 'Bearer ' + jwt);
    return this._http.post<ResponseModel<Empty>>(api + 'Users/Logout', null, httpOptions);
  }
}
