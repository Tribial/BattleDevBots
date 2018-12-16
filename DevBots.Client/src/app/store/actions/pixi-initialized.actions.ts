import { Action } from '@ngrx/store';

export const SET_AS_INITIALIZED     = '[PIXI] Initialized';
export const SET_AS_NOT_INITIALIZED = '[PIXI] Deinitialized';

export class InitializePixi implements Action {
    type = SET_AS_INITIALIZED;
    constructor() {}
}

export class DeinitializePixi implements Action {
    type = SET_AS_NOT_INITIALIZED;
    constructor() {}
}

export type Actions = InitializePixi | DeinitializePixi;