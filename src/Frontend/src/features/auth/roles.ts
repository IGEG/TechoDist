/** Роли административного персонала. Значения совпадают с Techodist.Identity.Domain.AdminRoles. */
export const AdminRoles = {
  Admin: 'Admin',
  Manager: 'Manager',
} as const;

export type AdminRole = (typeof AdminRoles)[keyof typeof AdminRoles];
