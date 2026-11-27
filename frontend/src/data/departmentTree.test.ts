import { describe, expect, it } from 'vitest';
import { buildDepartmentTree, type DepartmentNode } from './departmentTree';

const d = (id: number, name: string, org: number, parent: number | null = null, isActive = true): DepartmentNode => ({
  id, name, isActive, organizationId: org, organizationName: org === 1 ? 'Промінь' : 'Генштаб', parentDepartmentId: parent,
});

describe('buildDepartmentTree', () => {
  it('одна організація — без вузла організації, з ієрархією та шляхом', () => {
    const tree = buildDepartmentTree([d(1, 'Адміністрація', 1), d(2, 'Бухгалтерія', 1, 1)]);
    expect(tree).toHaveLength(1);
    expect(tree[0].title).toBe('Адміністрація');
    expect(tree[0].children?.[0]).toMatchObject({ value: 2, label: 'Адміністрація / Бухгалтерія' });
  });

  it('кілька організацій — групування, вузол організації не вибирається, у підписі є організація', () => {
    const tree = buildDepartmentTree([d(1, 'Адміністрація', 1), d(3, 'J-3', 2)]);
    expect(tree.map((n) => n.title)).toEqual(['Генштаб', 'Промінь']);
    expect(tree[0]).toMatchObject({ value: 'org-2', selectable: false });
    expect(tree[0].children?.[0].label).toBe('J-3 — Генштаб');
  });

  it('неактивні недоступні, крім поточного значення', () => {
    const rows = [d(1, 'Старий', 1, null, false), d(2, 'Архів', 1, null, false)];
    const tree = buildDepartmentTree(rows, 2);
    expect(tree.find((n) => n.value === 1)).toMatchObject({ disabled: true, title: 'Старий (неактивний)' });
    expect(tree.find((n) => n.value === 2)?.disabled).toBe(false);
  });
});
