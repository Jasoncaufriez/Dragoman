import { TestBed } from '@angular/core/testing';

import { AdStatusService } from './ad-status.service';

describe('AdStatusService', () => {
  let service: AdStatusService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdStatusService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
