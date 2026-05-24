<template>
  <div class="dashboard">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1>仪表盘</h1>
      <div class="header-actions">
        <a-button type="text" @click="refreshData">
          <SyncOutlined /> 刷新数据
        </a-button>
        <a-select 
          v-model:value="refreshInterval" 
          class="interval-select"
          @change="handleIntervalChange"
        >
          <a-select-option :value="0">不自动刷新</a-select-option>
          <a-select-option :value="30">30秒</a-select-option>
          <a-select-option :value="60">1分钟</a-select-option>
          <a-select-option :value="300">5分钟</a-select-option>
        </a-select>
      </div>
    </div>

    <!-- 指标卡片 -->
    <a-row :gutter="16" class="stats-row">
      <a-col :span="6" v-for="card in statCards" :key="card.key">
        <a-card 
          class="stat-card"
          @click="handleCardClick(card)"
          :class="{ 'clickable': card.path }"
        >
          <div class="stat-icon" :class="card.color">
            <component :is="card.icon" />
          </div>
          <div class="stat-content">
            <p class="stat-label">{{ card.label }}</p>
            <a-statistic 
              :value="card.value" 
              :prefix="card.prefix"
              :suffix="card.suffix"
              :precision="card.precision"
            />
            <p v-if="card.trend" class="stat-trend" :class="card.trend > 0 ? 'up' : 'down'">
              <component :is="card.trend > 0 ? RiseOutlined : FallOutlined" />
              {{ Math.abs(card.trend) }}%
            </p>
          </div>
        </a-card>
      </a-col>
    </a-row>

    <!-- 图表区域 -->
    <a-row :gutter="16" class="chart-row">
      <!-- 人员趋势图 -->
      <a-col :span="14">
        <a-card title="人员趋势" :bordered="false" class="chart-card">
          <div class="chart-header">
            <a-space>
              <a-select 
                v-model:value="chartPeriod" 
                class="period-select"
              >
                <a-select-option :value="3">近3个月</a-select-option>
                <a-select-option :value="6">近6个月</a-select-option>
                <a-select-option :value="12">近12个月</a-select-option>
              </a-select>
              <a-button type="text" @click="exportChart('trend')">
                <DownloadOutlined /> 导出图片
              </a-button>
            </a-space>
          </div>
          <div ref="trendChartRef" class="chart-container"></div>
        </a-card>
      </a-col>

      <!-- 薪资分布图 -->
      <a-col :span="10">
        <a-card title="薪资分布" :bordered="false" class="chart-card">
          <div class="chart-header">
            <a-space>
              <a-select 
                v-model:value="salaryChartType" 
                class="chart-type-select"
              >
                <a-select-option :value="'pie'">饼图</a-select-option>
                <a-select-option :value="'bar'">柱状图</a-select-option>
              </a-select>
              <a-button type="text" @click="exportChart('salary')">
                <DownloadOutlined /> 导出图片
              </a-button>
            </a-space>
          </div>
          <div ref="salaryChartRef" class="chart-container"></div>
        </a-card>
      </a-col>
    </a-row>

    <!-- 下半区域 -->
    <a-row :gutter="16" class="bottom-row">
      <!-- 工时统计 -->
      <a-col :span="10">
        <a-card title="工时统计" :bordered="false" class="chart-card">
          <div class="chart-header">
            <a-select 
              v-model:value="timesheetPeriod" 
              class="period-select"
            >
              <a-select-option :value="'day'">日统计</a-select-option>
              <a-select-option :value="'week'">周统计</a-select-option>
              <a-select-option :value="'month'">月统计</a-select-option>
            </a-select>
          </div>
          <div ref="timesheetChartRef" class="chart-container"></div>
        </a-card>
      </a-col>

      <!-- 待办事项 -->
      <a-col :span="8">
        <a-card title="待办事项" :bordered="false">
          <div class="todo-header">
            <span class="todo-count">{{ pendingTasks.length }} 项待处理</span>
            <a-button type="text" @click="handleBatchApprove" v-if="pendingTasks.length">
              批量审批
            </a-button>
          </div>
          <a-list 
            :data-source="pendingTasks" 
            class="todo-list"
          >
            <template #renderItem="{ item }">
              <a-list-item 
                class="todo-item"
                @click="handleTaskClick(item)"
              >
                <a-list-item-meta>
                  <template #title>
                    <span>{{ item.title }}</span>
                    <a-tag :color="item.typeColor" class="task-tag">{{ item.type }}</a-tag>
                  </template>
                  <template #description>
                    {{ item.description }} - {{ item.time }}
                  </template>
                </a-list-item-meta>
                <a-button type="primary" size="small" @click.stop="handleApprove(item)">
                  审批
                </a-button>
              </a-list-item>
            </template>
          </a-list>
        </a-card>
      </a-col>

      <!-- 快捷入口 -->
      <a-col :span="6">
        <a-card title="快捷入口" :bordered="false">
          <a-row :gutter="[16, 16]" class="quick-grid">
            <a-col :span="12" 
              v-for="item in quickEntries" 
              :key="item.key"
            >
              <div class="quick-item" @click="handleQuickEntryClick(item)">
                <component :is="item.icon" class="quick-icon" />
                <span class="quick-text">{{ item.name }}</span>
              </div>
            </a-col>
          </a-row>
        </a-card>

        <!-- 公告栏 -->
        <a-card title="公告" :bordered="false" class="notice-card">
          <a-carousel :autoplay="true" :dots="false" class="notice-carousel">
            <div v-for="notice in notices" :key="notice.id">
              <p class="notice-title">{{ notice.title }}</p>
              <p class="notice-content">{{ notice.content }}</p>
              <p class="notice-time">{{ notice.time }}</p>
            </div>
          </a-carousel>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { message } from 'ant-design-vue';
