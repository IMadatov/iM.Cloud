import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminPermissionsClient,
  AdminRolesClient,
  AssignPermissionRequest,
  PermissionListDto,
  PrimeTableMetaData,
  RoleListDto,
} from '@im-cloud/api';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { finalize, forkJoin } from 'rxjs';

@Component({
  selector: 'app-role-permissions-dialog',
  imports: [Dialog, Checkbox, FormsModule],
  templateUrl: './role-permissions-dialog.component.html',
  styleUrl: './role-permissions-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolePermissionsDialogComponent {
  private readonly rolesClient = inject(AdminRolesClient);
  private readonly permissionsClient = inject(AdminPermissionsClient);

  readonly visible = model(false);
  readonly role = input<RoleListDto | null>(null);

  protected readonly loading = signal(false);
  protected readonly catalog = signal<PermissionListDto[]>([]);
  protected readonly assigned = signal<PermissionListDto[]>([]);
  protected readonly busyIds = signal<Set<string>>(new Set());

  protected readonly assignedIds = computed(
    () =>
      new Set(
        this.assigned()
          .map((permission) => permission.id)
          .filter((id): id is string => !!id),
      ),
  );

  constructor() {
    effect(() => {
      if (!this.visible() || !this.role()?.id) {
        return;
      }

      this.loadData();
    });
  }

  protected get dialogHeader(): string {
    const name = this.role()?.name;
    return name ? `Permissions — ${name}` : 'Permissions';
  }

  protected isAssigned(permission: PermissionListDto): boolean {
    return !!permission.id && this.assignedIds().has(permission.id);
  }

  protected isBusy(permission: PermissionListDto): boolean {
    return !!permission.id && this.busyIds().has(permission.id);
  }

  protected onToggle(permission: PermissionListDto, checked: boolean): void {
    const roleId = this.role()?.id;
    const permissionId = permission.id;
    const permissionCode = permission.code;

    if (!roleId || !permissionId || !permissionCode) {
      return;
    }

    this.setBusy(permissionId, true);

    const request$ = checked
      ? this.rolesClient.assignPermission(
          roleId,
          new AssignPermissionRequest({ permissionCode }),
        )
      : this.rolesClient.removePermission(roleId, permissionId);

    request$
      .pipe(finalize(() => this.setBusy(permissionId, false)))
      .subscribe({
        next: () => this.reloadAssigned(roleId),
        error: () => this.loadData(),
      });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private loadData(): void {
    const roleId = this.role()?.id;
    if (!roleId) {
      return;
    }

    this.loading.set(true);
    const catalogMeta = new PrimeTableMetaData({ first: 0, rows: 1000 });

    forkJoin({
      catalog: this.permissionsClient.getAll(catalogMeta),
      assigned: this.rolesClient.getPermissions(roleId),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ catalog, assigned }) => {
          this.catalog.set(catalog.items ?? []);
          this.assigned.set(assigned ?? []);
        },
      });
  }

  private reloadAssigned(roleId: string): void {
    this.rolesClient.getPermissions(roleId).subscribe({
      next: (assigned) => this.assigned.set(assigned ?? []),
      error: () => this.loadData(),
    });
  }

  private setBusy(id: string, busy: boolean): void {
    this.busyIds.update((current) => {
      const next = new Set(current);
      if (busy) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }
}
