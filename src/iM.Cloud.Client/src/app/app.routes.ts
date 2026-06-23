import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { filesPathMatcher } from './features/files/files-route.matcher';
import { groupsPathMatcher } from './features/groups/groups-path.matcher';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    component: LoginComponent,
  },
  {
    path: 'share/:token',
    loadComponent: () =>
      import('./features/share/share-preview.component').then(
        (m) => m.SharePreviewComponent,
      ),
  },
  {
    path: '',
    canActivate: [authGuard],
    component: MainLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },
      {
        matcher: filesPathMatcher,
        loadComponent: () =>
          import('./features/files/files.component').then(
            (m) => m.FilesComponent,
          ),
      },
      {
        path: 'groups',
        loadComponent: () =>
          import('./features/groups/my-groups.component').then(
            (m) => m.MyGroupsComponent,
          ),
      },
      {
        matcher: groupsPathMatcher,
        loadComponent: () =>
          import('./features/groups/group-files.component').then(
            (m) => m.GroupFilesComponent,
          ),
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard],
        data: { permission: 'users.read' },
        loadComponent: () =>
          import('./features/admin/users/users.component').then(
            (m) => m.UsersComponent,
          ),
      },
      {
        path: 'admin/roles',
        canActivate: [permissionGuard],
        data: { permission: 'roles.manage' },
        loadComponent: () =>
          import('./features/admin/roles/roles.component').then(
            (m) => m.RolesComponent,
          ),
      },
      {
        path: 'admin/groups',
        canActivate: [permissionGuard],
        data: { permission: 'groups.read' },
        loadComponent: () =>
          import('./features/admin/groups/groups.component').then(
            (m) => m.GroupsComponent,
          ),
      },
      {
        path: 'admin/permissions',
        canActivate: [permissionGuard],
        data: { permission: 'roles.manage' },
        loadComponent: () =>
          import('./features/admin/permissions/permissions.component').then(
            (m) => m.PermissionsComponent,
          ),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