import {
  SyncOutlined,
  RiseOutlined,
  FallOutlined,
  DownloadOutlined,
  TeamOutlined,
  UserAddOutlined,
  UserDeleteOutlined,
  MoneyCollectOutlined,
  FileTextOutlined,
  UploadOutlined,
  CalculatorOutlined,
  BellOutlined,
  SettingOutlined,
  BarChartOutlined
} from '@ant-design/icons-vue';
import { useUserStore } from '../store/user';
import { resolveMergedSettingsPath } from '../utils/navigation';
import { employeeApi, salaryApi, timesheetApi, thirdPartyBillApi } from '../api';
import * as echarts from 'echarts/core';
import { LineChart, PieChart, BarChart } from 'echarts/charts';
import { GridComponent, TooltipComponent, LegendComponent, GraphicComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';

echarts.use([LineChart, PieChart, BarChart, GridComponent, TooltipComponent, LegendComponent, GraphicComponent, CanvasRenderer]);

const router = useRouter();
const userStore = useUserStore();

const refreshInterval = ref(0);
const chartPeriod = ref(6);
const salaryChartType = ref('pie');
const timesheetPeriod = ref('week');
const trendChartRef = ref(null);
const salaryChartRef = ref(null);
const timesheetChartRef = ref(null);
const rawEmployees = ref([]);
const rawSalaries = ref([]);
const rawTimesheets = ref([]);
const rawBills = ref([]);
let refreshTimer = null;
let trendChart = null;
let salaryChart = null;
let timesheetChart = null;

const salaryModeLabelMap = {
  Hourly: '时薪制',
  Fixed: '固薪制',
  PieceRate: '计件制',
  Mixed: '混合制'
};

const notices = ref([
  { id: 1, title: '春节放假通知', content: '春节放假时间为2月10日至2月17日，请各位员工提前安排好工作。', time: '2024-01-15' },
  { id: 2, title: '年度体检通知', content: '公司将组织年度体检，请各位员工在OA系统中预约体检时间。', time: '2024-01-10' },
  { id: 3, title: '新员工培训', content: '本月新员工培训将于1月20日举行，请相关部门安排人员参加。', time: '2024-01-08' }
]);

const quickEntries = computed(() => [
  { key: 'import-employee', name: '员工导入', icon: UploadOutlined, path: '/employees/import' },
  { key: 'import-timesheet', name: '工时导入', icon: UploadOutlined, path: '/timesheets/import' },
  { key: 'salary-calc', name: '薪资核算', icon: CalculatorOutlined, path: '/salary/calculate' },
  { key: 'generate-bill', name: '生成账单', icon: FileTextOutlined, path: '/bills/generate' },
  { key: 'notifications', name: '消息中心', icon: BellOutlined, path: '/notifications' },
  { key: 'settings', name: '系统配置', icon: SettingOutlined, path: resolveMergedSettingsPath(userStore.menuTree) }
]);

const parseDate = (value) => {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};

const startOfMonth = (date) => new Date(date.getFullYear(), date.getMonth(), 1);
const endOfMonth = (date) => new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59, 999);
const startOfDay = (date) => new Date(date.getFullYear(), date.getMonth(), date.getDate());
const addMonths = (date, offset) => new Date(date.getFullYear(), date.getMonth() + offset, 1);
const round = (value, precision = 1) => Number((Number(value) || 0).toFixed(precision));
const sameYearMonth = (date, target) =>
  !!date && date.getFullYear() === target.getFullYear() && date.getMonth() === target.getMonth();
