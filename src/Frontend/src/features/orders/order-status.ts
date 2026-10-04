/**
 * Статусы заявки. Значения совпадают с Techodist.Order.Domain.Enums.OrderStatus —
 * backend и события говорят строками (OrderDto.Status).
 */
export const OrderStatuses = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  InProgress: 'InProgress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
} as const;

export type OrderStatus = (typeof OrderStatuses)[keyof typeof OrderStatuses];

/** Воронка обработки заявки менеджером (для индикатора на витрине). */
export const OrderStatusSequence: readonly OrderStatus[] = [
  OrderStatuses.Pending,
  OrderStatuses.Confirmed,
  OrderStatuses.InProgress,
  OrderStatuses.Completed,
];

/** Терминальные статусы: дальше заявка не меняется. */
export function isTerminalOrderStatus(status: string): boolean {
  return status === OrderStatuses.Completed || status === OrderStatuses.Cancelled;
}

/** Предпочтительный канал связи (OrderContactChannel). */
export const OrderContactChannels = {
  Email: 'Email',
  Phone: 'Phone',
} as const;

/** Срочность заявки (OrderPriority). */
export const OrderPriorities = {
  Standard: 'Standard',
  Urgent: 'Urgent',
} as const;
