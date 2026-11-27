import { useCallback, useEffect, useState } from 'react';
import {
  App, Badge, Button, Card, Checkbox, Col, ConfigProvider, Drawer, Empty, Form, Input, Row, Select, Space, Spin, Switch, Tabs, Typography,
} from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import { api } from '../../api/client';

import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';
import { errorMessage, useApiAction, useApiData } from '../../hooks/useApi';
import { PARTY_KINDS, partyCodeError } from '../../data/partyCodes';
import BankAccountsTab from './BankAccountsTab';
import { ADDRESS_KINDS, CONTACT_KINDS, type PartyDetail, type PartyListItem } from './types';

interface DictItem { id: number; name: string; code?: string; }
const loadLegalForms = () => api<DictItem[]>('/api/dictionaries/legal-forms');
const loadCategories = () => api<DictItem[]>('/api/dictionaries/party-categories');

interface Props {
  /** null — закрито; 'new' — новий контрагент; число — картка наявного */
  partyId: number | 'new' | null;
  parties: PartyListItem[];
  onClose: () => void;
  /** Після збереження: перечитати реєстр (новий контрагент лишається відкритим — щоб додати рахунки) */
  onSaved: (id: number) => void;
}

const NEW_PARTY = { kind: 1, isActive: true, addresses: [], contacts: [], persons: [], categoryIds: [] };

/** Картка контрагента в бічній панелі; для кожного контрагента — окремий екземпляр (стан не переноситься). */
export default function PartyDrawer(props: Props) {
  return props.partyId === null ? null : <PartyCard key={String(props.partyId)} {...props} partyId={props.partyId} />;
}

