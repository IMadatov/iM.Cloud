import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { GroupListDto, GroupsClient } from '@im-cloud/api';
import { Card } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-my-groups',
  imports: [Card, TableModule],
  templateUrl: './my-groups.component.html',
  styleUrl: './my-groups.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyGroupsComponent implements OnInit {
  private readonly groupsClient = inject(GroupsClient);
  private readonly router = inject(Router);

  readonly groups = signal<GroupListDto[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.loadGroups();
  }

  openGroup(group: GroupListDto): void {
    if (!group.id) return;
    void this.router.navigate(['/groups', group.id]);
  }

  private loadGroups(): void {
    this.loading.set(true);
    this.groupsClient
      .mine()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (groups) => this.groups.set(groups ?? []),
        error: () => this.groups.set([]),
      });
  }
}
