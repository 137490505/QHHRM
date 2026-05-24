import request from './request'

export const orgUnitApi = {
  getAll: () => request.get('/v1/org-units'),
  getById: (id) => request.get(`/v1/org-units/${id}`),
  getByCode: (code) => request.get(`/v1/org-units/code/${code}`),
  getTree: () => request.get('/v1/org-units/tree'),
  getChildren: (parentId) => request.get(`/v1/org-units/${parentId}/children`),
  create: (data) => request.post('/v1/org-units', data),
  update: (data) => request.put('/v1/org-units', data),
  delete: (id) => request.delete(`/v1/org-units/${id}`),
  batchEnable: (ids) => request.post('/v1/org-units/batch-enable', ids),
  batchDisable: (ids) => request.post('/v1/org-units/batch-disable', ids),
  batchUpdate: (data) => request.post('/v1/org-units/batch-update', data),
  toggleStatus: (id) => request.post(`/v1/org-units/${id}/toggle-status`)
}

export const employeeApi = {
  getAll: (params) => request.get('/v1/employees', { params }),
  getById: (id) => request.get(`/v1/employees/${id}`),
  getByNo: (no) => request.get(`/v1/employees/no/${no}`),
  getByOrgUnit: (orgUnitId, params) => request.get(`/v1/employees/org-unit/${orgUnitId}`, { params }),
  getByThirdParty: (companyId) => request.get(`/v1/employees/third-party/${companyId}`),
  create: (data) => request.post('/v1/employees', data),
  update: (data) => request.put('/v1/employees', data),
  delete: (id) => request.delete(`/v1/employees/${id}`),
  toggleDismiss: (id) => request.post(`/v1/employees/${id}/toggle-dismiss`),
  batchDismiss: (data) => request.post('/v1/employees/batch-dismiss', data)
}

export const customerApi = {
  getAll: () => request.get('/v1/customers'),
  getById: (id) => request.get(`/v1/customers/${id}`),
  create: (data) => request.post('/v1/customers', data),
  update: (data) => request.put('/v1/customers', data),
  delete: (id) => request.delete(`/v1/customers/${id}`)
}

export const supplierApi = {
  getAll: () => request.get('/v1/suppliers'),
  getById: (id) => request.get(`/v1/suppliers/${id}`),
  create: (data) => request.post('/v1/suppliers', data),
  update: (data) => request.put('/v1/suppliers', data),
  delete: (id) => request.delete(`/v1/suppliers/${id}`)
}

export const timesheetApi = {
  getAll: () => request.get('/v1/timesheets'),
  getById: (id) => request.get(`/v1/timesheets/${id}`),
  getByEmployee: (employeeId, year, month) => request.get(`/v1/timesheets/employee/${employeeId}?year=${year}&month=${month}`),
  getByOrgUnit: (orgUnitId, startDate, endDate) => request.get(`/v1/timesheets/org-unit/${orgUnitId}?startDate=${startDate}&endDate=${endDate}`),
  create: (data) => request.post('/v1/timesheets', data),
  importBatch: (data) => request.post('/v1/timesheets/import', data),
  approve: (id, approverId) => request.post(`/v1/timesheets/${id}/approve`, null, { params: { approverId } })
}

export const salaryApi = {
  getAll: () => request.get('/v1/salary'),
  getById: (id) => request.get(`/v1/salary/${id}`),
  getByEmployee: (employeeId, year, month) => request.get(`/v1/salary/employee/${employeeId}?year=${year}&month=${month}`),
  calculate: (data) => request.post('/v1/salary/calculate', data)
}

export const payrollApi = {
  getRuns: (yearMonth) => request.get('/v1/payroll/runs', { params: { yearMonth } }),
  getRun: (runId) => request.get(`/v1/payroll/runs/${runId}`),
  getRunEmployee: (runId, employeeId) => request.get(`/v1/payroll/runs/${runId}/employees/${employeeId}`),
  trial: (data) => request.post('/v1/payroll/runs/trial', data),
  calculate: (data) => request.post('/v1/payroll/runs/calculate', data),
  approve: (runId, data = {}) => request.post(`/v1/payroll/runs/${runId}/approve`, data),
  pay: (runId, data = {}) => request.post(`/v1/payroll/runs/${runId}/pay`, data),
  rollback: (runId, remark) => request.post(`/v1/payroll/runs/${runId}/rollback`, null, { params: { remark } }),
  submitApproval: (runId, data = {}) => request.post(`/v1/payroll/runs/${runId}/submit-approval`, data)
}

