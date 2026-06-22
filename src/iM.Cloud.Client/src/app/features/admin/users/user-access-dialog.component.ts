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
  AdminUsersClient,
  AssignPermissionRequest,
  AssignRoleRequest,
  PermissionListDto,
  PrimeTableMetaData,
  RoleListDto,
  UserListDto,
} from '@im-cloud/api';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { Fieldset } from 'primeng/fieldset';
import { finalize, forkJoin } from 'rxjs';

@Component({
  selector: 'app-user-access-dialog',
  imports: [Dialog, Fieldset, Checkbox, FormsModule],
  templateUrl: './user-access-dialog.component.html',
  styleUrl: './user-access-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserAccessDialogComponent {
  private readonly usersClient = inject(AdminUsersClient);
  private readonly rolesClient = inject(AdminRolesClient);
  private readonly permissionsClient = inject(AdminPermissionsClient);

  readonly visible = model(false);
  readonly user = input<UserListDto | null>(null);

  protected readonly loading = signal(false);
  protected readonly roleCatalog = signal<RoleListDto[]>([]);
  protected readonly assignedRoles = signal<RoleListDto[]>([]);
  protected readonly permissionCatalog = signal<PermissionListDto[]>([]);
  protected readonly assignedPermissions = signal<PermissionListDto[]>([]);
  protected readonly busyRoleIds = signal<Set<string>>(new Set());
  protected readonly busyPermissionIds = signal<Set<string>>(new Set());

  protected readonly assignedRoleIds = computed(
    () =>
      new Set(
        this.assignedRoles()
          .map((role) => role.id)
          .filter((id): id is string => !!id),
      ),
  );

  protected readonly assignedPermissionIds = computed(
    () =>
      new Set(
        this.assignedPermissions()
          .map((permission) => permission.id)
          .filter((id): id is string => !!id),
      ),
  );

  constructor() {
    effect(() => {
      if (!this.visible() || !this.user()?.id) {
        return;
      }

      this.loadData();
    });
  }

  protected get dialogHeader(): string {
    const email = this.user()?.email;
    return email ? `Access — ${email}` : 'Access';
  }

  protected isRoleAssigned(role: RoleListDto): boolean {
    return !!role.id && this.assignedRoleIds().has(role.id);
  }

  protected isPermissionAssigned(permission: PermissionListDto): boolean {
    return !!permission.id && this.assignedPermissionIds().has(permission.id);
  }

  protected isRoleBusy(role: RoleListDto): boolean {
    return !!role.id && this.busyRoleIds().has(role.id);
  }

  protected isPermissionBusy(permission: PermissionListDto): boolean {
    return !!permission.id && this.busyPermissionIds().has(permission.id);
  }

  protected onRoleToggle(role: RoleListDto, checked: boolean): void {
    const userId = this.user()?.id;
    const roleId = role.id;
    const roleName = role.name;

    if (!userId || !roleId || !roleName) {
      return;
    }

    this.setRoleBusy(roleId, true);

    const request$ = checked
      ? this.usersClient.assignRole(
          userId,
          new AssignRoleRequest({ roleName }),
        )
      : this.usersClient.removeRole(userId, roleId);

    request$
      .pipe(finalize(() => this.setRoleBusy(roleId, false)))
      .subscribe({
        next: () => this.reloadRoles(userId),
        error: () => this.loadData(),
      });
  }

  protected onPermissionToggle(
    permission: PermissionListDto,
    checked: boolean,
  ): void {
    const userId = this.user()?.id;
    const permissionId = permission.id;
    const permissionCode = permission.code;

    if (!userId || !permissionId || !permissionCode) {
      return;
    }

    this.setPermissionBusy(permissionId, true);

    const request$ = checked
      ? this.usersClient.grantPermission(
          userId,
          new AssignPermissionRequest({ permissionCode }),
        )
      : this.usersClient.revokePermission(userId, permissionId);

    request$
      .pipe(finalize(() => this.setPermissionBusy(permissionId, false)))
      .subscribe({
        next: () => this.reloadPermissions(userId),
        error: () => this.loadData(),
      });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private loadData(): void {
    const userId = this.user()?.id;
    if (!userId) {
      return;
    }

    this.loading.set(true);
    const catalogMeta = new PrimeTableMetaData({ first: 0, rows: 1000 });

    forkJoin({
      roleCatalog: this.rolesClient.getAll(catalogMeta),
      assignedRoles: this.usersClient.getRoles(userId),
      permissionCatalog: this.permissionsClient.getAll(catalogMeta),
      assignedPermissions: this.usersClient.getPermissions(userId),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({
          roleCatalog,
          assignedRoles,
          permissionCatalog,
          assignedPermissions,
        }) => {
          this.roleCatalog.set(roleCatalog.items ?? []);
          this.assignedRoles.set(assignedRoles ?? []);
          this.permissionCatalog.set(permissionCatalog.items ?? []);
          this.assignedPermissions.set(assignedPermissions ?? []);
        },
      });
  }

  private reloadRoles(userId: string): void {
    this.usersClient.getRoles(userId).subscribe({
      next: (roles) => this.assignedRoles.set(roles ?? []),
      error: () => this.loadData(),
    });
  }

  private reloadPermissions(userId: string): void {
    this.usersClient.getPermissions(userId).subscribe({
      next: (permissions) => this.assignedPermissions.set(permissions ?? []),
      error: () => this.loadData(),
    });
  }

  private setRoleBusy(id: string, busy: boolean): void {
    this.busyRoleIds.update((current) => {
      const next = new Set(current);
      if (busy) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  private setPermissionBusy(id: string, busy: boolean): void {
    this.busyPermissionIds.update((current) => {
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
