import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterpreteListComponent } from './interprete-list.component';

describe('InterpreteListComponent', () => {
  let component: InterpreteListComponent;
  let fixture: ComponentFixture<InterpreteListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [InterpreteListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(InterpreteListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
