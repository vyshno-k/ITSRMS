import { TestBed } from '@angular/core/testing';
import { ServiceRequest } from './service-request';

describe('ServiceRequest', () => {
  let service: ServiceRequest;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ServiceRequest);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
