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
import { AdminRolesClient, RoleDetailsDto } from '@im-cloud/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-role-form-dialog',
  imports: [
    ReactiveFormsModule,
    Dialog,
    InputText,
    Textarea,
    ToggleSwitch,
    Button,
  ],
  templateUrl: './role-form-dialog.component.html',
  styleUrl: './role-form-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleFormDialogComponent {
  private readonly rolesClient = inject(AdminRolesClient);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly role = input<RoleDetailsDto | null>(null);
  readonly saved = output<void>();

  protected readonly loading = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    active: [true],
  });

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      const role = this.role();
      if (role) {
        this.form.reset({
          name: role.name ?? '',
          description: role.description ?? '',
          active: role.active ?? true,
        });
      } else {
        this.form.reset({
          name: '',
          description: '',
          active: true,
        });
      }
    });
  }

  protected get isEdit(): boolean {
    return !!this.role()?.id;
  }

  protected get dialogHeader(): string {
    return this.isEdit ? 'Edit role' : 'New role';
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description, active } = this.form.getRawValue();
    const dto = new RoleDetailsDto({
      name,
      description: description.trim() || undefined,
      active,
    });

    const existing = this.role();
    let request$;

    if (this.isEdit && existing?.id) {
      dto.id = existing.id;
      request$ = this.rolesClient.update(existing.id, dto);
    } else {
      request$ = this.rolesClient.create(dto);
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
