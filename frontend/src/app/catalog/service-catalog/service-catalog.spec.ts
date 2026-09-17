import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ServiceCatalog } from './service-catalog';

describe('ServiceCatalog', () => {
  let component: ServiceCatalog;
  let fixture: ComponentFixture<ServiceCatalog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceCatalog],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceCatalog);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
