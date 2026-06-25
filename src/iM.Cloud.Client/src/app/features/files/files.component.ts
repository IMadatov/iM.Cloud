import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import {
  CreateFolderRequest,
  CreateShareRequest,
  FileItemDto,
  FileParameter,
  FilesClient,
  MoveFileRequest,
  RenameFileRequest,
} from '@im-cloud/api';
import { MessageService } from 'primeng/api';
import { ConfirmationService, MenuItem } from 'primeng/api';
import { Breadcrumb } from 'primeng/breadcrumb';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { filter, finalize, firstValueFrom, startWith } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  FileMovedEvent,
  MoveFileDialogComponent,
} from './dialogs/move-file-dialog.component';
import { RenameFileDialogComponent } from './dialogs/rename-file-dialog.component';

interface BreadcrumbItem {
  id: string | null;
  label: string;
}

type ShareExpiryPreset = 'never' | '1h' | '1d' | '7d' | '30d';

@Component({
  selector: 'app-files',
  imports: [
    FormsModule,
    Card,
    TableModule,
    Button,
    Breadcrumb,
    Dialog,
    InputText,
    Menu,
    Select,
    Tag,
    RenameFileDialogComponent,
    MoveFileDialogComponent,
  ],
  templateUrl: './files.component.html',
  styleUrl: './files.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilesComponent implements OnInit {
  private readonly filesClient = inject(FilesClient);
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly items = signal<FileItemDto[]>([]);
  readonly loading = signal(false);
  readonly currentParentId = signal<string | null>(null);
  readonly folderPathIds = signal<string[]>([]);
  readonly breadcrumbs = signal<BreadcrumbItem[]>([{ id: null, label: 'nav.files' }]);

  readonly folderDialogVisible = signal(false);
  readonly newFolderName = signal('');

  readonly shareDialogVisible = signal(false);
  readonly shareItem = signal<FileItemDto | null>(null);
  readonly shareExpiryPreset = signal<ShareExpiryPreset>('never');
  readonly shareUrl = signal<string | null>(null);
  readonly shareExpiresAt = signal<Date | null>(null);
  readonly creatingShare = signal(false);

  readonly renameDialogVisible = signal(false);
  readonly renameItem = signal<FileItemDto | null>(null);

  readonly moveDialogVisible = signal(false);
  readonly moveItem = signal<FileItemDto | null>(null);
  readonly excludedMoveFolderIds = signal<ReadonlySet<string>>(new Set());
  readonly moveInitialPathIds = signal<string[]>([]);
  readonly moveInitialFolderNames = signal<Record<string, string>>({});

  protected readonly shareExpiryOptions: { label: string; value: ShareExpiryPreset }[] = [
    { label: 'Never', value: 'never' },
    { label: '1 hour', value: '1h' },
    { label: '1 day', value: '1d' },
    { label: '7 days', value: '7d' },
    { label: '30 days', value: '30d' },
  ];

  private readonly folderNames = signal<Record<string, string>>({});

  private readonly rowMenu = viewChild<Menu>('rowMenu');
  private readonly rowMenuItem = signal<FileItemDto | null>(null);

  readonly rowMenuItems = computed<MenuItem[]>(() => {
    const item = this.rowMenuItem();
    if (!item) return [];

    const menuItems: MenuItem[] = [
      {
        label: 'Rename',
        icon: 'pi pi-pencil',
        command: () => this.openRenameDialog(item),
      },
      {
        label: 'Move',
        icon: 'pi pi-arrow-right-arrow-left',
        command: () => void this.openMoveDialog(item),
      },
    ];

    if (!item.isFolder) {
      menuItems.push({
        label: 'Share',
        icon: 'pi pi-share-alt',
        command: () => this.openShareDialog(item),
      });
    }

    menuItems.push({
      label: 'Delete',
      icon: 'pi pi-trash',
      command: () => this.confirmDelete(item),
    });

    return menuItems;
  });

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
      )
      .subscribe(() => this.syncFromRoute());
  }

  private syncFromRoute(): void {
    const pathParam = this.route.snapshot.paramMap.get('path');
    const ids = pathParam ? pathParam.split('/').filter(Boolean) : [];

    this.folderPathIds.set(ids);
    this.currentParentId.set(ids.length ? ids[ids.length - 1] : null);
    this.breadcrumbs.set(this.buildBreadcrumbs(ids));
    this.loadItems();
  }

  private buildBreadcrumbs(ids: string[]): BreadcrumbItem[] {
    const names = this.folderNames();
    const crumbs: BreadcrumbItem[] = [{ id: null, label: 'nav.files' }];

    for (const id of ids) {
      crumbs.push({ id, label: names[id] ?? 'Folder' });
    }

    return crumbs;
  }

  private rememberFolderName(id: string, name: string): void {
    this.folderNames.update((map) => ({ ...map, [id]: name }));
  }

  private navigateToPath(ids: string[]): void {
    void this.router.navigate(ids.length ? ['/files', ...ids] : ['/files']);
  }

  loadItems(): void {
    this.loading.set(true);
    this.filesClient
      .list(this.currentParentId())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => this.items.set(items),
        error: () => this.items.set([]),
      });
  }

  openFolder(item: FileItemDto): void {
    if (!item.isFolder || !item.id) return;

    this.rememberFolderName(item.id, item.name ?? 'Folder');
    this.navigateToPath([...this.folderPathIds(), item.id]);
  }

  openFile(item: FileItemDto): void {
    if (item.isFolder || !item.id) return;

    if (this.canPreview(item)) {
      this.openInBrowser(item);
    } else {
      this.download(item);
    }
  }

  canPreview(item: FileItemDto): boolean {
    const contentType = item.contentType?.toLowerCase();
    if (contentType) {
      if (
        contentType.startsWith('image/') ||
        contentType.startsWith('text/') ||
        contentType.startsWith('video/') ||
        contentType.startsWith('audio/') ||
        contentType === 'application/pdf' ||
        contentType === 'application/json'
      ) {
        return true;
      }
    }

    const extension = item.name?.split('.').pop()?.toLowerCase();
    if (!extension) return false;

    const previewableExtensions = new Set([
      'png',
      'jpg',
      'jpeg',
      'gif',
      'webp',
      'svg',
      'pdf',
      'txt',
      'json',
      'md',
      'log',
      'csv',
      'mp4',
      'webm',
      'mp3',
      'wav',
      'ogg',
    ]);

    return previewableExtensions.has(extension);
  }

  navigateTo(crumb: BreadcrumbItem, index: number): void {
    const ids = index === 0 ? [] : this.folderPathIds().slice(0, index);
    this.navigateToPath(ids);
  }

  breadcrumbModel() {
    return this.breadcrumbs().map((crumb, index) => ({
      label: crumb.label,
      command: () => this.navigateTo(crumb, index),
    }));
  }

  showCreateFolderDialog(): void {
    this.newFolderName.set('');
    this.folderDialogVisible.set(true);
  }

  createFolder(): void {
    const name = this.newFolderName().trim();
    if (!name) return;

    const request = new CreateFolderRequest({
      name,
      parentId: this.currentParentId() ?? undefined,
    });
    this.filesClient.createFolder(request).subscribe({
      next: () => {
        this.folderDialogVisible.set(false);
        this.messages.add({ severity: 'success', summary: 'Folder created' });
        this.loadItems();
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const fileParam: FileParameter = { data: file, fileName: file.name };
    this.loading.set(true);
    this.filesClient
      .upload(this.currentParentId(), fileParam)
      .pipe(
        finalize(() => {
          this.loading.set(false);
          input.value = '';
        }),
      )
      .subscribe({
        next: () => {
          this.messages.add({ severity: 'success', summary: 'File uploaded' });
          this.loadItems();
        },
      });
  }

  download(item: FileItemDto): void {
    if (!item.id || item.isFolder) return;

    const url = `${environment.apiUrl}/api/files/${item.id}/download`;
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = objectUrl;
        anchor.download = item.name ?? 'download';
        anchor.click();
        URL.revokeObjectURL(objectUrl);
      },
    });
  }

  private openInBrowser(item: FileItemDto): void {
    if (!item.id) return;

    const url = `${environment.apiUrl}/api/files/${item.id}/download`;
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: () => this.messages.add({ severity: 'error', summary: 'Open failed' }),
    });
  }

  confirmDelete(item: FileItemDto): void {
    if (!item.id) return;

    const name = item.name ?? 'this item';
    const message = item.isFolder
      ? `Delete folder "${name}" and all its contents?`
      : `Delete file "${name}"?`;

    this.confirmation.confirm({
      header: 'Delete',
      message,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.filesClient.delete(item.id!).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: 'Deleted' });
            this.handleDeletedItemInRoute(item);
            this.loadItems();
          },
        });
      },
    });
  }

  private handleDeletedItemInRoute(item: FileItemDto): void {
    if (!item.id) return;

    const pathIds = this.folderPathIds();
    const deletedIndex = pathIds.indexOf(item.id);

    if (deletedIndex >= 0) {
      const parentIds = deletedIndex === 0 ? [] : pathIds.slice(0, deletedIndex);
      this.navigateToPath(parentIds);
    }
  }

  private handleMovedItemInRoute(item: FileItemDto): void {
    if (!item.id) return;

    const pathIds = this.folderPathIds();
    const movedIndex = pathIds.indexOf(item.id);

    if (movedIndex >= 0) {
      const parentIds = movedIndex === 0 ? [] : pathIds.slice(0, movedIndex);
      this.navigateToPath(parentIds);
    }
  }

  openRenameDialog(item: FileItemDto): void {
    this.renameItem.set(item);
    this.renameDialogVisible.set(true);
  }

  async openMoveDialog(item: FileItemDto): Promise<void> {
    const excluded = new Set<string>();
    if (item.isFolder && item.id) {
      excluded.add(item.id);
      const descendants = await this.collectDescendantFolderIds(item.id);
      descendants.forEach((id) => excluded.add(id));
    }

    this.moveItem.set(item);
    this.excludedMoveFolderIds.set(excluded);
    this.moveInitialPathIds.set(this.buildInitialMovePathIds(item));
    this.moveInitialFolderNames.set(this.buildInitialMoveFolderNames(this.moveInitialPathIds()));
    this.moveDialogVisible.set(true);
  }

  private buildInitialMovePathIds(item: FileItemDto): string[] {
    const parentId = item.parentId ?? null;
    if (!parentId) return [];

    const pathIds = this.folderPathIds();
    const currentParentId = this.currentParentId();

    if (parentId === currentParentId) {
      return [...pathIds];
    }

    const parentIndex = pathIds.indexOf(parentId);
    if (parentIndex >= 0) {
      return pathIds.slice(0, parentIndex + 1);
    }

    return [parentId];
  }

  private buildInitialMoveFolderNames(pathIds: string[]): Record<string, string> {
    const names = this.folderNames();
    const result: Record<string, string> = {};

    for (const id of pathIds) {
      if (names[id]) {
        result[id] = names[id];
      }
    }

    return result;
  }

  onRenamed(item: FileItemDto): void {
    if (item.isFolder && item.id && item.name) {
      this.rememberFolderName(item.id, item.name);
    }

    this.messages.add({ severity: 'success', summary: 'Renamed' });
    this.loadItems();
  }

  onMoved(event: FileMovedEvent): void {
    const movedItem = event.item;
    if (movedItem.id) {
      this.handleMovedItemInRoute(movedItem);
    }

    this.applyDestinationFolderNames(event.destinationFolderNames);
    if (movedItem.isFolder && movedItem.id && movedItem.name) {
      this.rememberFolderName(movedItem.id, movedItem.name);
    }

    this.navigateToPath(event.destinationPathIds);
    this.messages.add({ severity: 'success', summary: 'Moved' });
  }

  private applyDestinationFolderNames(names: Record<string, string>): void {
    if (!Object.keys(names).length) return;

    this.folderNames.update((current) => ({ ...current, ...names }));
    this.breadcrumbs.set(this.buildBreadcrumbs(this.folderPathIds()));
  }

  renameFile = (id: string, request: RenameFileRequest) => this.filesClient.rename(id, request);

  moveFile = (id: string, request: MoveFileRequest) => this.filesClient.move(id, request);

  createFolderForMove = (request: CreateFolderRequest) => this.filesClient.createFolder(request);

  listFiles = (parentId: string | null | undefined) => this.filesClient.list(parentId);

  private async collectDescendantFolderIds(folderId: string): Promise<string[]> {
    const result: string[] = [];
    const queue = [folderId];

    while (queue.length > 0) {
      const parentId = queue.shift()!;
      const items = await firstValueFrom(this.filesClient.list(parentId));

      for (const entry of items) {
        if (entry.isFolder && entry.id) {
          result.push(entry.id);
          queue.push(entry.id);
        }
      }
    }

    return result;
  }

  formatSize(size: number | undefined): string {
    if (size == null) return '—';
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  openShareDialog(item: FileItemDto): void {
    if (!item.id || item.isFolder) return;

    this.shareItem.set(item);
    this.shareExpiryPreset.set('never');
    this.shareUrl.set(null);
    this.shareExpiresAt.set(null);
    this.shareDialogVisible.set(true);
  }

  openRowMenu(event: Event, item: FileItemDto): void {
    event.stopPropagation();
    this.rowMenuItem.set(item);
    this.rowMenu()?.toggle(event);
  }

  createShare(): void {
    const item = this.shareItem();
    if (!item?.id || this.creatingShare()) return;

    const request = new CreateShareRequest({
      expiresAt: this.computeExpiresAt(this.shareExpiryPreset()),
    });

    this.creatingShare.set(true);
    this.filesClient
      .share(item.id, request)
      .pipe(finalize(() => this.creatingShare.set(false)))
      .subscribe({
        next: (link) => {
          const token = link.token;
          if (!token) return;

          this.shareUrl.set(`${window.location.origin}/share/${token}`);
          this.shareExpiresAt.set(link.expiresAt ?? null);
          this.messages.add({ severity: 'success', summary: 'Share link created' });
        },
      });
  }

  async copyShareUrl(): Promise<void> {
    const url = this.shareUrl();
    if (!url) return;

    await navigator.clipboard.writeText(url);
    this.messages.add({ severity: 'success', summary: 'Link copied' });
  }

  shareExpiryLabel(): string {
    const expiresAt = this.shareExpiresAt();
    if (!expiresAt) return 'No expiration';

    return `Valid until ${expiresAt.toLocaleString()}`;
  }

  private computeExpiresAt(preset: ShareExpiryPreset): Date | undefined {
    const now = Date.now();
    switch (preset) {
      case '1h':
        return new Date(now + 60 * 60 * 1000);
      case '1d':
        return new Date(now + 24 * 60 * 60 * 1000);
      case '7d':
        return new Date(now + 7 * 24 * 60 * 60 * 1000);
      case '30d':
        return new Date(now + 30 * 24 * 60 * 60 * 1000);
      default:
        return undefined;
    }
  }
}
