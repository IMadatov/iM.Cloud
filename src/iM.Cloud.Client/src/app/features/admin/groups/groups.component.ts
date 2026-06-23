import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  AdminGroupsClient,
  GroupDetailsDto,
  GroupListDto,
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
import { GroupFormDialogComponent } from './group-form-dialog.component';
import { GroupMembersDialogComponent } from './group-members-dialog.component';

type GroupTableRow = GroupListDto & { actions?: null };

@Component({
  selector: 'app-groups',
  imports: [
    GenericTableComponent,
    CellTemplateDirective,
    GroupFormDialogComponent,
    GroupMembersDialogComponent,
    Button,
    Tag,
    DatePipe,
  ],
  templateUrl: './groups.component.html',
  styleUrl: './groups.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupsComponent {
  private readonly groupsClient = inject(AdminGroupsClient);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  private readonly table = viewChild(GenericTableComponent);

  protected readonly dialogVisible = signal(false);
  protected readonly membersDialogVisible = signal(false);
  protected readonly editingGroup = signal<GroupDetailsDto | null>(null);
  protected readonly membersGroup = signal<GroupListDto | null>(null);
  protected readonly editLoading = signal(false);

  readonly columns: TableColumn<GroupTableRow>[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'description', header: 'Description', sortable: true, filter: true },
    { field: 'active', header: 'Status', sortable: true },
    { field: 'createdAt', header: 'Created', sortable: true },
    { field: 'actions', header: 'Actions', width: '11rem' },
  ];

  readonly tableConfig: GenericTableConfig = {
    rows: 10,
    globalFilter: true,
  };

  readonly loadGroups: DataSourceFn<GroupListDto> = (meta) =>
    this.groupsClient.getAll(meta);

  protected openCreate(): void {
    this.editingGroup.set(null);
    this.dialogVisible.set(true);
  }

  protected openEdit(row: GroupListDto): void {
    if (!row.id) {
      return;
    }

    this.editLoading.set(true);
    this.groupsClient
      .getById(row.id)
      .pipe(finalize(() => this.editLoading.set(false)))
      .subscribe({
        next: (group) => {
          this.editingGroup.set(group);
          this.dialogVisible.set(true);
        },
      });
  }

  protected openMembers(row: GroupListDto): void {
    this.membersGroup.set(row);
    this.membersDialogVisible.set(true);
  }

  protected confirmDelete(row: GroupListDto): void {
    if (!row.id) {
      return;
    }

    this.confirmation.confirm({
      header: 'Deactivate group',
      message: `Deactivate ${row.name ?? 'this group'}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Deactivate', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.groupsClient.deactivate(row.id!).subscribe({
          next: () => {
            this.messages.add({
              severity: 'success',
              summary: 'Group deactivated',
              detail: row.name ?? '',
            });
            this.table()?.reload();
          },
        });
      },
    });
  }

  protected onSaved(): void {
    const isEdit = !!this.editingGroup()?.id;
    this.dialogVisible.set(false);
    this.messages.add({
      severity: 'success',
      summary: isEdit ? 'Group updated' : 'Group created',
    });
    this.table()?.reload();
  }
}
