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
import { userReducer } from './store/reducers/user.reducer';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatButtonModule, MatProgressBarModule, MatCheckboxModule, MatIconModule, MatInputModule, MatSelectModule, MatChipsModule, MatGridListModule, MatProgressSpinnerModule, MatSortModule } from '@angular/material';
import {MatTableModule} from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ActivateAccountComponent } from './components/activate-account/activate-account.component';
import { MessagesComponent } from './components/messages/messages.component';
import { messageReducer } from './store/reducers/message.reducer';
import { MessageService } from './services/message/message-service.service';
import { NotificationsComponent } from './components/modal-windows/notifications/notifications.component';
import { SandboxComponent } from './components/sandbox/sandbox.component';
import { pixiInitializeReducer } from './store/reducers/pixi-initialized.reducer';
import { ScriptsPageComponent } from './components/scripts-page/scripts-page.component';
import { AddScriptComponent } from './components/partial/add-script/add-script.component';
import { SelectScriptComponent } from './components/partial/select-script/select-script.component';


@NgModule({
  declarations: [
    AppComponent,
    LoginPageComponent,
    MainPageComponent,
    PageNotFoundComponent,
    ActivateAccountComponent,
    MessagesComponent,
    NotificationsComponent,
    SandboxComponent,
    ScriptsPageComponent,
    AddScriptComponent,
    SelectScriptComponent,
  ],
  imports: [
    HttpClientModule,
    StoreModule.forRoot({
      auth: userReducer,
      messages: messageReducer,
      pixiInitialized: pixiInitializeReducer,
    }),
    StoreDevtoolsModule.instrument({maxAge: 10}),
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    BrowserAnimationsModule,
    MatButtonModule, 
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    ReactiveFormsModule,
    MatSelectModule,
    MatChipsModule,
    MatGridListModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTableModule,
    MatSortModule,
  ],
  providers: [CookieService, MessageService],
  bootstrap: [AppComponent]
})
export class AppModule { }