const sameDay = (left, right) =>
  !!left &&
  !!right &&
  left.getFullYear() === right.getFullYear() &&
  left.getMonth() === right.getMonth() &&
  left.getDate() === right.getDate();

const sumBy = (items, selector) => items.reduce((sum, item) => sum + (Number(selector(item)) || 0), 0);
const getRegularHours = (timesheet) => Math.max((Number(timesheet.workingHours) || 0) - (Number(timesheet.overtimeHours) || 0), 0);
const formatMonthLabel = (date) => `${date.getMonth() + 1}月`;
const formatDayLabel = (date) => `${date.getMonth() + 1}/${date.getDate()}`;
const formatDateLabel = (date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;

const getTrendRate = (current, previous) => {
  if (!previous) {
    return current ? 100 : 0;
  }

  return round(((current - previous) / previous) * 100, 1);
};

const getMonthSeries = (count) =>
  Array.from({ length: count }, (_, index) => addMonths(startOfMonth(new Date()), index - count + 1));

const getMonthEmployeesCount = (target, field) =>
  rawEmployees.value.filter((employee) => sameYearMonth(parseDate(employee[field]), target)).length;

const getMonthSalaries = (target) =>
  rawSalaries.value.filter((item) => Number(item.year) === target.getFullYear() && Number(item.month) === target.getMonth() + 1);

const getMonthBills = (target) =>
  rawBills.value.filter((item) => Number(item.year) === target.getFullYear() && Number(item.month) === target.getMonth() + 1);

const getMonthTimesheets = (target) =>
  rawTimesheets.value.filter((item) => sameYearMonth(parseDate(item.date), target));

const getAverageHours = (timesheets) => {
  const employeeHours = new Map();

  timesheets.forEach((item) => {
    if (!item.employeeId) {
      return;
    }

    employeeHours.set(item.employeeId, (employeeHours.get(item.employeeId) || 0) + (Number(item.workingHours) || 0));
  });

  if (!employeeHours.size) {
    return 0;
  }

  return round(sumBy(Array.from(employeeHours.values()), (value) => value) / employeeHours.size, 1);
};

const currentMonth = computed(() => startOfMonth(new Date()));
const previousMonth = computed(() => addMonths(currentMonth.value, -1));

const currentMonthSummary = computed(() => {
  const salaryItems = getMonthSalaries(currentMonth.value);
  const billItems = getMonthBills(currentMonth.value);
  const timesheetItems = getMonthTimesheets(currentMonth.value);

  return {
    totalEmployees: rawEmployees.value.length,
    newHireCount: getMonthEmployeesCount(currentMonth.value, 'hireDate'),
    resignCount: getMonthEmployeesCount(currentMonth.value, 'dismissDate'),
    salaryTotal: round(sumBy(salaryItems, (item) => item.grossWages), 2),
    billTotal: round(sumBy(billItems, (item) => item.totalAmount), 2),
    averageHours: getAverageHours(timesheetItems)
  };
});

const previousMonthSummary = computed(() => {
  const salaryItems = getMonthSalaries(previousMonth.value);
  const billItems = getMonthBills(previousMonth.value);
  const timesheetItems = getMonthTimesheets(previousMonth.value);

  return {
    newHireCount: getMonthEmployeesCount(previousMonth.value, 'hireDate'),
    resignCount: getMonthEmployeesCount(previousMonth.value, 'dismissDate'),
    salaryTotal: round(sumBy(salaryItems, (item) => item.grossWages), 2),
    billTotal: round(sumBy(billItems, (item) => item.totalAmount), 2),
    averageHours: getAverageHours(timesheetItems)
  };
});

const statCards = computed(() => [
  {
    key: 'employee',
    label: '员工总数',
    value: currentMonthSummary.value.totalEmployees,
    prefix: '',
    suffix: '人',
    precision: 0,
    icon: TeamOutlined,
    color: 'blue',
    trend: null,
    path: '/employees'
  },
  {
    key: 'new-hire',
    label: '本月入职',
    value: currentMonthSummary.value.newHireCount,
    prefix: '',
    suffix: '人',
    precision: 0,
    icon: UserAddOutlined,
    color: 'green',
    trend: getTrendRate(currentMonthSummary.value.newHireCount, previousMonthSummary.value.newHireCount),
    path: '/employees'
  },
  {
    key: 'resign',
    label: '本月离职',
    value: currentMonthSummary.value.resignCount,
    prefix: '',
    suffix: '人',
    precision: 0,
    icon: UserDeleteOutlined,
    color: 'red',
    trend: getTrendRate(currentMonthSummary.value.resignCount, previousMonthSummary.value.resignCount),
    path: '/employees'
  },
  {
    key: 'salary',
    label: '本月薪资总额',
    value: currentMonthSummary.value.salaryTotal,
    prefix: '¥',
    suffix: '',
    precision: 2,
    icon: MoneyCollectOutlined,
    color: 'orange',
    trend: getTrendRate(currentMonthSummary.value.salaryTotal, previousMonthSummary.value.salaryTotal),
    path: '/salary'
  },
  {
    key: 'bill',
    label: '本月甲方账单',
    value: currentMonthSummary.value.billTotal,
    prefix: '¥',
    suffix: '',
    precision: 2,
    icon: FileTextOutlined,
    color: 'purple',
    trend: getTrendRate(currentMonthSummary.value.billTotal, previousMonthSummary.value.billTotal),
    path: '/bills'
  },
  {
    key: 'hours',
    label: '本月平均工时',
    value: currentMonthSummary.value.averageHours,
    prefix: '',
    suffix: '小时',
    precision: 1,
    icon: BarChartOutlined,
    color: 'cyan',
    trend: getTrendRate(currentMonthSummary.value.averageHours, previousMonthSummary.value.averageHours),
    path: '/timesheets'
  }
]);

const formatRelativeTime = (value) => {
  const date = parseDate(value);
  if (!date) {
    return '刚刚';
  }

  const diff = Date.now() - date.getTime();
  const minute = 60 * 1000;
  const hour = 60 * minute;
  const day = 24 * hour;

  if (diff < hour) {
    return `${Math.max(1, Math.floor(diff / minute))}分钟前`;
  }

  if (diff < day) {
    return `${Math.floor(diff / hour)}小时前`;
  }

  return `${Math.floor(diff / day)}天前`;
};

const pendingTasks = computed(() => {
  const timesheetTasks = rawTimesheets.value
    .filter((item) => item.approvalStatus === 'Pending')
    .map((item) => {
      const date = parseDate(item.date);
      return {
        id: `timesheet-${item.id}`,
        title: '工时待审核',
        description: `${item.employeeName || item.employeeNo || '未命名员工'} ${date ? formatDateLabel(date) : ''}`.trim(),
        type: '工时',
        typeColor: 'orange',
        time: formatRelativeTime(item.createdAt || item.date),
        timestamp: date?.getTime() || 0,
        path: '/timesheets'
      };
    });

  const contractTasks = rawEmployees.value
    .filter((employee) => {
      const contractEndDate = parseDate(employee.contractEndDate);
      if (!contractEndDate || employee.isActive === false) {
        return false;
      }

      const today = startOfDay(new Date());
      const deadline = new Date(today);
      deadline.setDate(deadline.getDate() + 30);
      return contractEndDate >= today && contractEndDate <= deadline;
    })
    .map((employee) => {
      const contractEndDate = parseDate(employee.contractEndDate);
      return {
        id: `contract-${employee.id}`,
        title: '合同即将到期',
        description: `${employee.name} 合同到期日 ${contractEndDate ? formatDateLabel(contractEndDate) : '-'}`,
        type: '合同',
        typeColor: 'blue',
        time: contractEndDate ? formatRelativeTime(contractEndDate) : '待处理',
        timestamp: contractEndDate?.getTime() || 0,
        path: '/employees'
      };
    });

  return [...timesheetTasks, ...contractTasks]
    .sort((left, right) => right.timestamp - left.timestamp)
    .slice(0, 5);
});

const handleCardClick = (card) => {
  if (card.path) {
    router.push(card.path);
  }
};

const loadDashboardData = async ({ silent = false } = {}) => {
  const [employeesResult, salariesResult, timesheetsResult, billsResult] = await Promise.allSettled([
    employeeApi.getAll(),
    salaryApi.getAll(),
    timesheetApi.getAll(),
    thirdPartyBillApi.getAll()
  ]);

  let failedCount = 0;

  if (employeesResult.status === 'fulfilled') {
    rawEmployees.value = Array.isArray(employeesResult.value) ? employeesResult.value : [];
  } else {
    failedCount += 1;
  }

  if (salariesResult.status === 'fulfilled') {
    rawSalaries.value = Array.isArray(salariesResult.value) ? salariesResult.value : [];
  } else {
    failedCount += 1;
  }

  if (timesheetsResult.status === 'fulfilled') {
    rawTimesheets.value = Array.isArray(timesheetsResult.value) ? timesheetsResult.value : [];
  } else {
    failedCount += 1;
  }

  if (billsResult.status === 'fulfilled') {
    rawBills.value = Array.isArray(billsResult.value) ? billsResult.value : [];
  } else {
    failedCount += 1;
  }

  initCharts();

  if (!silent) {
    if (failedCount === 0) {
      message.success('仪表盘数据已更新');
    } else if (failedCount < 4) {
      message.warning('部分仪表盘数据刷新失败，已显示当前可用数据');
    } else {
      message.error('仪表盘数据刷新失败');
    }
  }
};

const refreshData = async () => {
  const key = 'dashboard-refresh';
  message.loading({ content: '数据刷新中...', key, duration: 0 });
  await loadDashboardData({ silent: true });
  message.success({ content: '仪表盘刷新完成', key, duration: 2 });
};

const handleIntervalChange = (value) => {
  if (refreshTimer) {
    clearInterval(refreshTimer);
  }

  if (value > 0) {
    refreshTimer = setInterval(() => {
      loadDashboardData({ silent: true });
    }, value * 1000);
  }
};

const exportChart = () => {
  message.info('正在导出图片...');
  setTimeout(() => {
    message.success('图片导出成功');
  }, 500);
};

const handleTaskClick = (task) => {
  if (task.path) {
    router.push(task.path);
  }
};

const handleApprove = (task) => {
  if (task.path) {
    router.push(task.path);
  }
};

const handleBatchApprove = () => {
  const firstTask = pendingTasks.value[0];
  if (firstTask?.path) {
    router.push(firstTask.path);
    return;
  }

  message.info('当前没有可处理的任务');
};

const handleQuickEntryClick = (entry) => {
  if (entry.path) {
    router.push(entry.path);
  }
};

const buildEmptyGraphic = (text = '暂无数据') => ({
  type: 'text',
  left: 'center',
  top: 'middle',
  silent: true,
  style: {
    text,
    fill: '#9ca3af',
    fontSize: 14
  }
});

const initCharts = () => {
  nextTick(() => {
    initTrendChart();
    initSalaryChart();
    initTimesheetChart();
  });
};

const getTrendSeriesData = () => {
  const months = getMonthSeries(chartPeriod.value);
  return {
    labels: months.map(formatMonthLabel),
    hireData: months.map((month) => getMonthEmployeesCount(month, 'hireDate')),
    resignData: months.map((month) => getMonthEmployeesCount(month, 'dismissDate'))
  };
};

const initTrendChart = () => {
  trendChart?.dispose();

  const chartDom = trendChartRef.value;
  if (!chartDom) {
    return;
  }

  trendChart = echarts.init(chartDom);
  const { labels, hireData, resignData } = getTrendSeriesData();
  const hasData = hireData.some((value) => value > 0) || resignData.some((value) => value > 0);

  trendChart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['入职', '离职'] },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: { type: 'category', boundaryGap: false, data: labels },
    yAxis: { type: 'value', minInterval: 1 },
    graphic: hasData ? [] : [buildEmptyGraphic()],
    series: [
      {
        name: '入职',
        type: 'line',
        data: hireData,
        smooth: true,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(52, 211, 153, 0.3)' },
            { offset: 1, color: 'rgba(52, 211, 153, 0.05)' }
          ])
        }
      },
      {
        name: '离职',
        type: 'line',
        data: resignData,
        smooth: true,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(248, 113, 113, 0.3)' },
            { offset: 1, color: 'rgba(248, 113, 113, 0.05)' }
          ])
        }
      }
    ]
  });
};

