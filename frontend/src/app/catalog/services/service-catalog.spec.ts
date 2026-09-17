import { TestBed } from '@angular/core/testing';
import { ServiceCatalog } from './service-catalog';

describe('ServiceCatalog', () => {
  let service: ServiceCatalog;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ServiceCatalog);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
