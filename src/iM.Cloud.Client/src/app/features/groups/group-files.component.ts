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
  GroupAccessLevel,
  GroupFilesClient,
  GroupsClient,
  MoveFileRequest,
  RenameFileRequest,
} from '@im-cloud/api';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
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
} from '../files/dialogs/move-file-dialog.component';
import { RenameFileDialogComponent } from '../files/dialogs/rename-file-dialog.component';

interface BreadcrumbItem {
  id: string | null;
  label: string;
  groupRoot?: boolean;
}

type ShareExpiryPreset = 'never' | '1h' | '1d' | '7d' | '30d';

@Component({
  selector: 'app-group-files',
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
  templateUrl: './group-files.component.html',
  styleUrl: './group-files.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupFilesComponent implements OnInit {
  private readonly groupFilesClient = inject(GroupFilesClient);
  private readonly groupsClient = inject(GroupsClient);
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly items = signal<FileItemDto[]>([]);
  readonly loading = signal(false);
  readonly groupId = signal<string | null>(null);
  readonly groupName = signal('Group');
  readonly accessLevel = signal<GroupAccessLevel | undefined>(undefined);
  readonly canWrite = computed(
    () =>
      this.accessLevel() !== undefined &&
      this.accessLevel()! >= GroupAccessLevel.Write,
  );
  readonly folderPathIds = signal<string[]>([]);
  readonly currentParentId = signal<string | null>(null);
  readonly breadcrumbs = signal<BreadcrumbItem[]>([
    { id: null, label: 'My groups' },
    { id: null, label: 'Group', groupRoot: true },
  ]);

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

    const menuItems: MenuItem[] = [];

    if (this.canWrite()) {
      menuItems.push(
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
      );
    }

    if (this.canWrite() && !item.isFolder) {
      menuItems.push({
        label: 'Share',
        icon: 'pi pi-share-alt',
        command: () => this.openShareDialog(item),
      });
    }

    if (item.canDelete) {
      menuItems.push({
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.confirmDelete(item),
      });
    }

    return menuItems;
  });

  canShowRowMenu(item: FileItemDto): boolean {
    return !!item.canDelete || this.canWrite();
  }

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
      )
      .subscribe(() => this.syncFromRoute());
  }

  private syncFromRoute(): void {
    const groupId = this.route.snapshot.paramMap.get('groupId');
    if (!groupId) return;

    this.groupId.set(groupId);

    const pathParam = this.route.snapshot.paramMap.get('path');
    const ids = pathParam ? pathParam.split('/').filter(Boolean) : [];

    this.folderPathIds.set(ids);
    this.currentParentId.set(ids.length ? ids[ids.length - 1] : null);
    this.breadcrumbs.set(this.buildBreadcrumbs(groupId, ids));
    this.loadGroupName(groupId);
    this.loadItems();
  }

  private loadGroupName(groupId: string): void {
    this.groupsClient.mine().subscribe({
      next: (groups) => {
        const group = groups?.find((g) => g.id === groupId);
        this.groupName.set(group?.name ?? 'Group');
        this.accessLevel.set(group?.accessLevel);
        this.breadcrumbs.set(this.buildBreadcrumbs(groupId, this.folderPathIds()));
      },
    });
  }

  private buildBreadcrumbs(groupId: string, folderIds: string[]): BreadcrumbItem[] {
    const names = this.folderNames();
    const crumbs: BreadcrumbItem[] = [
      { id: null, label: 'My groups' },
      { id: groupId, label: this.groupName(), groupRoot: true },
    ];

    for (const id of folderIds) {
      crumbs.push({ id, label: names[id] ?? 'Folder' });
    }

    return crumbs;
  }

  private rememberFolderName(id: string, name: string): void {
    this.folderNames.update((map) => ({ ...map, [id]: name }));
  }

  private navigateToPath(folderIds: string[]): void {
    const groupId = this.groupId();
    if (!groupId) return;

    void this.router.navigate(
      folderIds.length ? ['/groups', groupId, ...folderIds] : ['/groups', groupId],
    );
  }

  loadItems(): void {
    const groupId = this.groupId();
    if (!groupId) return;

    this.loading.set(true);
    this.groupFilesClient
      .list(groupId, this.currentParentId())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => this.items.set(items ?? []),
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
    if (index === 0) {
      void this.router.navigate(['/groups']);
      return;
    }

    if (crumb.groupRoot) {
      this.navigateToPath([]);
      return;
    }

    const folderIndex = index - 2;
    const ids = folderIndex < 0 ? [] : this.folderPathIds().slice(0, folderIndex + 1);
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
    const groupId = this.groupId();
    const name = this.newFolderName().trim();
    if (!groupId || !name) return;

    const request = new CreateFolderRequest({
      name,
      parentId: this.currentParentId() ?? undefined,
    });

    this.groupFilesClient.createFolder(groupId, request).subscribe({
      next: () => {
        this.folderDialogVisible.set(false);
        this.messages.add({ severity: 'success', summary: 'Folder created' });
        this.loadItems();
      },
    });
  }

  onFileSelected(event: Event): void {
    const groupId = this.groupId();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!groupId || !file) return;

    const fileParam: FileParameter = { data: file, fileName: file.name };
    this.loading.set(true);
    this.groupFilesClient
      .upload(groupId, this.currentParentId(), fileParam)
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
    const groupId = this.groupId();
    if (!groupId || !item.id || item.isFolder) return;

    const url = `${environment.apiUrl}/api/groups/${groupId}/files/${item.id}/download`;
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
    const groupId = this.groupId();
    if (!groupId || !item.id) return;

    const url = `${environment.apiUrl}/api/groups/${groupId}/files/${item.id}/download`;
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
    const groupId = this.groupId();
    if (!groupId || !item.id) return;

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
        this.groupFilesClient.delete(groupId, item.id!).subscribe({
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
    if (!this.canWrite()) return;

    this.renameItem.set(item);
    this.renameDialogVisible.set(true);
  }

  async openMoveDialog(item: FileItemDto): Promise<void> {
    if (!this.canWrite()) return;

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

    const groupId = this.groupId();
    if (groupId) {
      this.navigateToPath(event.destinationPathIds);
    }

    this.messages.add({ severity: 'success', summary: 'Moved' });
  }

  private applyDestinationFolderNames(names: Record<string, string>): void {
    if (!Object.keys(names).length) return;

    this.folderNames.update((current) => ({ ...current, ...names }));
    const groupId = this.groupId();
    if (groupId) {
      this.breadcrumbs.set(this.buildBreadcrumbs(groupId, this.folderPathIds()));
    }
  }

  renameFile = (id: string, request: RenameFileRequest) => {
    const groupId = this.groupId();
    if (!groupId) throw new Error('Group is required');
    return this.groupFilesClient.rename(groupId, id, request);
  };

  moveFile = (id: string, request: MoveFileRequest) => {
    const groupId = this.groupId();
    if (!groupId) throw new Error('Group is required');
    return this.groupFilesClient.move(groupId, id, request);
  };

  createFolderForMove = (request: CreateFolderRequest) => {
    const groupId = this.groupId();
    if (!groupId) throw new Error('Group is required');
    return this.groupFilesClient.createFolder(groupId, request);
  };

  listGroupFiles = (parentId: string | null | undefined) => {
    const groupId = this.groupId();
    if (!groupId) throw new Error('Group is required');
    return this.groupFilesClient.list(groupId, parentId);
  };

  private async collectDescendantFolderIds(folderId: string): Promise<string[]> {
    const groupId = this.groupId();
    if (!groupId) return [];

    const result: string[] = [];
    const queue = [folderId];

    while (queue.length > 0) {
      const parentId = queue.shift()!;
      const items = await firstValueFrom(this.groupFilesClient.list(groupId, parentId));

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

  openRowMenu(event: Event, item: FileItemDto): void {
    event.stopPropagation();
    this.rowMenuItem.set(item);
    this.rowMenu()?.toggle(event);
  }

  openShareDialog(item: FileItemDto): void {
    if (!item.id || item.isFolder || !this.canWrite()) return;

    this.shareItem.set(item);
    this.shareExpiryPreset.set('never');
    this.shareUrl.set(null);
    this.shareExpiresAt.set(null);
    this.shareDialogVisible.set(true);
  }

  createShare(): void {
    const groupId = this.groupId();
    const item = this.shareItem();
    if (!groupId || !item?.id || this.creatingShare()) return;

    const request = new CreateShareRequest({
      expiresAt: this.computeExpiresAt(this.shareExpiryPreset()),
    });

    this.creatingShare.set(true);
    this.groupFilesClient
      .share(groupId, item.id, request)
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
