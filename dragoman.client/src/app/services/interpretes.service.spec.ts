import { TestBed } from '@angular/core/testing';

import { InterpretesService } from './interpretes.service';

describe('InterpretesService', () => {
  let service: InterpretesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(InterpretesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
