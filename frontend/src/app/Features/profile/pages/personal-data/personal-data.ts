import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Country, CountryService } from '../../services/country-service';
import {
  UserProfile,
  UserProfileService,
} from '../../services/user-profile-service';

@Component({
  selector: 'app-personal-data',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './personal-data.html',
  styleUrl: './personal-data.css',
})
export class PersonalData implements OnInit {
  @ViewChild('phoneBtn') phoneBtn!: ElementRef;

  profileForm!: FormGroup;

  avatarPreview = 'Images/default-avatar.png';
  selectedFile: File | null = null;
  successMessage: string | null = null;
  errorMessage: string | null = null;
  isLoading = true;

  countries: Country[] = [];
  filteredCountries: Country[] = [];
  filteredPhoneCountries: Country[] = [];

  isDropdownOpen = signal(false);
  isPhoneDropdownOpen = signal(false);
  selectedPhoneCode = signal('+20');

  private readonly destroyRef = inject(DestroyRef);

  private currentProfile: UserProfile | null = null;
  private countriesLoaded = false;
  private profileLoaded = false;

  constructor(
    private readonly fb: FormBuilder,
    private readonly userProfileService: UserProfileService,
    private readonly countryService: CountryService,
    private readonly elementRef: ElementRef,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.bindCurrentUser();
    this.loadCountries();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const targetElement = event.target as HTMLElement;

    if (this.phoneBtn?.nativeElement.contains(targetElement)) {
      return;
    }

    if (!this.elementRef.nativeElement.contains(targetElement)) {
      this.isDropdownOpen.set(false);
      this.isPhoneDropdownOpen.set(false);
    }
  }

