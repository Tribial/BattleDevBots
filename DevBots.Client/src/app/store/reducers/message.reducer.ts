import { Message } from '../../models/message.model';
import * as MessageActions from '../actions/message.actions';

const initialState: Message[] = [];

export function messageReducer(state: Message[] = initialState, action: MessageActions.Actions) {
    switch(action.type) {
        case MessageActions.ADD_MESSAGE:
            return [...state, action.payload];
        case MessageActions.UPDATE_MESSAGES:
            return action.payload;
        case MessageActions.REMOVE_MESSAGE: 
            return state.filter(s => s.id !== action.payload);
        default:
            return state;
    }
}

