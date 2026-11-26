import { useCallback, useMemo, useState, type Key } from 'react';

/**
 * Розгортання рядків дерева в antd Table: усі вузли розгорнуті за замовчуванням (і нові після
 * перезавантаження теж), користувач може згортати. Зберігаються лише згорнуті вузли.
 *
 *   const expansion = useExpandAll(ids);
 *   <Table expandable={{ ...expansion, indentSize: 24 }} … />
 */
export function useExpandAll(ids: readonly number[]) {
  const [collapsed, setCollapsed] = useState<ReadonlySet<number>>(new Set());

  const expandedRowKeys = useMemo(() => ids.filter((id) => !collapsed.has(id)), [ids, collapsed]);
  const onExpandedRowsChange = useCallback((keys: readonly Key[]) => {
    const open = new Set(keys.map(Number));
    setCollapsed(new Set(ids.filter((id) => !open.has(id))));
  }, [ids]);

  return { expandedRowKeys, onExpandedRowsChange };
}