  initForm(): void {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: [{ value: '', disabled: true }],
      phoneNumber: ['', [Validators.pattern('^[0-9]*$')]],
      gender: [''],
      birthday: [''],
      country: [''],
      address: [''],
      zipCode: [''],
    });
  }

  bindCurrentUser(): void {
    this.userProfileService.initializeSession();

    this.userProfileService.currentUser$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          if (!profile) {
            if (!this.userProfileService.isAuthenticated()) {
              this.currentProfile = null;
              this.isLoading = false;
              this.cdr.detectChanges();
            }

            return;
          }

          this.currentProfile = profile;
          this.profileLoaded = true;

          this.tryHydrateForm();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  loadCountries(): void {
    this.countryService
      .getCountries()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.countries = list;
          this.filteredCountries = list;
          this.filteredPhoneCountries = list;
          this.countriesLoaded = true;

          this.tryHydrateForm();
        },
        error: (error) => {
          console.error('Error loading countries:', error);

          this.countriesLoaded = true;
          this.tryHydrateForm();
        },
      });
  }

  private tryHydrateForm(): void {
    if (!this.currentProfile || !this.profileLoaded || !this.countriesLoaded) {
      return;
    }

    this.applyProfileToForm(this.currentProfile);
    this.isLoading = false;
    this.cdr.detectChanges();
  }

  private applyProfileToForm(profile: UserProfile): void {
    const fullPhoneNumber = profile.phoneNumber || '';
    const extractedCode = this.getExtractedCode(fullPhoneNumber);
    const cleanPhoneNumber = this.getCleanNumber(fullPhoneNumber);

    this.selectedPhoneCode.set(extractedCode);

    this.avatarPreview =
      profile.profileImageUrl ||
      `https://ui-avatars.com/api/?name=${encodeURIComponent(
        `${profile.firstName || 'User'} ${profile.lastName || ''}`.trim()
      )}`;

    this.profileForm.patchValue(
      {
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phoneNumber: cleanPhoneNumber,
        gender: profile.gender || '',
        birthday: profile.birthday ? profile.birthday.split('T')[0] : '',
        country: profile.country || '',
        address: profile.address || '',
        zipCode: profile.zipCode || '',
      },
      { emitEvent: false }
    );

    this.profileForm.markAsPristine();
    this.profileForm.markAsUntouched();
  }

  toggleDropdown(event: MouseEvent): void {
    event.stopPropagation();

    this.isDropdownOpen.update((value) => !value);
    this.isPhoneDropdownOpen.set(false);
  }

  togglePhoneDropdown(): void {
    this.isPhoneDropdownOpen.update((value) => !value);
    this.isDropdownOpen.set(false);
  }

  closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  closePhoneDropdown(): void {
    this.isPhoneDropdownOpen.set(false);
  }

  selectCountry(countryName: string): void {
    this.profileForm.get('country')?.setValue(countryName);
    this.profileForm.get('country')?.markAsDirty();

    this.isDropdownOpen.set(false);
  }

  selectPhoneCode(code: string): void {
    this.selectedPhoneCode.set(code);
    this.profileForm.get('phoneNumber')?.markAsDirty();

    this.isPhoneDropdownOpen.set(false);
  }

  onSearchCountry(event: Event): void {
    const term = (event.target as HTMLInputElement).value.toLowerCase().trim();

    this.filteredCountries = term
      ? this.countries.filter((country) =>
          country.name.toLowerCase().includes(term)
        )
      : this.countries;
  }

  onSearchPhoneCode(event: Event): void {
    const term = (event.target as HTMLInputElement).value.toLowerCase().trim();

    this.filteredPhoneCountries = term
      ? this.countries.filter(
          (country) =>
            country.name.toLowerCase().includes(term) ||
            country.phoneCode.includes(term)
        )
      : this.countries;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    this.selectedFile = file;

    const reader = new FileReader();

    reader.onload = () => {
      this.avatarPreview = reader.result as string;
      this.profileForm.markAsDirty();
      this.cdr.detectChanges();
    };

    reader.readAsDataURL(file);
  }

  onSubmit(): void {
    if (this.profileForm.invalid || !this.currentProfile) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.successMessage = null;
    this.errorMessage = null;

    const rawFormData = this.profileForm.getRawValue();

    const rawPhone = rawFormData.phoneNumber
      ? String(rawFormData.phoneNumber).trim()
      : '';

    const fullPhoneNumber = rawPhone
      ? `${this.selectedPhoneCode()}${rawPhone}`
      : '';

    const formattedBirthday = this.formatDateForApi(rawFormData.birthday);

    const updatePayload: UserProfile = {
      firstName: rawFormData.firstName || '',
      lastName: rawFormData.lastName || '',
      email: rawFormData.email || '',
      phoneNumber: fullPhoneNumber,
      gender: rawFormData.gender || '',
      birthday: formattedBirthday,
      country: rawFormData.country || '',
      address: rawFormData.address || '',
      zipCode: rawFormData.zipCode || '',
      profileImageUrl: this.currentProfile.profileImageUrl || null,
      roles: this.currentProfile.roles || [],
      isProfileCompleted: this.currentProfile.isProfileCompleted,
    };

    this.userProfileService
      .updateProfile(updatePayload, this.selectedFile || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updatedProfile) => {
          this.currentProfile = updatedProfile;
          this.selectedFile = null;
          this.successMessage = 'Profile updated successfully!';
          this.isLoading = false;

          this.applyProfileToForm(updatedProfile);
          this.cdr.detectChanges();

          window.setTimeout(() => {
            this.successMessage = null;
            this.cdr.detectChanges();
          }, 4000);
        },
        error: (error) => {
          console.error('Update failed', error);

          this.errorMessage = 'Failed to update profile. Please try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  onDiscard(): void {
    this.selectedFile = null;

    if (this.currentProfile) {
      this.applyProfileToForm(this.currentProfile);
    }
  }

  getExtractedCode(fullNumber: string): string {
    if (!fullNumber || !fullNumber.startsWith('+')) {
      return '+20';
    }

    const sortedCountries = [...this.countries].sort(
      (left, right) => right.phoneCode.length - left.phoneCode.length
    );

    const matchedCountry = sortedCountries.find((country) =>
      fullNumber.startsWith(country.phoneCode)
    );

    return matchedCountry ? matchedCountry.phoneCode : '+20';
  }

  getCleanNumber(fullNumber: string): string {
    if (!fullNumber) {
      return '';
    }

    if (!fullNumber.startsWith('+')) {
      return fullNumber;
    }

    const code = this.getExtractedCode(fullNumber);

    return fullNumber.startsWith(code)
      ? fullNumber.slice(code.length)
      : fullNumber;
  }

  private formatDateForApi(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const dateObj = new Date(value);

    if (Number.isNaN(dateObj.getTime())) {
      return value;
    }

    return dateObj.toISOString().split('T')[0];
  }
}