import { Component, OnInit, HostListener, OnDestroy } from '@angular/core';
import { GridItem } from 'src/app/models/gridItem.model';
import { MatGridList } from '@angular/material';
import { AuthService } from 'src/app/services/auth/auth.service';
import { HttpService } from 'src/app/services/http/http.service';
import { Subscription } from 'rxjs';
import { MessageService } from 'src/app/services/message/message-service.service';

@Component({
  selector: 'app-main-page',
  templateUrl: './main-page.component.html',
  styleUrls: ['./main-page.component.scss']
})
export class MainPageComponent implements OnInit, OnDestroy {

  private _keyFocusOn: string = 'grid';
  private _bottomButtons: {id: number, name: string}[] = [
    {
      id: 0,
      name: 'notification',
    },
    {
      id: 1,
      name: 'logout',
    },
  ];
  private _subscriptions: Subscription[] = [];

  public isWaiting: boolean = false;
  public buttonFocused: string = '';
  public mouseEnabled: boolean;
  public currentDescription = '';
  public gridList: GridItem[] = [
    {
      id: 0,
      title: 'Tasks',
      disabled: true,
      activated: false,
      description: 'Here you can complete tasks to get points and test your knowledge',
    },
    {
      id: 1,
      title: 'Arena',
      disabled: false,
      activated: false,
      description: 'Here you can send your robots to fight other players',
    },
    {
      id: 2,
      title: 'Sandbox',
      disabled: false,
      activated: false,
      description: 'Here you can test scripts on a empty arena field',
    },
    {
      id: 3,
      title: 'Program',
      disabled: false,
      activated: false,
      description: 'Here you can create a script based on a robot movement',
    },
    {
      id: 4,
      title: 'Robots',
      disabled: false,
      activated: false,
      description: 'Here you can see all robots, their skills and appearance',
    },
    {
      id: 5,
      title: 'Documentation',
      disabled: true,
      activated: false,
      description: 'Here you can learn how to code in RoboLang',
    },
    {
      id: 6,
      title: 'Your account',
      disabled: false,
      activated: false,
      description: 'Here you can see your profile and edit yout profile or settings',
    },
    {
      id: 7,
      title: 'Highscore',
      disabled: false,
      activated: false,
      description: 'Here you can see what have other people achieved',
    },
    {
      id: 8,
      title: 'Scripts',
      disabled: false,
      activated: false,
      description: 'Here you can add, modify or delete your scripts',
    },
  ];

  constructor(private _auth: AuthService, private _http: HttpService, private _messageService: MessageService) {
    this.setFirstAvailableGridItemAsActivated()
  }


  ngOnInit() {
  }

  setFirstAvailableGridItemAsActivated() {
    for(let i = 0; i < this.gridList.length; i++) {
      if(!this.gridList[i].disabled) {
        this.gridList[i].activated = true;
        this.currentDescription = this.gridList[i].description;
        break;
      }
    }
  }

  changeSelecetdGrid(index: number) {
    if(this.isWaiting) {
      return;
    }
    if(this.gridList[index].disabled) {
      return 'disabled';
    }
    if(index > this.gridList.length) {
      return 'outOfIndex';
    }
    this.gridList.forEach(item => {
      item.activated = false;
    });
    this.gridList[index].activated = true;
    this.currentDescription = this.gridList[index].description;
    return 'done';
  }

  @HostListener('document:keydown', ['$event']) 
  onKeydownHandler(event: KeyboardEvent) {
    if(this.isWaiting) {
      return;
    }
    if(this._keyFocusOn === 'grid') {
      let itemIndex = this.gridList.findIndex(i => i.activated);
      let result = 'error';
      while(result !== 'done' && result !== 'outOfIndex') {
        if (event.keyCode === 37) {
          itemIndex -= 1;
        } else if (event.keyCode === 38) {
          itemIndex -= 3;
        } else if (event.keyCode === 39) {
          itemIndex += 1;
        } else if (event.keyCode === 40) {
          itemIndex += 3;
        }

        result = this.changeSelecetdGrid(itemIndex);
      }
    } else {
      let itemIndex = this._bottomButtons.find(b => b.name === this.buttonFocused).id;
      if (event.keyCode === 37) {
        itemIndex -= 1;
        if(itemIndex < 0) {
          itemIndex = 0;
        }
      } else if (event.keyCode === 39) {
        itemIndex += 1;
        if(itemIndex > this._bottomButtons.length - 1) {
          itemIndex = this._bottomButtons.length - 1;
        }
      }
      this.buttonFocused = this._bottomButtons[itemIndex].name;
    }
    
  }

  @HostListener('document:keydown.enter', ['$event'])
  onEnterPress() {
    if(this.isWaiting) {
      return;
    }
    if(this._keyFocusOn === 'grid') {
      this.gridList.forEach(item => {
        if(item.activated) {
          alert(item.title);
        }
      });
    } else {
      switch(this.buttonFocused) {
        case 'notification':
          alert('NOTIFICATIONS');
          break;
        case 'logout':
          this.logout();
          break;
      }
    }
  }
  @HostListener('document:keydown.tab', ['$event'])
  onTagPress() {
    if(this.isWaiting) {
      return;
    }
    if(this._keyFocusOn === 'grid') {
      this.gridList.forEach(item => item.activated = false);
      this.currentDescription = '';
      this.buttonFocused = this._bottomButtons[0].name;
      this._keyFocusOn = 'panel';
    } else {
      this.setFirstAvailableGridItemAsActivated();
      this.buttonFocused = '';
      this._keyFocusOn = 'grid';
    }
    return false;
  }

  logout() {
    if(this.isWaiting) {
      return;
    }
    this.isWaiting = true;
    this._http.logout().subscribe(
      data => {
        console.log(data);
        this._auth.handleUnauthorized();
        this.isWaiting = false;
      },
      error => {
        if(error.status === 401) {
          this.isWaiting = false;
          this._auth.handleUnauthorized();
        } else {
          this._messageService.addMessage("Something went wrong", 'error');
          console.log(error);
        }
      }
    );
  }

  ngOnDestroy(): void {
    this._subscriptions.forEach(s => s.unsubscribe());
  }
}
