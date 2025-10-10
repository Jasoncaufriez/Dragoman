import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IndispoComponent } from './indispo.component';

describe('IndispoComponent', () => {
  let component: IndispoComponent;
  let fixture: ComponentFixture<IndispoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [IndispoComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(IndispoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
