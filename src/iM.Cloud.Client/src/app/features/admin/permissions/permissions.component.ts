import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AdminPermissionsClient, PermissionListDto } from '@im-cloud/api';
import { Tag } from 'primeng/tag';
import { CellTemplateDirective } from '../../../shared/components/generic-table/cell-template.directive';
import { GenericTableComponent } from '../../../shared/components/generic-table/generic-table.component';
import {
  DataSourceFn,
  GenericTableConfig,
  TableColumn,
} from '../../../shared/components/generic-table/generic-table.types';

@Component({
  selector: 'app-permissions',
  imports: [GenericTableComponent, CellTemplateDirective, Tag],
  templateUrl: './permissions.component.html',
  styleUrl: './permissions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermissionsComponent {
  private readonly permissionsClient = inject(AdminPermissionsClient);

  readonly columns: TableColumn<PermissionListDto>[] = [
    { field: 'code', header: 'Code', sortable: true, filter: true },
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'description', header: 'Description', sortable: true, filter: true },
    { field: 'active', header: 'Status', sortable: true },
  ];

  readonly tableConfig: GenericTableConfig = {
    rows: 10,
    globalFilter: true,
  };

  readonly loadPermissions: DataSourceFn<PermissionListDto> = (meta) =>
    this.permissionsClient.getAll(meta);
}
