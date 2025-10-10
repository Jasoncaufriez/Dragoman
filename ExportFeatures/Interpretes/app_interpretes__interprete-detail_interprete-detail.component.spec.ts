import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterpreteDetailComponent } from './interprete-detail.component';

describe('InterpreteDetailComponent', () => {
  let component: InterpreteDetailComponent;
  let fixture: ComponentFixture<InterpreteDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [InterpreteDetailComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(InterpreteDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
