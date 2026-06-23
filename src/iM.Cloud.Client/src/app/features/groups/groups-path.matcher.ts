import { UrlMatchResult, UrlMatcher, UrlSegment } from '@angular/router';

const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const groupsPathMatcher: UrlMatcher = (segments): UrlMatchResult | null => {
  if (segments.length < 2 || segments[0].path !== 'groups') {
    return null;
  }

  const groupId = segments[1].path;
  if (!guidPattern.test(groupId)) {
    return null;
  }

  const folderSegments = segments.slice(2);
  if (folderSegments.some((segment) => !guidPattern.test(segment.path))) {
    return null;
  }

  return {
    consumed: segments,
    posParams: {
      groupId: new UrlSegment(groupId, {}),
      ...(folderSegments.length
        ? { path: new UrlSegment(folderSegments.map((s) => s.path).join('/'), {}) }
        : {}),
    },
  };
};
