import { Message } from '../../models/message.model';
import * as MessageActions from '../../store/actions/message.actions';
import { Store } from '@ngrx/store';
import { AppState } from '../../store/state/app.state';
import { Subscription } from 'rxjs';
import { Injectable } from '@angular/core';

@Injectable()
export class MessageService {
    private _messages: Message[];
    private _subscriptions: Subscription[] = [];

    constructor(private _store: Store<AppState>) {
        this._subscriptions.push(
            _store.select<Message[]>('messages').subscribe(data => this._messages = data)
        );
    }

    public addMessage(content: string, type: string, duration: number = 5) {
        let message: Message = {
            id: this._messages.length > 0 ? this._messages[this._messages.length - 1].id + 1 : 0,
            content: content,
            duration: duration,
            type: type,
            show: true,
        }

        this._store.dispatch(new MessageActions.AddMessage(message));
        return message.id;
    }

    public editMessage(id: number, content: string) {
        this._messages.forEach(m => {
            if(m.id === id) {
                m.content = content;
            }
        });
        this._store.dispatch(new MessageActions.UpdateAll(this._messages));
    }

    public removeAll() {
        this._messages = [];
        this._store.dispatch(new MessageActions.UpdateAll(this._messages));
    }
}
