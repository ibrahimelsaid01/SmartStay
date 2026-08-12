import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApplicationApprovedComponent } from './application-approved';

describe('ApplicationApprovedComponent', () => {
  let component: ApplicationApprovedComponent;
  let fixture: ComponentFixture<ApplicationApprovedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationApprovedComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationApprovedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
