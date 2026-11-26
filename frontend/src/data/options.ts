/** Елементи довідника -> опції antd Select. */
export const opt = (arr: { id: number; name: string }[]) =>
  arr.map((x) => ({ value: x.id, label: x.name }));
