import { Component, OnInit, OnDestroy, ViewChild, HostListener } from '@angular/core';
import { Command } from 'src/app/models/command.model';
import { Subscription, interval, timer } from 'rxjs';
import { HttpService } from 'src/app/services/http/http.service';
import { ConsoleCommand } from 'src/app/models/console-command.model';
import * as PIXI from 'pixi.js/dist/pixi.js';
import { map } from 'rxjs/operators'
import { Time } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from 'src/app/services/auth/auth.service';

const source = interval(1000);
const tails = '../../../assets/images/game/tails/';
const mapElements = '../../../assets/images/game/map_elements/';
const bots = '../../../assets/images/game/bots/';
//declare var PIXI: any; // instead of importing pixi like some tutorials say to do use declare

@Component({
  selector: 'app-sandbox',
  templateUrl: './sandbox.component.html',
  styleUrls: ['./sandbox.component.scss']
})
export class SandboxComponent implements OnInit, OnDestroy {
  
  @ViewChild('pixiContainer') pixiContainer; // this allows us to reference and load stuff into the div container

  public pixiApp: PIXI.Application; // this will be our pixi application
  public scriptSelected = false;
  public consoleCommands: ConsoleCommand[] = []
  public runSub;
  public pixiMarginLeft: number;
  public pixiMarginTop: number;
  private _commands: Command[] = [];
  private _subscriptions: Subscription[] = [];
  private _pixiTextures: any[] = [];
  private _player: PIXI.Sprite;

  constructor(private _httpService: HttpService, private _router: Router, private _authService: AuthService) { 
    this.runSub = source.subscribe(val => this.interpretateResult());
  }

  ngOnInit() {
    setTimeout(() => this.sayHello(), 10);
    this.pixiApp = new PIXI.Application({ width: 800, height: 600 }); // this creates our pixi application
    this.pixiContainer.nativeElement.appendChild(this.pixiApp.view); // this places our pixi application onto the viewable document
    this.pixiApp.loader
      .add([
        `${tails}tail_1.jpg`,
        `${tails}tail_2.jpg`,
        `${tails}tail_3.jpg`,
        `${tails}tail_4.jpg`,
        `${tails}tail_5.jpg`,
        `${tails}tail_6.jpg`,
        `${tails}tail_7.jpg`,
        `${mapElements}border_rock_transparent.png`,
        `${bots}tank2_blue_transparent.png`,
      ])
      .on('progress', (loader, resource) => this._pixiLoadProgress(loader, resource))
      .load(() => this._pixiSetup());
  }

  _pixiLoadProgress(loader, resource) {
    console.log("Progress: ", loader.progress, "%; Resource: ", resource.url);
  }

  _pixiGameLoop(delta) {
    //this._player.y--;
  }

  _pixiSetup() {
    let tailCollection: PIXI.Sprite[] = [];
    let borderRockCollection: PIXI.Sprite[] = [];
    let x = 0;
    let y = 0;
    while(y < 12) {
      while(x < 16) {
        let tailNum = Math.floor(Math.random() * 7 + 1);
        let tailTex = new PIXI.Sprite(this.pixiApp.loader.resources[`${tails}tail_${tailNum}.jpg`].texture);
        tailTex.x = x * 50;
        tailTex.y = y * 50;
        tailCollection.push(tailTex);
        if(x === 0 || x === 15 || y === 0 || y === 11) {
          let borderRock = new PIXI.Sprite(this.pixiApp.loader.resources[`${mapElements}border_rock_transparent.png`].texture);
          borderRock.anchor.x = 0.5;
          borderRock.anchor.y = 0.5;
          borderRock.x = x * 50 + 25;
          borderRock.y = y * 50 + 25;
          if(x === 0 && y === 0 || x === 15 && y === 11) {
            borderRock.rotation = -0.5;
          } else if(x === 0 && y === 11 || x === 15 && y === 0) {
            borderRock.rotation = 0.5;
          } else if(x === 0 || x === 15) {
            borderRock.rotation = 1;
          }
          borderRockCollection.push(borderRock);
        }
        x++;
      }
      x = 0;
      y++
    }

    this._player = new PIXI.Sprite(this.pixiApp.loader.resources[`${bots}tank2_blue_transparent.png`].texture);
    this._player.x = 7 * 50;
    this._player.y = 9 * 50;

    tailCollection.forEach(tail => {
      this.pixiApp.stage.addChild(tail);
    });
    borderRockCollection.forEach(rock => {
      this.pixiApp.stage.addChild(rock);
    });
    this.pixiApp.stage.addChild(this._player);

    this.pixiApp.ticker.add(delta => this._pixiGameLoop(delta));
  }

  sayHello() {
    let com = new Command();
    com.console = "<ISSYSTEM>Hello, first you will need to select a script.";
    this._commands.push(com);
    let com2 = new Command();
    com2.console = "<ISSYSTEM>Do it by clicking the 'Select script' button on the bottom of your screen.";
    this._commands.push(com2);
  }

  returnToMenu() {
    this._router.navigate(['/']);
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
          let com3 = new Command();
          com3.console = "<ISSYSTEM>Script executed successfully";
          this._commands.push(com3);
        },
        error => {
          if(error.status === 401) {
            this._authService.handleUnauthorized();
          }
          else {
            if(error.error !== undefined) {
              if(error.error.errorOccured === true) {
                error.error.errors.forEach(err => {
                  let com = new Command();
                  com.error = err;
                  this._commands.push(com);
                });
              }
            }
          }
          console.log(error);
        }
      )
    )
  }

  interpretateResult() {
    //console.log("Running...");
    if(this._commands.length !== 0) {
      this.interpretateCommand(this._commands[0]);
      this._commands = this._commands.splice(1);
      //console.log('...executing.');
    }
    else {
      //console.log('...nothing found.');
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
    this.pixiApp.loader.destroy();
    this.pixiApp.stage.destroy();
    this.pixiApp.destroy();
  }
}
