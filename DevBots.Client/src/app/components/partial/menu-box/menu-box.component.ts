import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-menu-box',
  templateUrl: './menu-box.component.html',
  styleUrls: ['./menu-box.component.scss']
})
export class MenuBoxComponent implements OnInit {
  public width: number;
  public height: number;
  @Input() disabled: boolean;
  @Input() activated: boolean;

  constructor() { 
  }

  ngOnInit() {
    setTimeout(() => console.log(this.disabled), 5000);
  }

}
