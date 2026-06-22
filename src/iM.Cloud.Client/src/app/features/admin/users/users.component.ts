import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  AdminUsersClient,
  UserDetailsDto,
  UserListDto,
} from '@im-cloud/api';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { finalize } from 'rxjs';
import { CellTemplateDirective } from '../../../shared/components/generic-table/cell-template.directive';
import { GenericTableComponent } from '../../../shared/components/generic-table/generic-table.component';
import {
  DataSourceFn,
  GenericTableConfig,
  TableColumn,
} from '../../../shared/components/generic-table/generic-table.types';
import { UserFormDialogComponent } from './user-form-dialog.component';

type UserTableRow = UserListDto & { actions?: null };

@Component({
  selector: 'app-users',
  imports: [
    GenericTableComponent,
    CellTemplateDirective,
    UserFormDialogComponent,
    Button,
    Tag,
    DatePipe,
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersComponent {
  private readonly usersClient = inject(AdminUsersClient);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  private readonly table = viewChild(GenericTableComponent);

  protected readonly dialogVisible = signal(false);
  protected readonly editingUser = signal<UserDetailsDto | null>(null);
  protected readonly editLoading = signal(false);

  readonly columns: TableColumn<UserTableRow>[] = [
    { field: 'email', header: 'Email', sortable: true, filter: true },
    { field: 'displayName', header: 'Name', sortable: true, filter: true },
    { field: 'active', header: 'Status', sortable: true },
    { field: 'createdAt', header: 'Created', sortable: true },
    { field: 'actions', header: 'Actions', width: '8rem' },
  ];

  readonly tableConfig: GenericTableConfig = {
    rows: 10,
    globalFilter: true,
  };

  readonly loadUsers: DataSourceFn<UserListDto> = (meta) =>
    this.usersClient.getAll(meta);

  protected openCreate(): void {
    this.editingUser.set(null);
    this.dialogVisible.set(true);
  }

  protected openEdit(row: UserListDto): void {
    if (!row.id) {
      return;
    }

    this.editLoading.set(true);
    this.usersClient
      .getById(row.id)
      .pipe(finalize(() => this.editLoading.set(false)))
      .subscribe({
        next: (user) => {
          this.editingUser.set(user);
          this.dialogVisible.set(true);
        },
      });
  }

  protected confirmDelete(row: UserListDto): void {
    if (!row.id) {
      return;
    }

    this.confirmation.confirm({
      header: 'Deactivate user',
      message: `Deactivate ${row.email ?? 'this user'}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Deactivate', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.usersClient.deactivate(row.id!).subscribe({
          next: () => {
            this.messages.add({
              severity: 'success',
              summary: 'User deactivated',
              detail: row.email ?? '',
            });
            this.table()?.reload();
          },
        });
      },
    });
  }

  protected onSaved(): void {
    const isEdit = !!this.editingUser()?.id;
    this.dialogVisible.set(false);
    this.messages.add({
      severity: 'success',
      summary: isEdit ? 'User updated' : 'User created',
    });
    this.table()?.reload();
  }
}
