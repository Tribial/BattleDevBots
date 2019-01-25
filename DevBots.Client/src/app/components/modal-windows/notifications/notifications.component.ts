import { Component, OnInit, Input, Output, HostListener, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { Notification } from 'src/app/models/notification.model';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit, OnChanges {

  @Input() notificationSelected: number;
  @Output() close: EventEmitter<any> = new EventEmitter<any>();
  @Output() setNotification: EventEmitter<number> = new EventEmitter<number>();
  public notifications: Notification[] = [
    {
      id: 1,
      title: 'Allah attacked you',
      body: 'Allah attacked you, you have won the battle and gained 50 points',
    },
    {
      id: 2,
      title: 'New robots',
      body: 'A new robots is available for you to use, check it out',
    },
    {
      id: 3,
      title: 'Alex attacked you',
      body: 'Alex attacked you, you have lost battle',
    },
    {
      id: 1,
      title: 'Michael attacked you',
      body: 'Michael attacked you, you have won the battle and gained 50 points',
    },
    {
      id: 2,
      title: 'New robots',
      body: 'A new robots is available for you to use, check it out',
    },
    {
      id: 3,
      title: 'Tommy attacked you',
      body: 'Tommy attacked you, you have lost battle',
    },
    {
      id: 1,
      title: 'Charlie attacked you',
      body: 'Charlie attacked you, you have won the battle and gained 50 points',
    },
    {
      id: 2,
      title: 'New robots',
      body: 'A new robots is available for you to use, check it out',
    },
    {
      id: 3,
      title: 'Karol attacked you',
      body: 'Karol attacked you, you have lost battle',
    },
    {
      id: 1,
      title: 'Victor attacked you',
      body: 'Victor attacked you, you have won the battle and gained 50 points',
    },
    {
      id: 2,
      title: 'New robots',
      body: 'A new robots is available for you to use, check it out',
    },
    {
      id: 3,
      title: 'Bulbasaur attacked you',
      body: 'Bulbasaur attacked you, you have lost battle',
    },
    {
      id: 1,
      title: 'Person attacked you',
      body: 'Person attacked you, you have won the battle and gained 50 points',
    },
    {
      id: 2,
      title: 'New robots',
      body: 'A new robots is available for you to use, check it out',
    },
    {
      id: 3,
      title: 'Jack attacked you',
      body: 'Jack attacked you, you have lost battle',
    },
  ];

  constructor() { }

  ngOnInit() {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if(this.notificationSelected < 0) {
      this.notificationSelected = 0;
      this.setNotification.emit(0);
    } else if(this.notificationSelected > this.notifications.length - 1) {
      this.setNotification.emit(this.notifications.length - 1);
      this.notificationSelected = this.notifications.length - 1;
    }
  }

  select(index) {
    this.notificationSelected = index;
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapePress() {
    this.close.emit(null);
  }
}
