/**
 * 人力资源管理系统 - 统一配置表
 * 包含所有下拉列表选项、KPI指标、系统参数等配置
 */

/**
 * 组织级别配置
 */
export const orgLevelOptions = [
  { value: 0, label: '总部' },
  { value: 1, label: '分公司' },
  { value: 2, label: '部门' },
  { value: 3, label: '产线' },
  { value: 4, label: '班组' },
  { value: 5, label: '供应商' }
]

/**
 * 员工类型配置
 */
export const employeeTypeOptions = [
  { value: 0, label: '自主员工' },
  { value: 1, label: '第三方派遣' }
]

/**
 * 薪资模式配置
 */
export const salaryModeOptions = [
  { value: 0, label: '时薪制' },
  { value: 1, label: '固薪制' },
  { value: 2, label: '计件制' },
  { value: 3, label: '混合制' }
]

/**
 * 工作班次类型配置
 */
export const workShiftTypeOptions = [
  { value: 0, label: '工作日' },
  { value: 1, label: '周末' },
  { value: 2, label: '节假日' }
]

/**
 * 审批状态配置
 */
export const approvalStatusOptions = [
  { value: 0, label: '待审批' },
  { value: 1, label: '已通过' },
  { value: 2, label: '已驳回' }
]

/**
 * 账单状态配置
 */
export const billStatusOptions = [
  { value: 0, label: '草稿' },
  { value: 1, label: '已确认' },
  { value: 2, label: '已开票' },
  { value: 3, label: '已支付' }
]

/**
 * 费用类型配置
 */
export const expenseTypeOptions = [
  { value: 0, label: '报销' },
  { value: 1, label: '采购' },
  { value: 2, label: '其他' }
]

/**
 * 费用状态配置
 */
export const expenseStatusOptions = [
  { value: 0, label: '草稿' },
  { value: 1, label: '已提交' },
  { value: 2, label: '已审批' },
  { value: 3, label: '已驳回' },
  { value: 4, label: '已支付' }
]

/**
 * 合同类型配置
 */
export const contractTypeOptions = [
  { value: 0, label: '劳动合同' },
  { value: 1, label: '劳务合同' },
  { value: 2, label: '实习协议' }
]

/**
 * 员工标签配置
 */
export const employeeTagOptions = [
  { value: '实习生', label: '实习生' },
  { value: '试用期', label: '试用期' },
  { value: '正式员工', label: '正式员工' },
  { value: '管理层', label: '管理层' }
]

/**
 * 员工状态配置
 */
export const employeeStatusOptions = [
  { value: true, label: '在职' },
  { value: false, label: '离职' }
]

/**
 * ========== KPI指标配置 ==========
 */

/**
 * 考勤KPI指标
 */
export const attendanceKPI = {
  attendanceRate: {
    name: '考勤率',
    unit: '%',
    target: 95,
    formula: '实际出勤天数 / 应出勤天数 * 100%',
    weight: 20
  },
  lateRate: {
    name: '迟到率',
    unit: '%',
    target: 3,
    formula: '迟到次数 / 应出勤天数 * 100%',
    weight: 10
  },
  earlyLeaveRate: {
    name: '早退率',
    unit: '%',
    target: 2,
    formula: '早退次数 / 应出勤天数 * 100%',
    weight: 10
  },
  absentRate: {
    name: '旷工率',
    unit: '%',
    target: 0,
    formula: '旷工天数 / 应出勤天数 * 100%',
    weight: 15
  },
  overtimeComplianceRate: {
    name: '加班合规率',
    unit: '%',
    target: 90,
    formula: '合规加班时长 / 总加班时长 * 100%',
    weight: 15
  }
}

/**
 * 绩效KPI指标
 */
