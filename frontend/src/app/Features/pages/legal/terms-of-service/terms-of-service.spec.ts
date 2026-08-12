import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TermsOfServiceComponent } from './terms-of-service';

describe('TermsOfServiceComponent', () => {
  let component: TermsOfServiceComponent;
  let fixture: ComponentFixture<TermsOfServiceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TermsOfServiceComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TermsOfServiceComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});