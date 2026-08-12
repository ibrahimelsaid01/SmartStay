import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRejectedComponent } from './application-rejected';

describe('ApplicationRejectedComponent', () => {
  let component: ApplicationRejectedComponent;
  let fixture: ComponentFixture<ApplicationRejectedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationRejectedComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationRejectedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
