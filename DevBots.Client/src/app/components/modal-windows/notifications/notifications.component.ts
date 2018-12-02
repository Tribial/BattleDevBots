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
      title: 'Muhamat attacked you',
      body: 'Muhamat attacked you, you have lost battle',
    },
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
      title: 'Muhamat attacked you',
      body: 'Muhamat attacked you, you have lost battle',
    },
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
      title: 'Muhamat attacked you',
      body: 'Muhamat attacked you, you have lost battle',
    },
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
      title: 'Muhamat attacked you',
      body: 'Muhamat attacked you, you have lost battle',
    },
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
      title: 'Muhamat attacked you',
      body: 'Muhamat attacked you, you have lost battle',
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

  @HostListener('document:keydown.escape', ['$event'])
  onEscapePress() {
    this.close.emit(null);
  }
}
