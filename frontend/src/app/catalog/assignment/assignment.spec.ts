import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Assignment } from './assignment';

describe('Assignment', () => {
  let component: Assignment;
  let fixture: ComponentFixture<Assignment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Assignment],
    }).compileComponents();

    fixture = TestBed.createComponent(Assignment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