export const performanceKPI = {
  taskCompletionRate: {
    name: '任务完成率',
    unit: '%',
    target: 90,
    formula: '完成任务数 / 分配任务数 * 100%',
    weight: 25
  },
  workQualityScore: {
    name: '工作质量',
    unit: '分',
    target: 90,
    formula: '上级评分 + 同事评分 + 客户评分 / 3',
    weight: 25
  },
  workEfficiency: {
    name: '工作效率',
    unit: '指数',
    target: 100,
    formula: '实际产出 / 标准产出 * 100',
    weight: 20
  },
  innovationContribution: {
    name: '创新贡献',
    unit: '分',
    target: 80,
    formula: '创新提案数 * 评分权重',
    weight: 15
  },
  teamworkScore: {
    name: '团队协作',
    unit: '分',
    target: 85,
    formula: '团队成员互评平均分',
    weight: 15
  }
}

/**
 * 销售KPI指标
 */
export const salesKPI = {
  salesTargetRate: {
    name: '销售额完成率',
    unit: '%',
    target: 100,
    formula: '实际销售额 / 目标销售额 * 100%',
    weight: 30
  },
  newCustomerCount: {
    name: '新客户开发',
    unit: '个',
    target: 5,
    formula: '周期内新增客户数',
    weight: 20
  },
  customerSatisfaction: {
    name: '客户满意度',
    unit: '%',
    target: 90,
    formula: '满意客户数 / 受访客户数 * 100%',
    weight: 20
  },
  paymentCollectionRate: {
    name: '回款率',
    unit: '%',
    target: 95,
    formula: '已回款金额 / 应收金额 * 100%',
    weight: 15
  },
  salesCostControlRate: {
    name: '费用控制率',
    unit: '%',
    target: 100,
    formula: '实际费用 / 预算费用 * 100%',
    weight: 15
  }
}

/**
 * 生产KPI指标
 */
export const productionKPI = {
  productionRate: {
    name: '产量达标率',
    unit: '%',
    target: 95,
    formula: '实际产量 / 目标产量 * 100%',
    weight: 25
  },
  qualityRate: {
    name: '产品合格率',
    unit: '%',
    target: 99,
    formula: '合格产品数 / 总产量 * 100%',
    weight: 25
  },
  equipmentUtilization: {
    name: '设备利用率',
    unit: '%',
    target: 85,
    formula: '实际运行时间 / 可用时间 * 100%',
    weight: 20
  },
  materialWasteRate: {
    name: '物料损耗率',
    unit: '%',
    target: 3,
    formula: '损耗物料量 / 投入物料量 * 100%',
    weight: 15
  },
  safetyDays: {
    name: '安全生产天数',
    unit: '天',
    target: 30,
    formula: '连续无事故天数',
    weight: 15
  }
}

/**
 * 行政KPI指标
 */
export const adminKPI = {
  responseTimeliness: {
    name: '响应及时率',
    unit: '%',
    target: 95,
    formula: '及时响应事项数 / 总事项数 * 100%',
    weight: 25
  },
  serviceSatisfaction: {
    name: '服务满意度',
    unit: '%',
    target: 90,
    formula: '满意票数 / 总投票数 * 100%',
    weight: 25
  },
  facilityMaintenanceRate: {
    name: '设施维护率',
    unit: '%',
    target: 98,
    formula: '正常运行设施数 / 设施总数 * 100%',
    weight: 20
  },
  procurementCostControl: {
    name: '采购成本控制',
    unit: '%',
    target: 100,
    formula: '实际采购成本 / 预算成本 * 100%',
    weight: 15
  },
  documentAccuracy: {
    name: '文件处理准确率',
    unit: '%',
    target: 99,
    formula: '正确处理文件数 / 总文件数 * 100%',
    weight: 15
  }
}

/**
 * ========== 系统参数配置 ==========
 */

/**
 * 考勤参数
 */
export const attendanceSettings = {
  normalDailyHours: 8,
  lateThreshold: 15,
  earlyLeaveThreshold: 15,
  absentThreshold: 4,
  maxOvertimeHoursPerMonth: 36,
  annualLeaveDays: 15
}

