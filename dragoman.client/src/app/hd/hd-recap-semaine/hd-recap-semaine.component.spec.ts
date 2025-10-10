import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HDRecapSemaineComponent } from './hd-recap-semaine.component';
describe('HDRecapSemaineComponent', () => {
  let component: HDRecapSemaineComponent;
  let fixture: ComponentFixture<HDRecapSemaineComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HDRecapSemaineComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(HDRecapSemaineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
