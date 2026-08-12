import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { StepIdDocumentComponent } from './step-id-document';

describe('StepIdDocumentComponent', () => {
  let component: StepIdDocumentComponent;
  let fixture: ComponentFixture<StepIdDocumentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StepIdDocumentComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(StepIdDocumentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
