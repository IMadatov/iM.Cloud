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
  AddGroupMemberRequest,
  AdminGroupsClient,
  AdminUsersClient,
  GroupAccessLevel,
  GroupListDto,
  GroupMemberDto,
  PrimeTableMetaData,
  UpdateGroupMemberAccessRequest,
  UserListDto,
} from '@im-cloud/api';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { finalize, forkJoin } from 'rxjs';

interface AccessOption {
  label: string;
  value: GroupAccessLevel;
}

@Component({
  selector: 'app-group-members-dialog',
  imports: [Dialog, Checkbox, Select, FormsModule],
  templateUrl: './group-members-dialog.component.html',
  styleUrl: './group-members-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupMembersDialogComponent {
  private readonly groupsClient = inject(AdminGroupsClient);
  private readonly usersClient = inject(AdminUsersClient);

  readonly visible = model(false);
  readonly group = input<GroupListDto | null>(null);

  protected readonly loading = signal(false);
  protected readonly catalog = signal<UserListDto[]>([]);
  protected readonly members = signal<GroupMemberDto[]>([]);
  protected readonly busyIds = signal<Set<string>>(new Set());

  protected readonly accessOptions: AccessOption[] = [
    { label: 'Read', value: GroupAccessLevel.Read },
    { label: 'Write', value: GroupAccessLevel.Write },
    { label: 'Admin', value: GroupAccessLevel.Admin },
  ];

  protected readonly memberAccessByUserId = computed(() => {
    const map = new Map<string, GroupAccessLevel>();
    for (const member of this.members()) {
      if (member.userId && member.accessLevel !== undefined) {
        map.set(member.userId, member.accessLevel);
      }
    }
    return map;
  });

  constructor() {
    effect(() => {
      if (!this.visible() || !this.group()?.id) {
        return;
      }

      this.loadData();
    });
  }

  protected get dialogHeader(): string {
    const name = this.group()?.name;
    return name ? `Members — ${name}` : 'Members';
  }

  protected isMember(user: UserListDto): boolean {
    return !!user.id && this.memberAccessByUserId().has(user.id);
  }

  protected isBusy(user: UserListDto): boolean {
    return !!user.id && this.busyIds().has(user.id);
  }

  protected memberAccessLevel(user: UserListDto): GroupAccessLevel {
    return user.id
      ? (this.memberAccessByUserId().get(user.id) ?? GroupAccessLevel.Write)
      : GroupAccessLevel.Write;
  }

  protected onToggle(user: UserListDto, checked: boolean): void {
    const groupId = this.group()?.id;
    const userId = user.id;

    if (!groupId || !userId) {
      return;
    }

    this.setBusy(userId, true);

    const request$ = checked
      ? this.groupsClient.addMember(
          groupId,
          new AddGroupMemberRequest({
            userId,
            accessLevel: GroupAccessLevel.Write,
          }),
        )
      : this.groupsClient.removeMember(groupId, userId);

    request$
      .pipe(finalize(() => this.setBusy(userId, false)))
      .subscribe({
        next: () => this.reloadMembers(groupId),
        error: () => this.loadData(),
      });
  }

  protected onAccessChange(user: UserListDto, level: GroupAccessLevel): void {
    const groupId = this.group()?.id;
    const userId = user.id;

    if (!groupId || !userId || !this.isMember(user)) {
      return;
    }

    this.setBusy(userId, true);
    this.groupsClient
      .updateMemberAccess(
        groupId,
        userId,
        new UpdateGroupMemberAccessRequest({ accessLevel: level }),
      )
      .pipe(finalize(() => this.setBusy(userId, false)))
      .subscribe({
        next: () => this.reloadMembers(groupId),
        error: () => this.loadData(),
      });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private loadData(): void {
    const groupId = this.group()?.id;
    if (!groupId) {
      return;
    }

    this.loading.set(true);
    const usersMeta = new PrimeTableMetaData({ first: 0, rows: 1000 });

    forkJoin({
      catalog: this.usersClient.getAll(usersMeta),
      members: this.groupsClient.listMembers(groupId),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ catalog, members }) => {
          this.catalog.set(catalog.items ?? []);
          this.members.set(members ?? []);
        },
      });
  }

  private reloadMembers(groupId: string): void {
    this.groupsClient.listMembers(groupId).subscribe({
      next: (members) => this.members.set(members ?? []),
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
