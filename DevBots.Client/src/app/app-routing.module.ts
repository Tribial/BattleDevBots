import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { LoginPageComponent } from './components/login-page/login-page.component';
import { MainPageComponent } from './components/main-page/main-page.component';
import { AuthGuardService } from './services/auth-guard/auth-guard.service';
import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { ActivateAccountComponent } from './components/activate-account/activate-account.component';
import { SandboxComponent } from './components/sandbox/sandbox.component';
import { ScriptsPageComponent } from './components/scripts-page/scripts-page.component';

const routes: Routes = [
  {path: 'account/:type', component: LoginPageComponent},
  {path: '', component: MainPageComponent, canActivate: [AuthGuardService]},
  {path: 'sandbox', component: SandboxComponent, canActivate: [AuthGuardService]},
  {path: 'scripts', component: ScriptsPageComponent, canActivate: [AuthGuardService]},
  {path: 'activate/:userGuid', component: ActivateAccountComponent},
  {path: '**', component: PageNotFoundComponent},
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
