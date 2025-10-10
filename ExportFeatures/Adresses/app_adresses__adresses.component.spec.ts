import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdressesComponent } from './adresses.component';

describe('TolkAdressesComponent', () => {
  let component: AdressesComponent;
  let fixture: ComponentFixture<AdressesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdressesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdressesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
