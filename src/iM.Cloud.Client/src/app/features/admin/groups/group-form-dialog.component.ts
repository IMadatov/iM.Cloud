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
import { AdminGroupsClient, GroupDetailsDto } from '@im-cloud/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-group-form-dialog',
  imports: [
    ReactiveFormsModule,
    Dialog,
    InputText,
    Textarea,
    ToggleSwitch,
    Button,
  ],
  templateUrl: './group-form-dialog.component.html',
  styleUrl: './group-form-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupFormDialogComponent {
  private readonly groupsClient = inject(AdminGroupsClient);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly group = input<GroupDetailsDto | null>(null);
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

      const group = this.group();
      if (group) {
        this.form.reset({
          name: group.name ?? '',
          description: group.description ?? '',
          active: group.active ?? true,
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
    return !!this.group()?.id;
  }

  protected get dialogHeader(): string {
    return this.isEdit ? 'Edit group' : 'New group';
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description, active } = this.form.getRawValue();
    const dto = new GroupDetailsDto({
      name,
      description: description.trim() || undefined,
      active,
    });

    const existing = this.group();
    let request$;

    if (this.isEdit && existing?.id) {
      dto.id = existing.id;
      request$ = this.groupsClient.update(existing.id, dto);
    } else {
      request$ = this.groupsClient.create(dto);
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
