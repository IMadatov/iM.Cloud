import { UrlMatchResult, UrlMatcher, UrlSegment } from '@angular/router';

const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const filesPathMatcher: UrlMatcher = (segments): UrlMatchResult | null => {
  if (!segments.length || segments[0].path !== 'files') {
    return null;
  }

  const folderSegments = segments.slice(1);
  if (folderSegments.some((segment) => !guidPattern.test(segment.path))) {
    return null;
  }

  return {
    consumed: segments,
    posParams: folderSegments.length
      ? { path: new UrlSegment(folderSegments.map((s) => s.path).join('/'), {}) }
      : {},
  };
};
