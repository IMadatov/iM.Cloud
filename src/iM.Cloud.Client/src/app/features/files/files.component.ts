import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import {
  CreateFolderRequest,
  FileItemDto,
  FileParameter,
  FilesClient,
} from '@im-cloud/api';
import { MessageService } from 'primeng/api';
import { ConfirmationService } from 'primeng/api';
import { Breadcrumb } from 'primeng/breadcrumb';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { filter, finalize, startWith } from 'rxjs';
import { environment } from '../../../environments/environment';

interface BreadcrumbItem {
  id: string | null;
  label: string;
}

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
    Tag,
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

  private readonly folderNames = signal<Record<string, string>>({});

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

  formatSize(size: number | undefined): string {
    if (size == null) return '—';
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }
}
