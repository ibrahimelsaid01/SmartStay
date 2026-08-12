import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { StepInformationComponent } from './step-information';

describe('StepInformationComponent', () => {
  let component: StepInformationComponent;
  let fixture: ComponentFixture<StepInformationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StepInformationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(StepInformationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when empty', () => {
    expect(component.form.invalid).toBeTrue();
  });
});
