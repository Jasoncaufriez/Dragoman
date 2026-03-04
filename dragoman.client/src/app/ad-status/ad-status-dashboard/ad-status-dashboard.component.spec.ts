import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdStatusDashboardComponent } from './ad-status-dashboard.component';

describe('AdStatusDashboardComponent', () => {
  let component: AdStatusDashboardComponent;
  let fixture: ComponentFixture<AdStatusDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdStatusDashboardComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AdStatusDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
