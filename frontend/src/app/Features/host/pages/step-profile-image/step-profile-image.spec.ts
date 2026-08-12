import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { StepProfileImageComponent } from './step-profile-image';

describe('StepProfileImageComponent', () => {
  let component: StepProfileImageComponent;
  let fixture: ComponentFixture<StepProfileImageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StepProfileImageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(StepProfileImageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
