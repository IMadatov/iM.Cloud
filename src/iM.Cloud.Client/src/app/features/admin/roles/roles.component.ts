import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  AdminRolesClient,
  RoleDetailsDto,
  RoleListDto,
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
import { RoleFormDialogComponent } from './role-form-dialog.component';
import { RolePermissionsDialogComponent } from './role-permissions-dialog.component';

type RoleTableRow = RoleListDto & { actions?: null };

@Component({
  selector: 'app-roles',
  imports: [
    GenericTableComponent,
    CellTemplateDirective,
    RoleFormDialogComponent,
    RolePermissionsDialogComponent,
    Button,
    Tag,
  ],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolesComponent {
  private readonly rolesClient = inject(AdminRolesClient);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  private readonly table = viewChild(GenericTableComponent);

  protected readonly dialogVisible = signal(false);
  protected readonly permDialogVisible = signal(false);
  protected readonly editingRole = signal<RoleDetailsDto | null>(null);
  protected readonly permissionsRole = signal<RoleListDto | null>(null);
  protected readonly editLoading = signal(false);

  readonly columns: TableColumn<RoleTableRow>[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'description', header: 'Description', sortable: true, filter: true },
    { field: 'active', header: 'Status', sortable: true },
    { field: 'actions', header: 'Actions', width: '11rem' },
  ];

  readonly tableConfig: GenericTableConfig = {
    rows: 10,
    globalFilter: true,
  };

  readonly loadRoles: DataSourceFn<RoleListDto> = (meta) =>
    this.rolesClient.getAll(meta);

  protected openCreate(): void {
    this.editingRole.set(null);
    this.dialogVisible.set(true);
  }

  protected openEdit(row: RoleListDto): void {
    if (!row.id) {
      return;
    }

    this.editLoading.set(true);
    this.rolesClient
      .getById(row.id)
      .pipe(finalize(() => this.editLoading.set(false)))
      .subscribe({
        next: (role) => {
          this.editingRole.set(role);
          this.dialogVisible.set(true);
        },
      });
  }

  protected openPermissions(row: RoleListDto): void {
    this.permissionsRole.set(row);
    this.permDialogVisible.set(true);
  }

  protected confirmDelete(row: RoleListDto): void {
    if (!row.id) {
      return;
    }

    this.confirmation.confirm({
      header: 'Deactivate role',
      message: `Deactivate ${row.name ?? 'this role'}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Deactivate', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.rolesClient.deactivate(row.id!).subscribe({
          next: () => {
            this.messages.add({
              severity: 'success',
              summary: 'Role deactivated',
              detail: row.name ?? '',
            });
            this.table()?.reload();
          },
        });
      },
    });
  }

  protected onSaved(): void {
    const isEdit = !!this.editingRole()?.id;
    this.dialogVisible.set(false);
    this.messages.add({
      severity: 'success',
      summary: isEdit ? 'Role updated' : 'Role created',
    });
    this.table()?.reload();
  }
}