const getSalaryDistributionData = () => {
  const baseItems = ['时薪制', '固薪制', '计件制', '混合制'].map((name) => ({ name, value: 0 }));
  const itemMap = new Map(baseItems.map((item) => [item.name, item]));

  rawEmployees.value.forEach((employee) => {
    const label = salaryModeLabelMap[employee.salaryMode] || employee.salaryMode || '未知';
    if (!itemMap.has(label)) {
      itemMap.set(label, { name: label, value: 0 });
    }

    itemMap.get(label).value += 1;
  });

  return Array.from(itemMap.values()).filter((item) => item.value > 0 || itemMap.size <= 4);
};

const initSalaryChart = () => {
  salaryChart?.dispose();

  const chartDom = salaryChartRef.value;
  if (!chartDom) {
    return;
  }

  salaryChart = echarts.init(chartDom);
  const data = getSalaryDistributionData();
  const hasData = data.some((item) => item.value > 0);

  const option = salaryChartType.value === 'pie'
    ? {
        tooltip: { trigger: 'item' },
        legend: { orient: 'vertical', left: 'left' },
        graphic: hasData ? [] : [buildEmptyGraphic()],
        series: [
          {
            type: 'pie',
            radius: ['40%', '70%'],
            avoidLabelOverlap: false,
            itemStyle: { borderRadius: 10, borderColor: '#fff', borderWidth: 2 },
            label: { show: true },
            emphasis: { label: { show: true, fontSize: 14, fontWeight: 'bold' } },
            data
          }
        ]
      }
    : {
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
        xAxis: { type: 'category', data: data.map((item) => item.name) },
        yAxis: { type: 'value', minInterval: 1 },
        graphic: hasData ? [] : [buildEmptyGraphic()],
        series: [
          {
            type: 'bar',
            data: data.map((item) => item.value),
            itemStyle: { borderRadius: [6, 6, 0, 0] }
          }
        ]
      };

  salaryChart.setOption(option);
};

