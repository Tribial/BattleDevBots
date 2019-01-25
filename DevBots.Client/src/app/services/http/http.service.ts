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
import { Command } from 'src/app/models/command.model';
import { SimpleRobot } from 'src/app/models/simple-robot.model';
import { Script } from 'src/app/models/script.model';
import { SimpleScript } from 'src/app/models/simple-script.model';

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
  private _headers;

  constructor(private _http: HttpClient, private store: Store<AppState>, private _cookieService: CookieService) { 
    this._headers = new HttpHeaders({
      'Content-Type':  'application/json',
      'Access-Control-Allow-Origin': 'localhost:4200',
      'Authorization': 'bearer ' + _cookieService.get('jwt_auth'),
    });
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
    return this._http.post<ResponseModel<Empty>>(api + 'Users/Logout', null, {headers: this._getHeaders()});
  }

  //SCRIPTS
  public runScript(scriptId: number) {
    return this._http.get<ResponseModel<Command[]>>(`${api}Language/Decode/${scriptId}`, {headers: this._getHeaders()});
  }

  public addScript(name: string, robotId: number, script: string) {
    return this._http.post<ResponseModel<Empty>>(`${api}Script`, {name, robotId, script}, {headers: this._getHeaders()});
  }

  public getScriptsByUser() {
    return this._http.get<ResponseModel<Script[]>>(`${api}Script/ByUser`, {headers: this._getHeaders()});
  }

  public removeScript(scriptId: number) {
    return this._http.delete<ResponseModel<Empty>>(`${api}Script/${scriptId}`, {headers: this._getHeaders()});
  }

  public getSimpleScriptsByRobot(robotId: number) {
    return this._http.get<ResponseModel<SimpleScript[]>>(`${api}Script/Simple/${robotId}`, {headers: this._getHeaders()});
  }
  //ROBOTS
  public getSimpleRobots(): Observable<ResponseModel<SimpleRobot[]>> {
    return this._http.get<ResponseModel<SimpleRobot[]>>(api + 'Robots/Simple', {headers: this._getHeaders()});
  }

  private _getHeaders() {
    return new HttpHeaders({
      'Content-Type':  'application/json',
      'access-control-allow-origin': 'localhost:4200',
      'Authorization': 'bearer ' + this._cookieService.get('jwt_auth'),
    });
  }
}
