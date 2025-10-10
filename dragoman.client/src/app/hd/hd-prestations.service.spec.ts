import { TestBed } from '@angular/core/testing';
import { HDPrestationsService } from './hd-prestations.service';

describe('HDPrestationsService', () => {
  let service: HDPrestationsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HDPrestationsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
