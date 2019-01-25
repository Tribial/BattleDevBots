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
  //public scriptSelected = false;
  public consoleCommands: ConsoleCommand[] = []
  public runSub;
  public pixiMarginLeft: number;
  public pixiMarginTop: number;
  public showSelectScriptModal: boolean = false;
  public scriptIsRunning: boolean = false;

  // private _playerTrajectory: {x: number, y: number, active: boolean} = {
  //   x: 0,
  //   y: 0,
  //   active: false, 
  // }
  private _playerTrajectory: {affect: string, speed: number, active: boolean, changed: boolean} = {
    affect: 'x',
    speed: 1,
    active: false,
    changed: false,
  }
  private _playerDirection: {direction: number, changing: boolean, speed: number} = {
    direction: 1,
    changing: false,
    speed: 1,
  };
  private _playerTrajectorySprite: PIXI.Sprite;
  private _robot: {
    speed: number;
  } = {speed: 1};
  private _movement: {
    direction: number;
    active: boolean;
  } = {direction: 1, active: false};
  private _scriptHasBegun: boolean = false;
  private _selectedScript: number;
  private _commands: Command[] = [];
  private _subscriptions: Subscription[] = [];
  private _pixiTextures: any[] = [];
  private _player: PIXI.Sprite;
  private _stones: PIXI.Sprite[] = [];

  constructor(private _httpService: HttpService, private _router: Router, private _authService: AuthService) { 
    this.runSub = source.subscribe(val => this.interpretateResult());
  }

  ngOnInit() {
    setTimeout(() => this.sayHello(), 10);
    this.pixiApp = new PIXI.Application({ width: 800, height: 600 });
    this.pixiContainer.nativeElement.appendChild(this.pixiApp.view);
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
        `${bots}laser.png`,
      ])
      .on('progress', (loader, resource) => this._pixiLoadProgress(loader, resource))
      .load(() => this._pixiSetup());
  }

  _pixiLoadProgress(loader, resource) {
    console.log("Progress: ", loader.progress, "%; Resource: ", resource.url);
  }

  _pixiGameLoop(delta) {
    if(this.isColiding(this._player)) {
      this.endGame(false);
    }
    if(this._playerTrajectory.active && this.isColiding(this._playerTrajectorySprite)) {
      this._playerTrajectory.active = false;
      this._playerTrajectory.changed = true;
    }
    if(this._playerTrajectory.changed) {
      if(this._playerTrajectory.active) {
        this.pixiApp.stage.addChild(this._playerTrajectorySprite);
      } else {
        this.pixiApp.stage.removeChild(this._playerTrajectorySprite);
      }
    }
    if(this._playerTrajectory.active) {
      if(this._playerTrajectory.affect === 'y') {
        this._playerTrajectorySprite.y += delta * this._playerTrajectory.speed;
      } else {
        this._playerTrajectorySprite.x += delta * this._playerTrajectory.speed;
      }
    }
    if(this._playerDirection.changing) {
      //console.log(this._playerDirection.speed);
      this._player.rotation += delta / 50 * 3.14 / 2 * this._playerDirection.speed;
    }
    if(this._movement.active) {
      switch(this._movement.direction) {
        case 1:
          this._player.y -= delta;
          break;
        case 2:
          this._player.x += delta;
          break;
        case 3:
          this._player.y += delta;
          break;
        case 4:
          this._player.x -= delta; 
          break;
      }
    }
  }

  _pixiSetup() {
    this.pixiApp.ticker.speed = 5/6;
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
      y++;
    }

    this._player = new PIXI.Sprite(this.pixiApp.loader.resources[`${bots}tank2_blue_transparent.png`].texture);
    this._player.x = 7 * 50 + 25;
    this._player.y = 9 * 50 + 25;
    this._player.anchor.set(0.5, 0.5);

    this._playerTrajectorySprite = new PIXI.Sprite(this.pixiApp.loader.resources[`${bots}laser.png`].texture);
    this._playerTrajectorySprite.x = this._player.x;
    this._playerTrajectorySprite.y = this._player.y - 50;
    this._playerTrajectorySprite.anchor.set(0.5, 0.5);

    tailCollection.forEach(tail => {
      this.pixiApp.stage.addChild(tail);
    });
    borderRockCollection.forEach(rock => {
      this._stones.push(rock);
      this.pixiApp.stage.addChild(rock);
    });
    this.pixiApp.stage.addChild(this._player);
    //this.pixiApp.stage.addChild(this._playerTrajectorySprite);

    this.pixiApp.ticker.add(delta => this._pixiGameLoop(delta));
  }

  endGame(win: boolean) {
    this.pixiApp.stop();
    if(win) {

    } else {
      this._commands = [];
      let com = new Command();
      com.console = "<ISSYSTEM>Your health points went down to zero, you have lost";
      this._commands.push(com);
    }
  }

  isColiding(object: PIXI.Sprite): boolean {
    let colides = false;
    this._stones.forEach(stone => {
      if(object.x > 715) {
        colides = true;
        return;
      }
      if(object.x < 35) {
        colides = true;
        return;
      }
      if(object.y > 515) {
        colides = true;
        return;
      }
      if(object.y < 35) {
        colides = true;
        return;
      }
    })
    return colides;
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
    this.scriptIsRunning = true;
    let com = new Command();
    com.console = "<ISSYSTEM>Running script 'testLang.rl'...";
    this._commands.push(com); 
    this._subscriptions.push(
      this._httpService.runScript(this._selectedScript).subscribe(
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
                let com3 = new Command();
                com3.error = "!! Script executed with errors, see above. !!";
                this._commands.push(com3);
              }
            }
          }
          console.log(error);
        }
      )
    )
  }

  interpretateResult() {
    if(this._movement.active) {
      this._movement.active = false;
      // this._test2.push(this._test3);
      // this._test3 = 0;
      // console.log(this._test2);
    }
    if(this._playerDirection.changing) {
      this._playerDirection.changing = false;
    }
    if(this._commands.length !== 0) {
      this.interpretateCommand(this._commands[0]);
      this._commands = this._commands.splice(1);
      this._scriptHasBegun = true;
    }
    else {
      if(this._scriptHasBegun) {
        this.scriptIsRunning = false;
      }
    }
  }

  scriptSelected = () => this._selectedScript !== undefined;

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
    if(command.type === "MOVE") {
      this.consoleCommands.push({content: 'Robot is moving ' + this.getDirectionString(command.direction), class: 'info'})
      this._movement = {
        direction: (this._playerDirection.direction + command.direction - 1) % 4 === 0 ? 
        this._playerDirection.direction + command.direction - 1 : (this._playerDirection.direction + command.direction - 1)%4, 
        active: true
      };
      console.log('Robot is moving ' + this.getDirectionString(command.direction), this._movement);
      setTimeout(() => this.scrollDownOnConsole(), 1);
    } else if (command.type === "ATTACK") {
      this.consoleCommands.push({content: 'Robot is attacking', class: 'info'});
      this.calculateTrajectory();
      setTimeout(() => this.scrollDownOnConsole(), 1);
    } else if (command.type === "TURN") {
      this.consoleCommands.push({content: 'Robot is turning ' + this.getDirectionString(command.direction), class: 'info'});
      this.turnPlayer(command.direction);
      setTimeout(() => this.scrollDownOnConsole(), 1);
    }
  }

  turnPlayer(direction: number) {
    this._playerDirection.speed = direction - this._playerDirection.direction;
    this._playerDirection.direction = direction;
    this._playerDirection.changing = true;
  }

  calculateTrajectory() {
    this._playerTrajectorySprite.rotation = 3.14 / 2 * (this._playerDirection.direction - 1);
    this._playerTrajectorySprite.y = this._playerDirection.direction === 1 ? 
    this._player.y - 1 : 
    (this._playerDirection.direction === 3 ? 
      this._player.y + 1 : 
      this._player.y);

    this._playerTrajectorySprite.x = this._playerDirection.direction === 2 ? 
    this._player.x + 1 : 
    (this._playerDirection.direction === 4 ? 
      this._player.x - 1 : 
      this._player.x);

    if(!this.isColiding(this._playerTrajectorySprite)) {
      this._playerTrajectory.affect = this._playerDirection.direction === 1 || this._playerDirection.direction === 3 ? 'y' : 'x';
      let tails = 0;
      let tempSprite = new PIXI.Sprite();
      tempSprite.x = this._playerTrajectorySprite.x;
      tempSprite.y = this._playerTrajectorySprite.y;
      while(true) {
        if(this._playerTrajectory.affect === 'y') {
          if(this._playerDirection.direction === 1) {
            tempSprite.y -= 50;
            tails--;
          } else {
            tempSprite.y += 50;
            tails++;
          }
        } else {
          if(this._playerDirection.direction === 2) {
            tempSprite.x += 50;
            tails++;
          } else {
            tempSprite.x -= 50;
            tails--;
          }
        }
        if(this.isColiding(tempSprite)) {
              break;
        }
      }
      this._playerTrajectory.speed = tails;
      this._playerTrajectory.active = true;
      this._playerTrajectory.changed = true;
    }
  }

  getDirectionString(dirNum: number) {
    switch(dirNum) {
      case 1:
        return "forward";
      case 2:
        return "right";
      case 3:
        return "back";
      case 4:
        return "left";
    }
  }

  scrollDownOnConsole() {
    let roboConsole = document.getElementById('console');
    roboConsole.scrollTo(0, roboConsole.scrollHeight);
  }

  selectScript() {
    this.showSelectScriptModal = true;
    
  }

  selectedScript(event) {
    this.showSelectScriptModal = false;
    this._selectedScript = event;
    let com = new Command();
    com.console = "<ISSYSTEM>You have selected 'testLang.rl. Now you can run the script by clicking the 'Run' button.";
    this._commands.push(com);
  }

  ngOnDestroy(): void {
    this._subscriptions.forEach(s => s.unsubscribe());
    this.runSub.unsubscribe();
    this.pixiApp.loader.destroy();
    this.pixiApp.stage.destroy();
    this.pixiApp.destroy();
  }
}
