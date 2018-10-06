import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoginPageComponent } from './components/login-page/login-page.component';
import { MainPageComponent } from './components/main-page/main-page.component';
import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { HttpClientModule } from '@angular/common/http';
import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { JwtModule } from '@auth0/angular-jwt';

export function tokenGetter() {
  var name = 'jwt_auth=';
  var ca = document.cookie.split(';');
  for(var i = 0; i < ca.length; i++) {
    var c = ca[i];
    while(c.charAt(0) == ' ') {
      c = c.substring(1);
    }
    if( c.indexOf(name) == 0) {
      return c.substring(name.length, c.length);
    }
  }
  return "";
}

@NgModule({
  declarations: [
    AppComponent,
    LoginPageComponent,
    MainPageComponent,
    PageNotFoundComponent  
  ],
  imports: [
    HttpClientModule,
    StoreModule.forRoot({}),
    StoreDevtoolsModule.instrument({maxAge: 10}),
    JwtModule.forRoot({config: {tokenGetter: tokenGetter}}),
    BrowserModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
