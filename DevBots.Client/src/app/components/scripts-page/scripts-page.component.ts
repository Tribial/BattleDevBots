import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Script } from 'src/app/models/script.model';
import { MatIconRegistry } from '@angular/material';
import { DomSanitizer } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { HttpService } from 'src/app/services/http/http.service';
import { AuthService } from 'src/app/services/auth/auth.service';
import { MessageService } from 'src/app/services/message/message-service.service';

@Component({
  selector: 'app-scripts-page',
  templateUrl: './scripts-page.component.html',
  styleUrls: ['./scripts-page.component.scss']
})
export class ScriptsPageComponent implements OnInit, OnDestroy {

  public scripts: Script[] = [];
  public displayedColumns = ['name', 'lastUpdate', 'forBot', 'lines', 'actions'];
  public showAddScript: boolean = false;
  public isLoading: boolean = true;

  private _subscriptions: Subscription[] = [];

  constructor(private _messageService: MessageService, private _router: Router, iconRegistry: MatIconRegistry, sanitizer: DomSanitizer, private _httpService: HttpService, private _authService: AuthService) {
    iconRegistry.addSvgIcon(
      'upload',
      sanitizer.bypassSecurityTrustResourceUrl('assets/icons/cloud_upload.svg'));
    iconRegistry.addSvgIcon(
      'download',
      sanitizer.bypassSecurityTrustResourceUrl('assets/icons/cloud_download.svg'));
    iconRegistry.addSvgIcon(
      'delete',
      sanitizer.bypassSecurityTrustResourceUrl('assets/icons/delete.svg'));
   }

  ngOnInit() {
    this.getScripts();
  }

  removeScript(scriptId: number) {
    this.isLoading = true;
    this._subscriptions.push(
      this._httpService.removeScript(scriptId).subscribe(
        data => {
          this._messageService.addMessage("Script was remove successfully", "success", 5);
          this.getScripts();
        },
        error => {
          if(error.status === 401) {
            this._authService.handleUnauthorized();
            this.isLoading = false;
          } else if(error.status === 500) {
            this._messageService.addMessage('Script was not removed due to an error', 'error', 7);
            this.isLoading = false;
          } else {
            if(error.error.errorOccured) {
              error.error.errors.forEach(err => {
                this._messageService.addMessage(err, 'error', 7);
              });
            } else {
              console.log(error.error);
            }
            this.isLoading = false;
          }
        }
      )
    );
  }

  showFormToAddScript() {
    this.showAddScript = true;
  }

  getScripts() {
    this._subscriptions.push(
      this._httpService.getScriptsByUser().subscribe(
        data => {
          this.scripts = data.model;
          this.scripts.forEach(s => {
            console.log(s.lastUpdate);
            s.lastUpdate = new Date(s.lastUpdate);
            //s.lastUpdate.toLocaleTimeString
            s.lastUpdate = new Date(s.lastUpdate.getFullYear(), s.lastUpdate.getMonth(), s.lastUpdate.getDate(), s.lastUpdate.getHours(), s.lastUpdate.getMinutes(), s.lastUpdate.getSeconds());
          })
          this.isLoading = false;
        },
        error => {
          if(error.status === 401) {
            this._authService.handleUnauthorized();
            this.isLoading = false;
          } else {
            if(error.error.errorOccured) {
              error.error.errors.forEach(err => {
                this._messageService.addMessage(err, 'error', 7);
              });
            } else {
              console.log(error.error);
            }
            this.isLoading = false;
          }
        }
      )
    );
  }

  returnToMenu() {
    this._router.navigate(['/']);
  }

  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    this._subscriptions.forEach(s => s.unsubscribe());
  }
}
