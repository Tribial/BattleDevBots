import { Component, OnInit, OnDestroy } from '@angular/core';
import { Command } from 'src/app/models/command.model';
import { Subscription, interval } from 'rxjs';
import { HttpService } from 'src/app/services/http/http.service';
import { ConsoleCommand } from 'src/app/models/console-command.model';
import { map } from 'rxjs/operators'

const source = interval(1000);

@Component({
  selector: 'app-sandbox',
  templateUrl: './sandbox.component.html',
  styleUrls: ['./sandbox.component.scss']
})
export class SandboxComponent implements OnInit, OnDestroy {
  
  public scriptSelected = false;
  public consoleCommands: ConsoleCommand[] = []
  public runSub;
  private _commands: Command[] = [];
  private _subscriptions: Subscription[] = [];

  constructor(private _httpService: HttpService) { 
    source.subscribe(val => this.interpretateResult());
  }

  ngOnInit() {
    setTimeout(() => this.sayHello(), 10);
  }

  sayHello() {
    let com = new Command();
    com.console = "<ISSYSTEM>Hello, first you will need to select a script.";
    this._commands.push(com);
    let com2 = new Command();
    com2.console = "<ISSYSTEM>Do it by clicking the 'Select script' button on the bottom of your screen.";
    this._commands.push(com2);
  }

  runScript() {
    let com = new Command();
    com.console = "<ISSYSTEM>Running script 'testLang.rl'...";
    this._commands.push(com); 
    this._subscriptions.push(
      this._httpService.runScript().subscribe(
        data => {
          let com2 = new Command();
          com2.console = "<ISSYSTEM>Script interpreted successfully. Now executing the outcome...";
          this._commands.push(com2); 
          this._commands.push(...data.model);
        },
        error => {
          console.log(error);
        }
      )
    )
  }

  interpretateResult() {
    console.log("Running...");
    if(this._commands.length !== 0) {
      this.interpretateCommand(this._commands[0]);
      this._commands = this._commands.splice(1);
      console.log('...executing.');
    }
    else {
      console.log('...nothing found.');
    }
  }

  interpretateCommand(command: Command) {
    if(command.console) {
      if(command.console.startsWith('<ISSYSTEM>')) {
        this.consoleCommands.push({content: command.console.split('<ISSYSTEM>')[1], class: 'system'});
      } else {
        this.consoleCommands.push({content: command.console, class: 'message'});
      }
      setTimeout(() => this.scrollDownOnConsole(), 1);
    }
    if(command.error) {
      this.consoleCommands.push({content: command.error, class: 'error'});
      setTimeout(() => this.scrollDownOnConsole(), 1);
    }
  }

  scrollDownOnConsole() {
    let roboConsole = document.getElementById('console');
    roboConsole.scrollTo(0, roboConsole.scrollHeight);
  }

  selectScript() {
    let com = new Command();
    com.console = "<ISSYSTEM>You have selected 'testLang.rl. Now you can run the script by clicking the 'Run' button.";
    this._commands.push(com); 
    this.scriptSelected = true;
  }

  ngOnDestroy(): void {
    this._subscriptions.forEach(s => s.unsubscribe());
    this.runSub.unsubscribe();
  }
}
