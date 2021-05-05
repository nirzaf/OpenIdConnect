import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoggedoutComponent } from './loggedout.component';

describe('LoggedoutComponent', () => {
  let component: LoggedoutComponent;
  let fixture: ComponentFixture<LoggedoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoggedoutComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoggedoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
