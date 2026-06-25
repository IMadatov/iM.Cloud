import { ChangeDetectionStrategy, Component, effect, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FileItemDto, RenameFileRequest } from '@im-cloud/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs';

export type RenameFileFn = (id: string, request: RenameFileRequest) => Observable<FileItemDto>;

@Component({
  selector: 'app-rename-file-dialog',
  imports: [FormsModule, Dialog, Button, InputText],
  templateUrl: './rename-file-dialog.component.html',
  styleUrl: './rename-file-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenameFileDialogComponent {
  readonly visible = model(false);
  readonly item = input<FileItemDto | null>(null);
  readonly renameFn = input.required<RenameFileFn>();

  readonly renamed = output<FileItemDto>();

  readonly newName = signal('');
  readonly saving = signal(false);

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.newName.set(this.item()?.name ?? '');
      }
    });
  }

  save(): void {
    const item = this.item();
    if (!item?.id || this.saving()) return;

    const name = this.newName().trim();
    if (!name) return;

    this.saving.set(true);
    this.renameFn()(item.id, new RenameFileRequest({ name }))
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (dto) => {
          this.renamed.emit(dto);
          this.visible.set(false);
        },
      });
  }
}
