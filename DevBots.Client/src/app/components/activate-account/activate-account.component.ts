import { Component, OnInit } from '@angular/core';
import {
  trigger,
  state,
  style,
  animate,
  transition
} from '@angular/animations';
import { Router, ActivatedRoute } from '@angular/router';
import { MessageService } from '../../services/message/message-service.service';
import { HttpService } from '../../services/http/http.service';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-activate-account',
  templateUrl: './activate-account.component.html',
  styleUrls: ['./activate-account.component.scss'],
  animations: [
    trigger('loading', [
      state('start', style({
        width: '100px',
        height: '100px',
        top: 'calc(50% - 55px)',
        left: 'calc(50% - 55px)',
      })),
      state('stop', style({
        width: window.innerWidth,
        height: window.innerWidth,
        top: `calc(50% - ${window.innerWidth/2 + 5}px)`,
        left: `calc(50% - ${window.innerWidth/2 + 5}px)`,
        'border-style': 'solid',
        'border-color': 'indigo',
        'border-width': '10px'
      })),
      transition('* => stop', [
        animate('0.2s')
      ]),
    ]),
  ]
})
export class ActivateAccountComponent implements OnInit {
  stopLoading: boolean = false;
  private _userGuid: string;
  private _subscriptions: Subscription[] = [];
  private _count: number;
  private _messageContent: string;
  private _redirectTo: string;
  private _messageId: number;

  constructor(private _router: Router, private _route: ActivatedRoute, private _messageService: MessageService, private _httpService: HttpService) { 
    this._userGuid = _route.snapshot.paramMap.get('userGuid');
    _messageService.addMessage("Your account is being activated, please wait a few secodns", "message", 7);
  }

  ngOnInit() {
    setTimeout(() => this.activateAccount(), 3000);
  }

  countDown() {
    this._count -= 1;
    this._messageService.editMessage(this._messageId, this._messageContent + this._count);
    console.log(this._redirectTo);
    if(this._count === 0) {
      switch(this._redirectTo) {
        case "login":
          this._router.navigate(['account', 'login']);
          break;
        case "register":
          this._router.navigate(['account', 'register']);
          break;
      }
    }
  }

  activateAccount() {
    this._subscriptions.push(
      this._httpService.activate(this._userGuid).subscribe(
        data => {
          this.stopLoading = true;
          this._count = 10;
          this._redirectTo = "login";
          this._messageContent = "Your account has been activated, you will be redirected to the login page in ";
          this._messageId = this._messageService.addMessage(this._messageContent + this._count, "success", 10);
          this._subscriptions.push(interval(1000).subscribe(i => this.countDown()));
        },
        error => {
          this.stopLoading = true;
          this._count = 10;
          this._redirectTo = 'register';
          if(error.error !== undefined && error.error !== null) {
            this._messageContent = error.error.errors[0] + ". You will be redirected to the register page in ";
          } else {
            this._messageContent = "Something went wrong, please contact DevBots support or try again later. You will be redirected to the register page in ";
          }

          this._messageId = this._messageService.addMessage(this._messageContent + this._count, "error", 10);
          this._subscriptions.push(interval(1000).subscribe(i => this.countDown()));
        }
      )
    );
  }

  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    this._subscriptions.forEach(s => s.unsubscribe());
  }
}
