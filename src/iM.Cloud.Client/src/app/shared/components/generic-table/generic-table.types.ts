import { PrimeTableMetaData } from '@im-cloud/api';
import { Observable } from 'rxjs';

export interface TableColumn<T> {
  field: Extract<keyof T, string>;
  header: string;
  sortable?: boolean;
  filter?: boolean;
  filterType?: 'text' | 'numeric' | 'boolean' | 'date';
  width?: string;
}

export interface GenericTableConfig {
  paginator?: boolean;
  rows?: number;
  rowsPerPageOptions?: number[];
  globalFilter?: boolean;
  globalFilterFields?: string[];
  selectionMode?: 'single' | 'multiple' | null;
  dataKey?: string;
}

export interface QueryResult<T> {
  items?: T[];
  totalItems?: number;
}

export type DataSourceFn<T> = (meta: PrimeTableMetaData) => Observable<QueryResult<T>>;
