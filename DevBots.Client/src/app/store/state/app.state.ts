import { LoginModel } from "../../models/login-model.model";
import { Message } from '../../models/message.model';

export interface AppState {
    readonly auth: LoginModel;
    readonly messages: Message[];
  }