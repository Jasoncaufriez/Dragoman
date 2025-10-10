import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NavbarInterComponent } from './navbarinter.component';

describe('NavbarinterComponent', () => {
  let component: NavbarInterComponent;
  let fixture: ComponentFixture<NavbarInterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NavbarInterComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(NavbarInterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
