import * as PixiInitializedActions from '../actions/pixi-initialized.actions';

const initialState = false;

export function pixiInitializeReducer(state: boolean = initialState, action: PixiInitializedActions.Actions) {
    switch(action.type) {
        case PixiInitializedActions.SET_AS_INITIALIZED:
            return true;
        case PixiInitializedActions.SET_AS_NOT_INITIALIZED:
            return false;
        default:
            return state;
    }
}