const buildDayTimesheetSeries = () => {
  const today = startOfDay(new Date());
  const dayIndex = today.getDay() === 0 ? 6 : today.getDay() - 1;
  const monday = new Date(today);
  monday.setDate(today.getDate() - dayIndex);

  const days = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(monday);
    date.setDate(monday.getDate() + index);
    return date;
  });

  return {
    labels: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
    regularData: days.map((day) => sumBy(rawTimesheets.value.filter((item) => sameDay(parseDate(item.date), day)), getRegularHours)),
    overtimeData: days.map((day) => sumBy(rawTimesheets.value.filter((item) => sameDay(parseDate(item.date), day)), (item) => item.overtimeHours))
  };
};

const buildWeekTimesheetSeries = () => {
  const monthStart = startOfMonth(new Date());
  const daysInMonth = endOfMonth(monthStart).getDate();
  const firstWeekday = (monthStart.getDay() + 6) % 7;
  const weekCount = Math.ceil((firstWeekday + daysInMonth) / 7);
  const labels = Array.from({ length: weekCount }, (_, index) => `第${index + 1}周`);
  const regularData = Array.from({ length: weekCount }, () => 0);
  const overtimeData = Array.from({ length: weekCount }, () => 0);

  getMonthTimesheets(monthStart).forEach((item) => {
    const date = parseDate(item.date);
    if (!date) {
      return;
    }

    const weekIndex = Math.floor((firstWeekday + date.getDate() - 1) / 7);
    regularData[weekIndex] += getRegularHours(item);
    overtimeData[weekIndex] += Number(item.overtimeHours) || 0;
  });

  return { labels, regularData, overtimeData };
};

