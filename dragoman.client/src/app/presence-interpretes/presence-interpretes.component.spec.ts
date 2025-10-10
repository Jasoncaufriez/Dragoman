import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PresenceInterpretesComponent } from './presence-interpretes.component';

describe('PresenceInterpretesComponent', () => {
  let component: PresenceInterpretesComponent;
  let fixture: ComponentFixture<PresenceInterpretesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PresenceInterpretesComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(PresenceInterpretesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
