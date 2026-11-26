import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useExpandAll } from './useExpandAll';

describe('useExpandAll', () => {
  it('усі вузли розгорнуті за замовчуванням', () => {
    const { result } = renderHook(() => useExpandAll([1, 2, 3]));
    expect(result.current.expandedRowKeys).toEqual([1, 2, 3]);
  });

  it('пам\'ятає згорнуті вузли, нові вузли — розгорнуті', () => {
    const { result, rerender } = renderHook(({ ids }) => useExpandAll(ids), { initialProps: { ids: [1, 2, 3] } });

    act(() => result.current.onExpandedRowsChange([1, 3]));
    expect(result.current.expandedRowKeys).toEqual([1, 3]);

    rerender({ ids: [1, 2, 3, 4] });
    expect(result.current.expandedRowKeys).toEqual([1, 3, 4]);
  });
});
