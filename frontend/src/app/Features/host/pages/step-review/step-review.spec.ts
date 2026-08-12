import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { StepReviewComponent } from './step-review';

describe('StepReviewComponent', () => {
  let component: StepReviewComponent;
  let fixture: ComponentFixture<StepReviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StepReviewComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(StepReviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