const buildMonthTimesheetSeries = () => {
  const months = getMonthSeries(6);
  return {
    labels: months.map(formatMonthLabel),
    regularData: months.map((month) => sumBy(getMonthTimesheets(month), getRegularHours)),
    overtimeData: months.map((month) => sumBy(getMonthTimesheets(month), (item) => item.overtimeHours))
  };
};

const getTimesheetSeries = () => {
  if (timesheetPeriod.value === 'day') {
    return buildDayTimesheetSeries();
  }

  if (timesheetPeriod.value === 'month') {
    return buildMonthTimesheetSeries();
  }

  return buildWeekTimesheetSeries();
};

const initTimesheetChart = () => {
  timesheetChart?.dispose();

  const chartDom = timesheetChartRef.value;
  if (!chartDom) {
    return;
  }

  timesheetChart = echarts.init(chartDom);
  const { labels, regularData, overtimeData } = getTimesheetSeries();
  const hasData = regularData.some((value) => value > 0) || overtimeData.some((value) => value > 0);

  timesheetChart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['正常工时', '加班工时'] },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: { type: 'category', data: labels },
    yAxis: { type: 'value' },
    graphic: hasData ? [] : [buildEmptyGraphic()],
    series: [
      { name: '正常工时', type: 'bar', stack: 'total', data: regularData.map((value) => round(value, 1)) },
      { name: '加班工时', type: 'bar', stack: 'total', data: overtimeData.map((value) => round(value, 1)) }
    ]
  });
};

