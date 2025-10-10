import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HDPrestationJourComponent } from './hd-prestation-jour.component';

describe('HDPrestationJourComponent', () => {
  let component: HDPrestationJourComponent;
  let fixture: ComponentFixture<HDPrestationJourComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HDPrestationJourComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(HDPrestationJourComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
