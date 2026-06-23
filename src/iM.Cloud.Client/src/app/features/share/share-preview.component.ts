import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SharePreviewDto } from '@im-cloud/api';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { finalize } from 'rxjs';
import { SKIP_ERROR_TOAST_HEADER } from '../../core/http/error-notification.interceptor';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-share-preview',
  imports: [Card, Button],
  templateUrl: './share-preview.component.html',
  styleUrl: './share-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SharePreviewComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly preview = signal<SharePreviewDto | null>(null);

  readonly token = signal('');

  readonly contentUrl = computed(
    () => `${environment.apiUrl}/api/public/shares/${this.token()}/content`,
  );

  readonly downloadUrl = computed(
    () =>
      `${environment.apiUrl}/api/public/shares/${this.token()}/content?disposition=attachment`,
  );

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.loading.set(false);
      this.error.set(true);
      return;
    }

    this.token.set(token);
    this.loadPreview(token);
  }

  isImage(): boolean {
    const contentType = this.preview()?.contentType?.toLowerCase();
    return !!contentType?.startsWith('image/');
  }

  isPdf(): boolean {
    return this.preview()?.contentType?.toLowerCase() === 'application/pdf';
  }

  expiryLabel(): string | null {
    const expiresAt = this.preview()?.expiresAt;
    if (!expiresAt) return null;

    return `Valid until ${new Date(expiresAt).toLocaleString()}`;
  }

  private loadPreview(token: string): void {
    this.loading.set(true);
    this.error.set(false);

    this.http
      .get<SharePreviewDto>(`${environment.apiUrl}/api/public/shares/${token}`, {
        headers: { [SKIP_ERROR_TOAST_HEADER]: 'true' },
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (preview) => this.preview.set(preview),
        error: () => {
          this.preview.set(null);
          this.error.set(true);
        },
      });
  }
}
