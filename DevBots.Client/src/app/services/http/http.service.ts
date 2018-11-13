import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HttpParamsOptions, HttpParams } from '@angular/common/http/src/params';
import { Store } from '@ngrx/store';
import { AppState } from '../../store/state/app.state';
import { LoginModel } from '../../models/login-model.model';
import { ResponseModel } from '../../models/response-model';
import { Observable } from 'rxjs';
import { Empty } from '../../models/empty.model';

const api = 'https://localhost:44397/api/';
const httpOptions = {
  headers: new HttpHeaders({
    'Content-Type':  'application/json'
  })
};

@Injectable({
  providedIn: 'root'
})
export class HttpService {

  private tokenModel: LoginModel;

  constructor(private _http: HttpClient, private store: Store<AppState>) { 
    if(this.tokenModel != undefined) {
      httpOptions.headers.set('Authorization', 'Bearer ' + this.tokenModel.tokens.token);
    }
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
}