/**
 * 薪资参数
 */
export const salarySettings = {
  socialSecurityBaseMin: 3613,
  socialSecurityBaseMax: 27786,
  housingFundBaseMin: 2200,
  housingFundBaseMax: 31884,
  taxThreshold: 5000,
  socialSecurityPersonalRate: 0.08,
  socialSecurityCompanyRate: 0.16,
  housingFundPersonalRate: 0.12,
  housingFundCompanyRate: 0.12,
  hourlyMinWage: 25,
  weekdayOvertimeMultiplier: 1.5,
  weekendOvertimeMultiplier: 2,
  holidayOvertimeMultiplier: 3
}

/**
 * 绩效评估参数
 */
export const performanceSettings = {
  evaluationCycleMonths: 3,
  excellentThreshold: 90,
  goodThreshold: 80,
  qualifiedThreshold: 60,
  excellentRatioLimit: 0.2,
  unqualifiedRatioLimit: 0.1,
  selfEvaluationWeight: 0.2,
  supervisorEvaluationWeight: 0.5,
  peerEvaluationWeight: 0.3
}

/**
 * 组织架构树状数据
 */
export const orgTreeData = [
  {
    id: 'root',
    name: '企业总部',
    code: 'HQ',
    level: 0,
    manager: '张三',
    phone: '13800138001',
    email: 'zhangsan@company.com',
    remark: '企业总部',
    children: [
      {
        id: 'project-nj',
        name: '南京项目部',
        code: 'PROJ-NJ',
        level: 1,
        manager: '李四',
        phone: '13800138002',
        email: 'lisi@company.com',
        remark: '南京项目组',
        children: [
          {
            id: 'line-nj-1',
            name: '生产线A',
            code: 'LINE-NJ-A',
            level: 2,
            manager: '王五',
            phone: '13800138003',
            email: 'wangwu@company.com',
            remark: '南京生产线A',
            children: [
              {
                id: 'team-nj-a-1',
                name: '一组',
                code: 'TEAM-NJ-A-1',
                level: 3,
                manager: '赵六',
                phone: '13800138004',
                email: 'zhaoliu@company.com',
                remark: 'A线一组'
              },
            ]
          },
          {
            id: 'line-nj-2',
            name: '生产线B',
            code: 'LINE-NJ-B',
            level: 2,
            manager: '吴九',
            phone: '13800138007',
            email: 'wujiu@company.com',
            remark: '南京生产线B'
          }
        ]
      },
      {
        id: 'project-fq',
        name: '福清项目部',
        code: 'PROJ-FQ',
        level: 1,
        manager: '郑十',
        phone: '13800138008',
        email: 'zhengshi@company.com',
        remark: '福清项目组',
        children: [
          {
            id: 'line-fq-1',
            name: '生产线C',
            code: 'LINE-FQ-C',
            level: 2,
            manager: '钱十一',
            phone: '13800138009',
            email: 'qianshiyi@company.com',
            remark: '福清生产线C'
          }
        ]
      }
    ]
  }
]


/**
 * ========== 配置表导出 ==========
 */
export const configTable = {
  selectOptions: {
    orgLevel: orgLevelOptions,
    employeeType: employeeTypeOptions,
    salaryMode: salaryModeOptions,
    workShiftType: workShiftTypeOptions,
    approvalStatus: approvalStatusOptions,
    billStatus: billStatusOptions,
    expenseType: expenseTypeOptions,
    expenseStatus: expenseStatusOptions,
    contractType: contractTypeOptions,
    employeeTag: employeeTagOptions,
    employeeStatus: employeeStatusOptions
  },
  kpi: {
    attendance: attendanceKPI,
    performance: performanceKPI,
    sales: salesKPI,
    production: productionKPI,
    admin: adminKPI
  },
  settings: {
    attendance: attendanceSettings,
    salary: salarySettings,
    performance: performanceSettings
  },
  orgTreeData: orgTreeData
}

export default configTable
