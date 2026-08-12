import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { HostApplicationComponent } from './host-application';

describe('HostApplicationComponent', () => {
  let component: HostApplicationComponent;
  let fixture: ComponentFixture<HostApplicationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostApplicationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(HostApplicationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
