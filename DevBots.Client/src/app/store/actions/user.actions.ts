import { Injectable } from '@angular/core'
import { Action } from '@ngrx/store'
import { LoginModel } from '../../models/login-model.model';

export const SET_TOKEN      = '[TOKEN] Set';
export const REMOVE_TOKEN   = '[TOKEN] Remove'

export class SetToken implements Action {
    readonly type = SET_TOKEN;
    constructor(public payload: LoginModel) {}
}

export class RemoveToken implements Action {
    readonly type = REMOVE_TOKEN;
    constructor() {}
}

export type Actions = SetToken | RemoveToken;