import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { SimpleRobot } from 'src/app/models/simple-robot.model';
import { SimpleScript } from 'src/app/models/simple-script.model';
import { Subscription } from 'rxjs';
import { HttpService } from 'src/app/services/http/http.service';
import { AuthService } from 'src/app/services/auth/auth.service';
import { MessageService } from 'src/app/services/message/message-service.service';

@Component({
  selector: 'app-select-script',
  templateUrl: './select-script.component.html',
  styleUrls: ['./select-script.component.scss']
})
export class SelectScriptComponent implements OnInit {
  public robots: SimpleRobot[] = [];
  public scripts: SimpleScript[] = []
  public selectedRobotId: number;
  public selectedScriptId: number;
  public isLoading: boolean = true;
  public done: boolean = false;
  @Output() scriptEmitter = new EventEmitter<number>();
  @Output() cancelEmitter = new EventEmitter();

  private _subscriptions: Subscription[] = []

  constructor(private _httpService: HttpService, private _authService: AuthService, private _messageService: MessageService) { }

  ngOnInit() {
    this.getBots();
  }

  getBots() {
    this._subscriptions.push(this._httpService.getSimpleRobots().subscribe(
      data => {
        this.robots = data.model;
        this.isLoading = false;
      },
      error => {
        if(error.status === 401) {
          this._authService.handleUnauthorized();
        } else {
          console.log(error);
        }
      }
    ))
  }

  getScripts() {
    this._subscriptions.push(this._httpService.getSimpleScriptsByRobot(this.selectedRobotId).subscribe(
      data => {
        this.scripts = data.model;
        this.isLoading = false;
      },
      error => {
        if(error.status === 401) {
          this._authService.handleUnauthorized();
        } else {
          console.log(error);
        }
      }
    ));
  }

  robotChange() {
    this.isLoading = true;
    this.getScripts();
    this.done = false;
    this.selectedScriptId = undefined;
  }

  scriptChange() {
    this.done = true;
  }

  select() {
    this.scriptEmitter.emit(this.selectedScriptId);
  }

  cancel() {
    this.cancelEmitter.emit(null);
  }

  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    this._subscriptions.forEach(s => s.unsubscribe());
  }
}