/** Картка контрагента: реквізити, адреси, контакти, контактні особи, банківські рахунки. */
function PartyCard({ partyId, parties, onClose, onSaved }: Props & { partyId: number | 'new' }) {
  const { can } = useAuth();
  const editable = can(P.parties);
  const run = useApiAction();
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [detail, setDetail] = useState<PartyDetail | null>(null);
  const [loading, setLoading] = useState(partyId !== 'new');
  const [tab, setTab] = useState('main');
  const { data: legalForms } = useApiData(loadLegalForms, []);
  const { data: categories } = useApiData(loadCategories, []);
  const kind = Form.useWatch('kind', form) as number | undefined;
  const legal = kind === 1 || kind === 4;

  const load = useCallback(async (id: number) => {
    try {
      const d = await api<PartyDetail>(`/api/parties/${id}`);
      setDetail(d);
      form.setFieldsValue(d);
    } finally {
      setLoading(false);
    }
  }, [form]);

  const reloadDetail = (id: number) => { setLoading(true); return load(id); };

  useEffect(() => {
    if (partyId !== 'new') load(partyId).catch((e) => { message.error(errorMessage(e)); onClose(); });
  }, [partyId, load, onClose, message]);

  const save = async () => {
    let v;
    try {
      v = await form.validateFields();
    } catch (e) {
      // Перейти на вкладку з першою помилкою
      const first = (e as { errorFields?: { name: (string | number)[] }[] }).errorFields?.[0]?.name[0];
      const tabOf: Record<string, string> = { addresses: 'addresses', contacts: 'contacts', persons: 'persons' };
      setTab(tabOf[String(first)] ?? 'main');
      return;
    }
    const body = JSON.stringify({
      ...v,
      code: v.code?.trim() || null,
      legalFormId: legal ? v.legalFormId ?? null : null,
      parentPartyId: legal ? v.parentPartyId ?? null : null,
    });
    let id = detail?.id;
    const ok = await run(async () => {
      if (id) await api(`/api/parties/${id}`, { method: 'PUT', body });
      else id = (await api<{ id: number }>('/api/parties', { method: 'POST', body })).id;
    }, 'Контрагента збережено');
    if (ok && id) { await reloadDetail(id); onSaved(id); }
  };

  const parentOptions = parties
    .filter((p) => p.id !== detail?.id && p.parentPartyId == null && (p.kind === 1 || p.kind === 4))
    .map((p) => ({ value: p.id, label: p.shortName ?? p.fullName }));

  const title = partyId === 'new' ? 'Новий контрагент' : detail ? (detail.shortName ?? detail.fullName) : 'Контрагент';
  const addressCount = (Form.useWatch('addresses', form) as unknown[] | undefined)?.length ?? 0;
  const contactCount = (Form.useWatch('contacts', form) as unknown[] | undefined)?.length ?? 0;
  const personCount = (Form.useWatch('persons', form) as unknown[] | undefined)?.length ?? 0;
  const label = (text: string, n: number) => <Space size={4}>{text}<Badge count={n} color="#8c8c8c" size="small" /></Space>;

  return (
    <Drawer open onClose={onClose} size={880} title={title}
      extra={editable && <Button type="primary" onClick={save}>Зберегти</Button>}>
      <Spin spinning={loading}>
        <Form form={form} layout="vertical" disabled={!editable} initialValues={partyId === 'new' ? NEW_PARTY : undefined}>
          <Tabs activeKey={tab} onChange={setTab} items={[
            {
              key: 'main', label: 'Реквізити', forceRender: true, children: (
                <>
                  <Form.Item name="kind" label="Вид контрагента" rules={[{ required: true }]}>
                    <Select options={PARTY_KINDS.map((k) => ({ value: k.id, label: k.name }))} />
                  </Form.Item>
                  <Form.Item name="fullName" label={kind === 2 || kind === 3 ? 'Повне ім\'я / найменування' : 'Повна назва'}
                    rules={[{ required: true, message: 'Вкажіть повну назву або ПІБ' }]}>
                    <Input placeholder={kind === 3 ? 'Прізвище Ім\'я По батькові' : 'ТОВАРИСТВО З ОБМЕЖЕНОЮ ВІДПОВІДАЛЬНІСТЮ «…»'} />
                  </Form.Item>
                  <Form.Item name="shortName" label="Коротка назва" tooltip="Так контрагент показується в документах і списках">
                    <Input placeholder={kind === 2 ? 'ФОП Прізвище І.Б.' : 'ТОВ «…»'} />
                  </Form.Item>
                  <Row gutter={16}>
                    <Col xs={24} sm={10}>
                      <Form.Item name="code" label={PARTY_KINDS.find((k) => k.id === kind)?.code ?? 'Код'}
                        dependencies={['kind']}
                        rules={[{ validator: (_, v) => { const e = partyCodeError(kind, v?.trim()); return e ? Promise.reject(new Error(e)) : Promise.resolve(); } }]}>
                        <Input maxLength={10} style={{ fontFamily: 'monospace' }} />
                      </Form.Item>
                    </Col>
                    {legal && (
                      <Col xs={24} sm={14}>
                        <Form.Item name="legalFormId" label="Організаційно-правова форма">
                          <Select allowClear showSearch optionFilterProp="label"
                            options={legalForms.map((f) => ({ value: f.id, label: f.code ? `${f.code} — ${f.name}` : f.name }))} />
                        </Form.Item>
                      </Col>
                    )}
                  </Row>
                  {legal && (
                    <Form.Item name="parentPartyId" label="Головна юридична особа" tooltip="Для філій і відокремлених підрозділів">
                      <Select allowClear showSearch optionFilterProp="label" options={parentOptions} placeholder="—" />
                    </Form.Item>
                  )}
                  <Form.Item name="categoryIds" label="Категорії">
                    <Select mode="multiple" allowClear optionFilterProp="label"
                      options={categories.map((c) => ({ value: c.id, label: c.name }))} />
                  </Form.Item>
                  <Form.Item name="note" label="Примітка"><Input.TextArea rows={2} /></Form.Item>
                  <Form.Item name="isActive" label="Діє" valuePropName="checked"
                    extra="Неактивний контрагент лишається в документах, але не пропонується для нових.">
                    <Switch />
                  </Form.Item>
                  {detail && detail.documentCount > 0 && (
                    <Typography.Text type="secondary">Документів з цим контрагентом: {detail.documentCount}</Typography.Text>
                  )}
                </>
              ),
            },
            {
              key: 'addresses', label: label('Адреси', addressCount), forceRender: true, children: (
                <Form.List name="addresses">
                  {(fields, { add, remove }) => (
                    <>
                      {fields.length === 0 && <Empty description="Адрес немає" image={Empty.PRESENTED_IMAGE_SIMPLE} />}
                      {fields.map(({ key, name }) => (
                        <Card key={key} size="small" style={{ marginBottom: 12 }}
                          title={<Form.Item name={[name, 'kind']} noStyle rules={[{ required: true }]}>
                            <Select options={ADDRESS_KINDS} style={{ width: 160 }} variant="borderless" />
                          </Form.Item>}
                          extra={editable && <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(name)} />}>
                          <Row gutter={12}>
                            <Col span={8}><Form.Item name={[name, 'postalCode']} label="Індекс"><Input maxLength={10} /></Form.Item></Col>
                            <Col span={16}><Form.Item name={[name, 'country']} label="Країна"><Input /></Form.Item></Col>
                            <Col span={12}><Form.Item name={[name, 'region']} label="Область"><Input /></Form.Item></Col>
                            <Col span={12}><Form.Item name={[name, 'city']} label="Населений пункт"><Input /></Form.Item></Col>
                            <Col span={24}>
                              <Form.Item name={[name, 'line']} label="Вулиця, будинок, офіс" style={{ marginBottom: 0 }}
                                rules={[({ getFieldValue }) => ({
                                  validator: (_, v) => (v || getFieldValue(['addresses', name, 'city']) ? Promise.resolve()
                                    : Promise.reject(new Error('Вкажіть населений пункт або адресу'))),
                                })]}>
                                <Input />
                              </Form.Item>
                            </Col>
                          </Row>
                        </Card>
                      ))}
                      {editable && (
                        <Button type="dashed" block icon={<PlusOutlined />}
                          onClick={() => {
                            const used = (form.getFieldValue('addresses') ?? []).map((a: { kind: number }) => a.kind);
                            add({ kind: ADDRESS_KINDS.find((k) => !used.includes(k.value))?.value ?? 2, country: 'Україна' });
                          }}>
                          Додати адресу
                        </Button>
                      )}
                    </>
                  )}
                </Form.List>
              ),
            },
            {
              key: 'contacts', label: label('Контакти', contactCount), forceRender: true, children: (
                <Form.List name="contacts">
                  {(fields, { add, remove }) => (
                    <>
                      {fields.length === 0 && <Empty description="Контактів немає" image={Empty.PRESENTED_IMAGE_SIMPLE} />}
                      {fields.map(({ key, name }) => (
                        <Space key={key} align="start" wrap style={{ display: 'flex', marginBottom: 4 }}>
                          <Form.Item name={[name, 'kind']} rules={[{ required: true }]}>
                            <Select options={CONTACT_KINDS} style={{ width: 170 }} />
                          </Form.Item>
                          <Form.Item name={[name, 'value']} dependencies={[['contacts', name, 'kind']]}
                            rules={[{ required: true, message: 'Вкажіть значення' }, ({ getFieldValue }) => ({
                              type: getFieldValue(['contacts', name, 'kind']) === 2 ? 'email' : 'string',
                              message: 'Невірна адреса електронної пошти',
                            })]}>
                            <Input style={{ width: 260 }} />
                          </Form.Item>
                          <Form.Item name={[name, 'isPrimary']} valuePropName="checked"><Checkbox>Основний</Checkbox></Form.Item>
                          {editable && <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(name)} />}
                        </Space>
                      ))}
                      {editable && (
                        <Button type="dashed" block icon={<PlusOutlined />} onClick={() => add({ kind: 1, isPrimary: false })}>
                          Додати контакт
                        </Button>
                      )}
                    </>
                  )}
                </Form.List>
              ),
            },
            {
              key: 'persons', label: label('Особи', personCount), forceRender: true, children: (
                <Form.List name="persons">
                  {(fields, { add, remove }) => (
                    <>
                      {fields.length === 0 && <Empty description="Контактних осіб немає" image={Empty.PRESENTED_IMAGE_SIMPLE} />}
                      {fields.map(({ key, name }) => (
                        <Card key={key} size="small" style={{ marginBottom: 12 }}
                          extra={editable && <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(name)} />}
                          title={<Form.Item name={[name, 'isSignatory']} valuePropName="checked" noStyle>
                            <Checkbox>Має право підпису</Checkbox></Form.Item>}>
                          <Row gutter={12}>
                            <Col span={14}>
                              <Form.Item name={[name, 'fullName']} label="ПІБ" rules={[{ required: true, message: 'Вкажіть ПІБ' }]}>
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col span={10}><Form.Item name={[name, 'positionName']} label="Посада"><Input /></Form.Item></Col>
                            <Col span={12}><Form.Item name={[name, 'phone']} label="Телефон" style={{ marginBottom: 0 }}><Input /></Form.Item></Col>
                            <Col span={12}>
                              <Form.Item name={[name, 'email']} label="Email" rules={[{ type: 'email', message: 'Невірна адреса' }]}
                                style={{ marginBottom: 0 }}>
                                <Input />
                              </Form.Item>
                            </Col>
                          </Row>
                        </Card>
                      ))}
                      {editable && (
                        <Button type="dashed" block icon={<PlusOutlined />} onClick={() => add({ isSignatory: false })}>
                          Додати особу
                        </Button>
                      )}
                    </>
                  )}
                </Form.List>
              ),
            },
            {
              key: 'accounts', label: label('Рахунки', detail?.bankAccounts.filter((a) => !a.closedAt).length ?? 0),
              children: detail
                // Рахунки мають окреме право — форма реквізитів (disabled без parties.manage) на них не впливає
                ? <ConfigProvider componentDisabled={false}>
                    <BankAccountsTab partyId={detail.id} accounts={detail.bankAccounts}
                      onChanged={() => { reloadDetail(detail.id); onSaved(detail.id); }} />
                  </ConfigProvider>
                : <Empty description="Збережіть контрагента, щоб додати банківські рахунки" image={Empty.PRESENTED_IMAGE_SIMPLE} />,
            },
          ]} />
        </Form>
      </Spin>
    </Drawer>
  );
}