export const todoCenterApi = {
  getMyTasks: (params) => request.get('/v1/todo/tasks/my-tasks', { params }),
  getTaskDetail: (id) => request.get(`/v1/todo/tasks/${id}/detail`),
  completeTask: (id, data = {}) => request.put(`/v1/todo/tasks/${id}/complete`, data),
  rejectTask: (id, data = {}) => request.put(`/v1/todo/tasks/${id}/reject`, data),
  transferTask: (id, data = {}) => request.put(`/v1/todo/tasks/${id}/transfer`, data),
  batchComplete: (data = {}) => request.post('/v1/todo/tasks/batch/complete', data),
  batchReject: (data = {}) => request.post('/v1/todo/tasks/batch/reject', data),
  batchTransfer: (data = {}) => request.post('/v1/todo/tasks/batch/transfer', data),
  batchUrge: (data = {}) => request.post('/v1/todo/tasks/batch/urge', data),
  urgeTask: (id, data = {}) => request.post(`/v1/todo/tasks/${id}/urge`, data),
  createAgentSetting: (data = {}) => request.post('/v1/todo/agent/settings', data),
  updateAgentSetting: (id, data = {}) => request.put(`/v1/todo/agent/settings/${id}`, data),
  disableAgentSetting: (id) => request.put(`/v1/todo/agent/settings/${id}/disable`),
  deleteAgentSetting: (id) => request.delete(`/v1/todo/agent/settings/${id}`),
  getAgentSettings: (principalUserId) => request.get('/v1/todo/agent/settings', { params: { principalUserId } }),
  getKpiSummary: (assigneeId) => request.get('/v1/todo/kpi/summary', { params: { assigneeId } })
}

export const processCenterApi = {
  getDefinitions: (params) => request.get('/v1/process/definitions', { params }),
  getDefinition: (id) => request.get(`/v1/process/definitions/${id}`),
  saveDefinition: (id, data) => id ? request.put(`/v1/process/definitions/${id}`, data) : request.post('/v1/process/definitions', data),
  publishDefinition: (id) => request.put(`/v1/process/definitions/${id}/publish`),
  getInstances: (params) => request.get('/v1/process/instances', { params }),
  getInstance: (id) => request.get(`/v1/process/instances/${id}`),
  getCallbackLogs: (id) => request.get(`/v1/process/instances/${id}/callback-logs`),
  retryCallback: (instanceId, logId) => request.post(`/v1/process/instances/${instanceId}/callback-logs/${logId}/retry`),
  rejectInstance: (id, data = {}) => request.post(`/v1/process/instances/${id}/reject`, data),
  rebuildCurrentTodo: (id, data = {}) => request.post(`/v1/process/instances/${id}/rebuild-current-todo`, data),
  terminateInstance: (id, data = {}) => request.post(`/v1/process/instances/${id}/terminate`, data),
  startProcess: (data) => request.post('/v1/process/start', data)
}

export const thirdPartyBillApi = {
  getAll: () => request.get('/v1/third-party-bills'),
  getById: (id) => request.get(`/v1/third-party-bills/${id}`),
  generate: (data) => request.post('/v1/third-party-bills/generate', data)
}

export const accessApi = {
  getMenuTree: (includeButtons = true, includeInactive = false) =>
    request.get('/v1/access/menus/tree', { params: { includeButtons, includeInactive } }),
  createMenu: (data) => request.post('/v1/access/menus', data),
  updateMenu: (data) => request.put('/v1/access/menus', data),
  deleteMenu: (id) => request.delete(`/v1/access/menus/${id}`),

  getRoles: () => request.get('/v1/access/roles'),
  getRole: (id) => request.get(`/v1/access/roles/${id}`),
  createRole: (data) => request.post('/v1/access/roles', data),
  updateRole: (data) => request.put('/v1/access/roles', data),
  deleteRole: (id) => request.delete(`/v1/access/roles/${id}`),
  assignRolePermissions: (id, menuIds) => request.put(`/v1/access/roles/${id}/permissions`, { menuIds }),
  copyRolePermissions: (id, sourceId) => request.put(`/v1/access/roles/${id}/permissions/copy`, { sourceId }),

  getPosts: () => request.get('/v1/access/posts'),
  getPost: (id) => request.get(`/v1/access/posts/${id}`),
  createPost: (data) => request.post('/v1/access/posts', data),
  updatePost: (data) => request.put('/v1/access/posts', data),
  deletePost: (id) => request.delete(`/v1/access/posts/${id}`),
  assignPostPermissions: (id, menuIds) => request.put(`/v1/access/posts/${id}/permissions`, { menuIds }),
  copyPostPermissions: (id, sourceId) => request.put(`/v1/access/posts/${id}/permissions/copy`, { sourceId }),

  getUsers: () => request.get('/v1/access/users'),
  getUser: (id) => request.get(`/v1/access/users/${id}`),
  getUserPermissionContext: (id) => request.get(`/v1/access/users/${id}/permissions`),
  createUser: (data) => request.post('/v1/access/users', data),
  updateUser: (data) => request.put('/v1/access/users', data),
  deleteUser: (id) => request.delete(`/v1/access/users/${id}`),

  getOptions: () => request.get('/v1/access/options')
}

export const settingsApi = {
  getLoginSecuritySettings: () => request.get('/v1/settings/login-security'),
  updateLoginSecuritySettings: (data) => request.put('/v1/settings/login-security', data),
  getSelectOptions: (category) => request.get(`/v1/settings/select-options/${category}`),
  updateSelectOptions: (category, data) => request.put(`/v1/settings/select-options/${category}`, data)
}
