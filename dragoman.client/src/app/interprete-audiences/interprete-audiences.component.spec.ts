import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterpreteAudiencesComponent } from './interprete-audiences.component';

describe('InterpreteAudiencesComponent', () => {
  let component: InterpreteAudiencesComponent;
  let fixture: ComponentFixture<InterpreteAudiencesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [InterpreteAudiencesComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(InterpreteAudiencesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
