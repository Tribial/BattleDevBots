import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectScriptComponent } from './select-script.component';

describe('SelectScriptComponent', () => {
  let component: SelectScriptComponent;
  let fixture: ComponentFixture<SelectScriptComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SelectScriptComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectScriptComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
