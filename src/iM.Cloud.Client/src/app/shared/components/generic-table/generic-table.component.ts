import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  DestroyRef,
  inject,
  input,
  model,
  output,
  signal,
  TemplateRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { InputText } from 'primeng/inputtext';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { debounceTime, finalize, Subject, switchMap } from 'rxjs';
import { CellTemplateDirective } from './cell-template.directive';
import {
  DataSourceFn,
  GenericTableConfig,
  TableColumn,
} from './generic-table.types';
import { toPrimeTableMetaData } from './prime-table.mapper';

@Component({
  selector: 'im-generic-table',
  standalone: true,
  imports: [TableModule, InputText, NgTemplateOutlet],
  templateUrl: './generic-table.component.html',
  styleUrl: './generic-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GenericTableComponent<T extends object> {
  readonly columns = input.required<TableColumn<T>[]>();
  readonly dataSource = input.required<DataSourceFn<T>>();
  readonly config = input<GenericTableConfig>({});

  readonly selection = model<T | T[] | null>(null);
  readonly rowSelect = output<T>();

  protected readonly data = signal<T[]>([]);
  protected readonly totalRecords = signal(0);
  protected readonly loading = signal(false);

  protected readonly cellTemplates = contentChildren(CellTemplateDirective);

  private readonly destroyRef = inject(DestroyRef);
  private readonly loadRequest$ = new Subject<TableLazyLoadEvent>();
  private readonly globalFilterRequest$ = new Subject<string>();

  private lastEvent?: TableLazyLoadEvent;
  private globalFilterValue = '';

  constructor() {
    this.loadRequest$
      .pipe(
        switchMap((event) => {
          this.lastEvent = event;
          this.loading.set(true);
          const meta = toPrimeTableMetaData(event, this.globalFilterValue);
          return this.dataSource()(meta).pipe(
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.data.set(result.items ?? []);
          this.totalRecords.set(result.totalItems ?? 0);
        },
        error: () => {
          // Errors are surfaced by the HTTP error notification interceptor.
        },
      });

    this.globalFilterRequest$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.globalFilterValue = value;
        if (this.lastEvent) {
          this.onLazyLoad({ ...this.lastEvent, first: 0 });
        }
      });
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    this.loadRequest$.next(event);
  }

  protected applyGlobalFilter(value: string): void {
    this.globalFilterRequest$.next(value);
  }

  reload(): void {
    if (this.lastEvent) {
      this.onLazyLoad(this.lastEvent);
    }
  }

  protected templateFor(field: string): TemplateRef<unknown> | null {
    const directive = this.cellTemplates().find(
      (template) => template.imCellTemplate() === field,
    );
    return directive?.templateRef ?? null;
  }

  protected cellValue(row: T, field: Extract<keyof T, string>): unknown {
    return row[field];
  }
}
