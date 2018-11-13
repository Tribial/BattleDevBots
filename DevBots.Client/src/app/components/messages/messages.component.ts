import { Component, OnInit, OnDestroy } from '@angular/core';
import {
  trigger,
  state,
  style,
  animate,
  transition
} from '@angular/animations';
import * as MessageActions from '../../store/actions/message.actions';
import { Message } from '../../models/message.model';
import { AppState } from '../../store/state/app.state';
import { Store } from '@ngrx/store';
import { Subscription, interval } from 'rxjs';


@Component({
  selector: 'app-messages',
  templateUrl: './messages.component.html',
  styleUrls: ['./messages.component.scss'],
  animations: [
    trigger('openClose', [
      state('open', style({
        opacity: 1,
      })),
      state('closed', style({
        opacity: 0,
      })),
      transition('open => closed', [
        animate('1s')
      ]),
      transition('closed => open', [
        animate('0.5s')
      ]),
    ]),
  ]
})
export class MessagesComponent implements OnInit, OnDestroy {

  messages: Message[];
  private subscriptions: Subscription[] = [];

  constructor(private _store: Store<AppState>) { 
    this.subscriptions.push(
      _store.select<Message[]>('messages').subscribe(
      data => this.messages = data
    ));
    this.subscriptions.push(
      interval(500).subscribe(i => {
        this.check();
      }
    ));
  }


  check() {
    let idsToRemove: number[] = [];
    this.messages.forEach(m => {
      m.duration -= 0.5;
      if(m.duration === -0.5) {
        idsToRemove.push(m.id);
      }
    });
    idsToRemove.forEach(i => this.messages = this.messages.filter(m => m.id !== i));
    this._store.dispatch(new MessageActions.UpdateAll(this.messages));
  }

  ngOnInit() {
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    
  }
}
