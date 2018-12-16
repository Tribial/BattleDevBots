import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { SimpleRobot } from 'src/app/models/simple-robot.model';
import { HttpService } from 'src/app/services/http/http.service';
import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/services/auth/auth.service';
import { MessageService } from 'src/app/services/message/message-service.service';

@Component({
  selector: 'app-add-script',
  templateUrl: './add-script.component.html',
  styleUrls: ['./add-script.component.scss']
})
export class AddScriptComponent implements OnInit {

  @Output() closeEmitter = new EventEmitter();
  public isLoading: boolean = true;
  public displayName: string = '';
  public script: string | ArrayBuffer;
  public robots: SimpleRobot[];
  public selectedRobotId: number;
  public isValid: boolean = true;

  private _errOccured: boolean = false;
  private _subscriptions: Subscription[] = [];

  constructor(private _httpService: HttpService, private _authService: AuthService, private _messageService: MessageService) { 
    this._subscriptions.push(_httpService.getSimpleRobots().subscribe(
      data => {
        this.robots = data.model;
        this.isLoading = false;
      },
      error => {
        if(error.status === 401) {
          _authService.handleUnauthorized();
        } else {
          console.log(error);
        }
      }
    ))
  }

  ngOnInit() {
  }

  fileChange(event) {
    let fileList: FileList = event.target.files;
    if(fileList.length > 0) {
        let file: File = fileList[0];
        let fileReader = new FileReader();
        fileReader.onload = (e) => {
          this.script = fileReader.result;
        }
        fileReader.readAsText(file);
    }
  }

  cancel() {
    this.closeEmitter.emit(null);
  }

  checkDisplayName(): boolean {
    if (this.displayName === '' || 
        this.displayName === undefined || 
        this.displayName === null || 
        this.displayName.length <= 3) {
          this._messageService.addMessage("The display name should have at least 3 characters", 'error', 7);
          return true;
    }
    return false;
  }

  checkRobot(): boolean {
    if(this.selectedRobotId === undefined) {
      this._messageService.addMessage('You need to select a robot', 'error', 5);
      return true;
    }
    return false;
  }

  checkScript(): boolean {
    if (this.script === '' || 
        this.script === undefined || 
        this.script === null) {
          this._messageService.addMessage("You have to select a script", 'error', 5);
          return true;
    }
    return false;
  }

  checkForm() {
    return this.checkDisplayName() || this.checkRobot() || this.checkScript()
  }

  addScript() {
    if(this.checkForm()) {
      return;
    }
    this.isLoading = true;
    this._subscriptions.push(
      this._httpService.addScript(this.displayName, this.selectedRobotId, this.script.toString()).subscribe(
        data => {
          this._messageService.addMessage("Script wass added successfully", 'success', 5);
          this.isLoading = false;
          this.cancel();
        },
        error => {
          if(error.status === 401) {
            this._authService.handleUnauthorized();
            this.isLoading = false;
          } else {
            if(error.error.errorOccured) {
              error.error.errors.forEach(err => {
                this._messageService.addMessage(err, 'error', 5);
              });
            } else {
              console.log(error.error);
            }
            this.isLoading = false;
          }
        }
      )
    )
  }
}
