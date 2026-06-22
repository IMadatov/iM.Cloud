import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AdminUsersClient, UserDetailsDto } from '@im-cloud/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-user-form-dialog',
  imports: [
    ReactiveFormsModule,
    Dialog,
    InputText,
    Password,
    ToggleSwitch,
    Button,
  ],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormDialogComponent {
  private readonly usersClient = inject(AdminUsersClient);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly user = input<UserDetailsDto | null>(null);
  readonly saved = output<void>();

  protected readonly loading = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', Validators.required],
    active: [true],
    password: [''],
  });

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      const user = this.user();
      if (user) {
        this.form.reset({
          email: user.email ?? '',
          displayName: user.displayName ?? '',
          active: user.active ?? true,
          password: '',
        });
      } else {
        this.form.reset({
          email: '',
          displayName: '',
          active: true,
          password: '',
        });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.user()?.id;
  }

  protected get dialogHeader(): string {
    return this.isEdit ? 'Edit user' : 'New user';
  }

  protected submit(): void {
    if (this.isEdit) {
      this.form.controls.password.clearValidators();
    } else {
      this.form.controls.password.setValidators([Validators.required]);
    }
    this.form.controls.password.updateValueAndValidity();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, displayName, active, password } = this.form.getRawValue();
    const dto = new UserDetailsDto({
      email,
      displayName,
      active,
    });

    const existing = this.user();
    let request$;

    if (this.isEdit && existing?.id) {
      dto.id = existing.id;
      dto.createdAt = existing.createdAt;
      request$ = this.usersClient.update(existing.id, dto);
    } else {
      dto.password = password;
      request$ = this.usersClient.create(dto);
    }

    this.loading.set(true);
    request$
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.saved.emit(),
      });
  }

  protected close(): void {
    this.visible.set(false);
  }
}
