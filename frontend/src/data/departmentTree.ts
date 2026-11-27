/** Дерево підрозділів для TreeSelect: групування за організаціями та підпорядкуванням. */

export interface DepartmentNode {
  id: number; name: string; isActive: boolean;
  organizationId: number; organizationName: string; parentDepartmentId?: number | null;
}

export interface DeptTreeOption {
  value: number | string;
  title: string;
  /** Що показується в полі після вибору: шлях підрозділу (і організація, якщо їх кілька). */
  label: string;
  selectable?: boolean;
  disabled?: boolean;
  children?: DeptTreeOption[];
}

const byName = (a: { title: string }, b: { title: string }) => a.title.localeCompare(b.title, 'uk');

/**
 * Будує дерево: організація → кореневі підрозділи → підпорядковані.
 * Якщо організація одна, її вузол не додається. Неактивні підрозділи позначені й недоступні
 * для вибору (крім `keepId` — поточного значення поля, щоб воно не зникало з форми).
 */
export function buildDepartmentTree(rows: readonly DepartmentNode[], keepId?: number | null): DeptTreeOption[] {
  const byId = new Map(rows.map((r) => [r.id, r]));
  const multiOrg = new Set(rows.map((r) => r.organizationId)).size > 1;

  const pathOf = (r: DepartmentNode) => {
    const parts: string[] = [];
    const seen = new Set<number>();
    for (let cur: DepartmentNode | undefined = r; cur && !seen.has(cur.id);
      cur = cur.parentDepartmentId != null ? byId.get(cur.parentDepartmentId) : undefined) {
      seen.add(cur.id);
      parts.unshift(cur.name);
    }
    return parts.join(' / ');
  };

  const nodes = new Map<number, DeptTreeOption>(rows.map((r) => [r.id, {
    value: r.id,
    title: r.isActive ? r.name : `${r.name} (неактивний)`,
    label: multiOrg ? `${pathOf(r)} — ${r.organizationName}` : pathOf(r),
    disabled: !r.isActive && r.id !== keepId,
  }]));

  const orgRoots = new Map<number, DeptTreeOption[]>();
  for (const r of rows) {
    const node = nodes.get(r.id)!;
    const parent = r.parentDepartmentId != null ? nodes.get(r.parentDepartmentId) : undefined;
    if (parent) (parent.children ??= []).push(node);
    else {
      const list = orgRoots.get(r.organizationId) ?? [];
      list.push(node);
      orgRoots.set(r.organizationId, list);
    }
  }

  const sortDeep = (list: DeptTreeOption[]): DeptTreeOption[] => {
    list.sort(byName);
    for (const n of list) if (n.children) sortDeep(n.children);
    return list;
  };

  if (!multiOrg) return sortDeep([...orgRoots.values()].flat());

  return [...orgRoots.entries()]
    .map(([orgId, children]) => {
      const name = rows.find((r) => r.organizationId === orgId)!.organizationName;
      return { value: `org-${orgId}`, title: name, label: name, selectable: false, children: sortDeep(children) };
    })
    .sort(byName);
}
