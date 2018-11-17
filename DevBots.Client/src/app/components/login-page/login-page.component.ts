import { Component, OnInit, HostListener } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { Store } from '@ngrx/store';
import { AppState } from '../../store/state/app.state';
import { Observable, Subscription, interval } from 'rxjs';
import { LoginModel } from '../../models/login-model.model';
import { HttpService } from '../../services/http/http.service';
import { ResponseModel } from '../../models/response-model';
import * as AuthActions from '../../store/actions/user.actions';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from '../../services/message/message-service.service';
import { Location } from '@angular/common';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.scss'],
})
export class LoginPageComponent implements OnInit {
  user_auth: LoginModel;
  response: ResponseModel<LoginModel> = new ResponseModel();
  isLoading: boolean = false;
  username: string = "";
  password: string = "";
  top: string;
  left: string;
  isLoginForm: boolean = true;
  subscriptions: Subscription[] = [];
  r_username: string = "";
  r_email: string = "";
  r_password: string = "";
  r_c_password: string = "";
  r_m_password: string = "";

  constructor(private location: Location, private _route: ActivatedRoute, private _cookieService: CookieService, private _router: Router, private _auth: AuthService, private _store: Store<AppState>, private _http: HttpService, private _messageService: MessageService) {
    this.subscriptions.push(
      _store.select<LoginModel>('auth')
        .subscribe(data => this.user_auth = data)
    );
   }

  ngOnInit() {
    switch(this._route.snapshot.paramMap.get('type')) {
      case "login":
        this.isLoginForm = true;
        break;
      case "register":
        this.isLoginForm = false;
        break;
      default:
        this._router.navigate(['account', 'login']);
    }
    this.checkAuth();
    this.calculateOffset(true);
  }

  checkAuth() {
    if(this._auth.isAuthenticated()) {
      this._router.navigate(['/']);
    }
  }

  changeToRegister() {
    this.location.replaceState("/account/register");
    this.username = "";
    this.password = "";
    this.isLoginForm = false;
    setTimeout(() => this.calculateOffset(), 10);
  }

  changeToLogin() {
    this.location.replaceState("/account/login");
    this.r_email = "";
    this.r_c_password = "";
    this.r_m_password = "";
    this.r_username = "";
    this.r_password = "";
    this.isLoginForm = true;
    setTimeout(() => this.calculateOffset(), 10);
  }

  @HostListener('window:resize', ['$event'])
  onResize(event) {
    this.calculateOffset();
  }

  calculateOffset(isFirst?: boolean) {
    let width = window.innerWidth;
    let height = window.innerHeight;
    let panel_height = document.getElementById('login-register-panel').clientHeight;
    let panel_width = document.getElementById('login-register-panel').clientWidth;
    if(isFirst) {
      this.top = ((height - (this.isLoginForm ? 264 : 470))/(2 * height) * 100) + '%';
      this.left = ((width - 344)/(2 * width) * 100) + '%';
    } else {
      this.top = ((height - panel_height)/(2 * height) * 100) + '%';
      this.left = ((width - panel_width)/(2 * width) * 100) + '%';
    }
  }

  register() {
    this.isLoading = true;
    this.subscriptions.push(
      this._http.register(this.r_email, this.r_username, this.r_password, this.r_c_password, this.r_m_password).subscribe(
        data => {
          this.r_password = "";
          this.r_c_password = "";
          this.r_m_password = "";
          this.isLoading = false;
          this._messageService.addMessage(
            "Your account has been created, soon you will receive an email, click the link to confirm your account. Only then the registration process will complete",
            "success",
            10
            );
          this.changeToLogin();
        },
        error => {
          this.r_password = "";
          this.r_c_password = "";
          this.r_m_password = "";
          if(error.error.errorOccured === undefined) {
            error.error.Email ? error.error.Email.forEach(e => {
              this._messageService.addMessage(e, "error", 5);
            }) : null;
            error.error.Username ? error.error.Username.forEach(e => {
              this._messageService.addMessage(e, "error", 5);
            }) : null;
            error.error.Password ? error.error.Password.forEach(e => {
              this._messageService.addMessage(e, "error", 5);
            }) : null;
          } else {
            this.response = error.error;
            error.error.errors.forEach(e => {
              this._messageService.addMessage(e, "error", 5);
            });
          }
          this.isLoading = false;
        })
    );
  }

  login() {
    this.isLoading = true;
    this.subscriptions.push(
      this._http.login(this.username, this.password).subscribe(
        data => {
          this.password = "";
          this.response = data;
          this.user_auth = this.response.model;
          this._store.dispatch(new AuthActions.SetToken(this.user_auth));
          this._cookieService.set('jwt_auth', this.user_auth.tokens.token, undefined, '/');
          this._cookieService.set('r_jwt_auth', this.user_auth.tokens.refreshToken, undefined, '/');
          this.isLoading = false;
          this.checkAuth();
        },
        error => {
          this.password = "";
          if(error.error.errorOccured === undefined) {
            error.error.EmailOrUsername ? error.error.EmailOrUsername.forEach(e => {
              this._messageService.addMessage(e.replace('EmailOrUsername', 'Username or email'), "error", 5);
            }) : null;
            error.error.Password ? error.error.Password.forEach(e => {
              this._messageService.addMessage(e, "error", 5);
            }) : null;
          } else {
            this.response = error.error;
            error.error.errors.forEach(e => {
              this._messageService.addMessage(e, "error", 5);

            });
          }
          this.isLoading = false;
        })
    );
  }

  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    this.subscriptions.forEach(s => {
      s.unsubscribe();
    });
  }
}
