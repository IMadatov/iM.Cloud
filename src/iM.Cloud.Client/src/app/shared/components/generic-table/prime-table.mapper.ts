import { FilterMetadata as ApiFilterMetadata, PrimeTableMetaData } from '@im-cloud/api';
import { FilterMetadata } from 'primeng/api';
import { TableLazyLoadEvent } from 'primeng/table';

export function toPrimeTableMetaData(
  event: TableLazyLoadEvent,
  globalFilter = '',
): PrimeTableMetaData {
  const meta = new PrimeTableMetaData();
  meta.first = event.first ?? 0;
  meta.rows = event.rows ?? 10;
  meta.sortField = (event.sortField as string | undefined) ?? 'id';
  meta.sortOrder = event.sortOrder ?? 1;
  meta.globalFilter = globalFilter;
  meta.filters = mapFilters(event.filters);
  return meta;
}

function mapFilters(
  filters?: Record<string, FilterMetadata | FilterMetadata[] | undefined>,
): { [key: string]: ApiFilterMetadata } | undefined {
  if (!filters) {
    return undefined;
  }

  const result: { [key: string]: ApiFilterMetadata } = {};

  for (const [key, value] of Object.entries(filters)) {
    const item = Array.isArray(value) ? value[0] : value;
    if (!item || isEmptyFilterValue(item.value)) {
      continue;
    }

    result[key] = new ApiFilterMetadata({
      value: item.value,
      matchMode: item.matchMode ?? 'contains',
    });
  }

  return Object.keys(result).length > 0 ? result : undefined;
}

function isEmptyFilterValue(value: unknown): boolean {
  if (value === null || value === undefined) {
    return true;
  }

  if (typeof value === 'string') {
    return value.trim() === '';
  }

  return false;
}
