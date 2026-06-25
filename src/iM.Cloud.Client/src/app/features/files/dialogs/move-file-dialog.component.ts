import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CreateFolderRequest, FileItemDto, MoveFileRequest } from '@im-cloud/api';
import { Breadcrumb } from 'primeng/breadcrumb';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs';

export type ListFilesFn = (parentId: string | null | undefined) => Observable<FileItemDto[]>;
export type MoveFileFn = (id: string, request: MoveFileRequest) => Observable<FileItemDto>;
export type CreateFolderFn = (request: CreateFolderRequest) => Observable<FileItemDto>;

interface MoveBreadcrumbItem {
  id: string | null;
  label: string;
}

export interface FileMovedEvent {
  item: FileItemDto;
  newParentId: string | null;
  destinationPathIds: string[];
  destinationFolderNames: Record<string, string>;
}

@Component({
  selector: 'app-move-file-dialog',
  imports: [FormsModule, Dialog, Button, Breadcrumb, InputText],
  templateUrl: './move-file-dialog.component.html',
  styleUrl: './move-file-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoveFileDialogComponent {
  readonly visible = model(false);
  readonly item = input<FileItemDto | null>(null);
  readonly listFn = input.required<ListFilesFn>();
  readonly moveFn = input.required<MoveFileFn>();
  readonly createFolderFn = input<CreateFolderFn | null>(null);
  readonly excludedFolderIds = input<ReadonlySet<string>>(new Set());
  readonly initialPathIds = input<string[]>([]);
  readonly initialFolderNames = input<Record<string, string>>({});
  readonly rootLabel = input('Files');

  readonly moved = output<FileMovedEvent>();

  readonly dialogParentId = signal<string | null>(null);
  readonly dialogPathIds = signal<string[]>([]);
  readonly folderNames = signal<Record<string, string>>({});
  readonly folders = signal<FileItemDto[]>([]);
  readonly loading = signal(false);
  readonly moving = signal(false);
  readonly showNewFolderInput = signal(false);
  readonly newFolderName = signal('');
  readonly creatingFolder = signal(false);

  readonly breadcrumbs = computed<MoveBreadcrumbItem[]>(() => {
    const names = this.folderNames();
    const crumbs: MoveBreadcrumbItem[] = [{ id: null, label: this.rootLabel() }];

    for (const id of this.dialogPathIds()) {
      crumbs.push({ id, label: names[id] ?? 'Folder' });
    }

    return crumbs;
  });

  readonly destinationLabel = computed(() => {
    const crumbs = this.breadcrumbs();
    return crumbs[crumbs.length - 1]?.label ?? this.rootLabel();
  });

  readonly breadcrumbModel = computed(() =>
    this.breadcrumbs().map((crumb) => ({
      label: crumb.label,
      command: () => this.navigateToCrumb(crumb.id),
    })),
  );

  constructor() {
    let wasOpen = false;
    effect(() => {
      const open = this.visible();
      if (open && !wasOpen) {
        this.resetDialog();
      }
      wasOpen = open;
    });
  }

  canMoveHere(): boolean {
    return this.canMoveToParent(this.dialogParentId());
  }

  canMoveIntoFolder(folder: FileItemDto): boolean {
    return !!folder.id && this.canMoveToParent(folder.id);
  }

  openFolder(folder: FileItemDto): void {
    if (!folder.id) return;

    this.folderNames.update((map) => ({ ...map, [folder.id!]: folder.name ?? 'Folder' }));
    this.dialogPathIds.update((ids) => [...ids, folder.id!]);
    this.dialogParentId.set(folder.id);
    this.loadFolders();
  }

  moveHere(): void {
    this.executeMove(this.dialogParentId(), [...this.dialogPathIds()]);
  }

  moveIntoFolder(folder: FileItemDto, event: Event): void {
    event.stopPropagation();
    if (!folder.id) return;

    const destinationPathIds = [...this.dialogPathIds(), folder.id];
    this.executeMove(folder.id, destinationPathIds, folder.name ?? 'Folder');
  }

  toggleNewFolderInput(): void {
    this.showNewFolderInput.update((visible) => !visible);
    if (!this.showNewFolderInput()) {
      this.newFolderName.set('');
    }
  }

  createFolder(): void {
    const createFn = this.createFolderFn();
    if (!createFn || this.creatingFolder() || this.moving()) return;

    const name = this.newFolderName().trim();
    if (!name) return;

    const request = new CreateFolderRequest({
      name,
      parentId: this.dialogParentId() ?? undefined,
    });

    this.creatingFolder.set(true);
    createFn(request)
      .pipe(finalize(() => this.creatingFolder.set(false)))
      .subscribe({
        next: (folder) => {
          if (folder.id && folder.name) {
            this.folderNames.update((map) => ({ ...map, [folder.id!]: folder.name! }));
          }
          this.newFolderName.set('');
          this.showNewFolderInput.set(false);
          this.loadFolders();
        },
      });
  }

  private executeMove(
    newParentId: string | null,
    destinationPathIds: string[],
    destinationFolderName?: string,
  ): void {
    const item = this.item();
    if (!item?.id || this.moving() || !this.canMoveToParent(newParentId)) return;

    const request = new MoveFileRequest();
    request.init({ parentId: newParentId });

    this.moving.set(true);
    this.moveFn()(item.id, request)
      .pipe(finalize(() => this.moving.set(false)))
      .subscribe({
        next: (dto) => {
          const folderNames = { ...this.folderNames() };
          if (newParentId && destinationFolderName) {
            folderNames[newParentId] = destinationFolderName;
          }

          this.moved.emit({
            item: dto,
            newParentId,
            destinationPathIds,
            destinationFolderNames: folderNames,
          });
          this.visible.set(false);
        },
      });
  }

  private canMoveToParent(destinationId: string | null): boolean {
    const item = this.item();
    if (!item?.id) return false;

    const currentParentId = item.parentId ?? null;

    if (destinationId === currentParentId) return false;
    if (destinationId && this.excludedFolderIds().has(destinationId)) return false;

    return true;
  }

  private resetDialog(): void {
    const pathIds = [...this.initialPathIds()];
    this.dialogPathIds.set(pathIds);
    this.dialogParentId.set(pathIds.length ? pathIds[pathIds.length - 1] : null);
    this.folderNames.set({ ...this.initialFolderNames() });
    this.showNewFolderInput.set(false);
    this.newFolderName.set('');
    this.loadFolders();
  }

  private navigateToCrumb(crumbId: string | null): void {
    if (crumbId === null) {
      this.dialogPathIds.set([]);
      this.dialogParentId.set(null);
      this.loadFolders();
      return;
    }

    const pathIds = this.dialogPathIds();
    const index = pathIds.indexOf(crumbId);
    if (index < 0) return;

    const ids = pathIds.slice(0, index + 1);
    this.dialogPathIds.set(ids);
    this.dialogParentId.set(crumbId);
    this.loadFolders();
  }

  private loadFolders(): void {
    const parentId = this.dialogParentId();
    this.loading.set(true);
    this.listFn()(parentId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => {
          const excluded = this.excludedFolderIds();
          this.folders.set(
            items.filter((entry) => entry.isFolder && entry.id && !excluded.has(entry.id)),
          );
        },
        error: () => this.folders.set([]),
      });
  }
}
