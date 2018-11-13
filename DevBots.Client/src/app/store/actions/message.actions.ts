import { Action } from '@ngrx/store';
import { Message } from '../../models/message.model';

export const ADD_MESSAGE        = '[MESSAGE] Add';
export const UPDATE_MESSAGES    = '[MESSAGE] Update all';
export const REMOVE_MESSAGE     = '[MESSAGE] Remove';

export class UpdateAll implements Action {
    readonly type = UPDATE_MESSAGES;
    constructor(public payload: Message[]) {}
}

export class AddMessage implements Action {
    readonly type = ADD_MESSAGE;
    constructor(public payload: Message) {}
}

export class RemoveMessage implements Action {
    readonly type = REMOVE_MESSAGE;
    constructor(public payload: number) {}
}

export type Actions = AddMessage | RemoveMessage | UpdateAll;