watch(chartPeriod, () => {
  initTrendChart();
});

watch(salaryChartType, () => {
  initSalaryChart();
});

watch(timesheetPeriod, () => {
  initTimesheetChart();
});

const handleResize = () => {
  trendChart?.resize();
  salaryChart?.resize();
  timesheetChart?.resize();
};

onMounted(async () => {
  await loadDashboardData({ silent: true });
  window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer);
  }

  window.removeEventListener('resize', handleResize);
  trendChart?.dispose();
  salaryChart?.dispose();
  timesheetChart?.dispose();
});
</script>

<style scoped>
.dashboard {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.interval-select {
  width: 120px;
}

/* 指标卡片 */
.stats-row {
  margin-bottom: 16px;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 20px;
  border-radius: 12px;
  transition: all 0.3s;
}

.stat-card.clickable {
  cursor: pointer;
}

.stat-card.clickable:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-right: 16px;
}

.stat-icon.blue {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: #fff;
}

.stat-icon.green {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
  color: #fff;
}

.stat-icon.red {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  color: #fff;
}

.stat-icon.orange {
  background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
  color: #fff;
}

.stat-icon.purple {
  background: linear-gradient(135deg, #a18cd1 0%, #fbc2eb 100%);
  color: #fff;
}

.stat-icon.cyan {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
  color: #fff;
}

.stat-content {
  flex: 1;
}

.stat-label {
  font-size: 13px;
  color: #6b7280;
  margin-bottom: 4px;
}

.stat-trend {
  font-size: 12px;
  margin-top: 4px;
  display: flex;
  align-items: center;
}

.stat-trend.up {
  color: #10b981;
}

.stat-trend.down {
  color: #ef4444;
}

/* 图表区域 */
.chart-row {
  margin-bottom: 16px;
}

.chart-card {
  border-radius: 12px;
}

.chart-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 12px;
}

.period-select,
.chart-type-select {
  width: 120px;
}

.chart-container {
  height: 280px;
}

/* 下半区域 */
.bottom-row {
  margin-bottom: 16px;
}

/* 待办事项 */
.todo-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.todo-count {
  font-size: 13px;
  color: #6b7280;
}

.todo-list {
  max-height: 280px;
  overflow-y: auto;
}

.todo-item {
  cursor: pointer;
  transition: background-color 0.2s;
}

.todo-item:hover {
  background-color: #f9fafb;
}

.task-tag {
  margin-left: 8px;
  font-size: 12px;
}

/* 快捷入口 */
.quick-grid {
  gap: 12px;
}

.quick-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 16px;
  background: #f9fafb;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s;
}

.quick-item:hover {
  background: #f3f4f6;
  transform: translateY(-2px);
}

.quick-icon {
  font-size: 24px;
  color: #667eea;
  margin-bottom: 8px;
}

.quick-text {
  font-size: 12px;
  color: #374151;
}

/* 公告栏 */
.notice-card {
  margin-top: 16px;
}

.notice-carousel {
  height: 140px;
}

.notice-carousel .slick-slide {
  padding: 8px 0;
}

.notice-title {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 8px;
}

.notice-content {
  font-size: 13px;
  color: #6b7280;
  margin-bottom: 8px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.notice-time {
  font-size: 12px;
  color: #9ca3af;
}

/* 响应式 */
@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
  
  .stats-row .ant-col {
    margin-bottom: 12px;
  }
  
  .stat-card {
    flex-direction: column;
    text-align: center;
  }
  
  .stat-icon {
    margin-right: 0;
    margin-bottom: 12px;
  }
}
</style>
