import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ServiceRequest } from './service-request';

describe('ServiceRequest', () => {
  let component: ServiceRequest;
  let fixture: ComponentFixture<ServiceRequest>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceRequest],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceRequest);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
