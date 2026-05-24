<template>
  <div class="timesheet-import">
    <a-tabs v-model:activeKey="activeTab">
      <a-tab-pane key="import" tab="工时导入">
        <a-card title="工时批量导入">
          <a-space direction="vertical" style="width: 100%;">
            <a-alert message="导入说明" type="info" show-icon>
              <template #description>
                请先下载模板，按模板列名填写后上传。系统会先在前端校验，再调用后端导入接口。
              </template>
            </a-alert>

            <a-space>
              <a-button type="primary" @click="downloadTemplate">
                <DownloadOutlined /> 下载模板
              </a-button>
              <a-button @click="downloadSample">
                <FileExcelOutlined /> 下载示例
              </a-button>
            </a-space>

            <div class="upload-area" @click="triggerFileInput" @drop.prevent="handleDrop" @dragover.prevent>
              <input
                ref="fileInput"
                type="file"
                accept=".xlsx,.xls"
                class="file-input"
                @change="handleFileSelect"
              />
              <UploadOutlined class="upload-icon" />
              <p>点击或拖拽上传 Excel 文件</p>
              <p class="upload-hint">支持 `.xlsx`、`.xls`，文件大小不超过 20MB</p>
            </div>

            <div v-if="uploadedFile" class="file-info">
              <a-tag color="blue">{{ uploadedFile.name }}</a-tag>
              <a-space>
                <span>共 {{ importPreview.length }} 行</span>
                <a-button type="text" danger @click="clearUpload">移除</a-button>
              </a-space>
            </div>

            <div v-if="importPreview.length" class="import-preview">
              <h4>数据预览</h4>
              <a-table
                :columns="importColumns"
                :data-source="importPreview"
                :pagination="{ pageSize: 10, showSizeChanger: true }"
                size="small"
                row-key="rowNum"
                :scroll="{ x: 920 }"
              >
                <template #bodyCell="{ column, record }">
                  <span :class="{ 'error-cell': record[`${column.dataIndex}Error`] }">
                    {{ record[column.dataIndex] || '-' }}
                  </span>
                </template>
              </a-table>
              <p v-if="errorCount > 0" class="error-count">发现 {{ errorCount }} 条无效数据，这些行不会被提交到后端。</p>
              <p v-if="successCount > 0" class="success-count">可导入 {{ successCount }} 条数据。</p>
            </div>

            <div v-if="uploadedFile" class="import-actions">
              <a-space>
                <a-button
                  type="primary"
                  :loading="uploading"
                  :disabled="validPayload.length === 0"
                  @click="handleImport"
                >
                  <UploadOutlined /> 确认导入
                </a-button>
                <a-button :disabled="errorRows.length === 0" @click="downloadErrorData">
                  <DownloadOutlined /> 导出错误行
                </a-button>
                <a-button @click="clearUpload">清空</a-button>
              </a-space>
            </div>
          </a-space>
        </a-card>
      </a-tab-pane>

      <a-tab-pane key="history" tab="最近导入结果">
        <a-card title="最近导入结果">
          <a-table :columns="historyColumns" :data-source="importHistory" row-key="id" :pagination="{ pageSize: 10 }">
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'status'">
                <a-tag :color="record.failureCount > 0 ? 'orange' : 'green'">
                  {{ record.failureCount > 0 ? '部分成功' : '成功' }}
                </a-tag>
              </template>
            </template>
          </a-table>
        </a-card>
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import dayjs from 'dayjs'
import { message } from 'ant-design-vue'
import { DownloadOutlined, FileExcelOutlined, UploadOutlined } from '@ant-design/icons-vue'
import * as XLSX from 'xlsx'
import { timesheetApi } from '../../api'
import { useUserStore } from '../../store/user'

const HISTORY_STORAGE_KEY = 'hrms-timesheet-import-history'

const userStore = useUserStore()
const activeTab = ref('import')
const fileInput = ref(null)
const uploadedFile = ref(null)
const importPreview = ref([])
const uploading = ref(false)
const importHistory = ref(loadHistory())

const importColumns = [
  { title: '行号', dataIndex: 'rowNum', width: 80 },
  { title: '员工工号', dataIndex: 'employeeNo', width: 120 },
  { title: '工作日期', dataIndex: 'workDate', width: 120 },
  { title: '实际班组编码', dataIndex: 'actualOrgUnitCode', width: 150 },
  { title: '工作时长', dataIndex: 'workingHours', width: 100 },
  { title: '班次类型', dataIndex: 'shiftTypeText', width: 120 },
  { title: '备注', dataIndex: 'remark', width: 150 },
  { title: '错误信息', dataIndex: 'error', width: 220 }
]

const historyColumns = [
  { title: '文件名', dataIndex: 'fileName', key: 'fileName' },
  { title: '导入时间', dataIndex: 'importTime', key: 'importTime', width: 180 },
  { title: '导入人', dataIndex: 'importBy', key: 'importBy', width: 120 },
  { title: '总条数', dataIndex: 'totalCount', key: 'totalCount', width: 100 },
  { title: '成功数', dataIndex: 'successCount', key: 'successCount', width: 100 },
  { title: '失败数', dataIndex: 'failureCount', key: 'failureCount', width: 100 },
  { title: '状态', key: 'status', width: 100 }
]

const errorRows = computed(() => importPreview.value.filter(item => item.error))
const errorCount = computed(() => errorRows.value.length)
const successCount = computed(() => importPreview.value.length - errorCount.value)
const validPayload = computed(() => {
  return importPreview.value
    .filter(item => !item.error)
    .map(item => ({
      employeeNo: item.employeeNo,
      date: item.workDate,
      actualOrgUnitCode: item.actualOrgUnitCode,
      workingHours: Number(item.workingHours),
      shiftType: item.shiftType,
      remark: item.remark || null
    }))
})

function loadHistory() {
  try {
    const raw = localStorage.getItem(HISTORY_STORAGE_KEY)
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

function saveHistory(record) {
  importHistory.value = [record, ...importHistory.value].slice(0, 50)
  localStorage.setItem(HISTORY_STORAGE_KEY, JSON.stringify(importHistory.value))
}

const triggerFileInput = () => {
  fileInput.value?.click()
}

const handleFileSelect = (event) => {
  const file = event.target.files?.[0]
  if (file) {
    handleFile(file)
  }
}

const handleDrop = (event) => {
  const file = event.dataTransfer?.files?.[0]
  if (file) {
    handleFile(file)
  }
}

const handleFile = async (file) => {
  if (!file.name.match(/\.(xlsx|xls)$/i)) {
    message.error('请选择 Excel 文件')
    return
  }
  if (file.size > 20 * 1024 * 1024) {
    message.error('文件大小不能超过 20MB')
    return
  }

  uploadedFile.value = file
  importPreview.value = await parseWorkbook(file)
}

const readFileAsArrayBuffer = (file) => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (event) => resolve(event.target?.result)
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}

const getCellValue = (row, ...keys) => {
  for (const key of keys) {
    if (row[key] !== undefined && row[key] !== null && `${row[key]}`.trim() !== '') {
      return `${row[key]}`.trim()
    }
  }
  return ''
}

const parseShiftType = (value) => {
  const normalized = `${value || ''}`.trim()
  const map = {
    工作日: 0,
    周末: 1,
    节假日: 2,
    Weekday: 0,
    Weekend: 1,
    Holiday: 2,
    '0': 0,
    '1': 1,
    '2': 2
  }
  if (normalized in map) {
    return map[normalized]
  }
  return 0
}

const getShiftTypeText = (value) => {
  const map = {
    0: '工作日',
    1: '周末',
    2: '节假日'
  }
  return map[value] || '工作日'
}

const parseWorkbook = async (file) => {
  const buffer = await readFileAsArrayBuffer(file)
  const workbook = XLSX.read(buffer, { type: 'array' })
  const sheetName = workbook.SheetNames[0]
  const rows = XLSX.utils.sheet_to_json(workbook.Sheets[sheetName], { defval: '' })

  return rows.map((row, index) => {
    const employeeNo = getCellValue(row, '员工工号', 'employeeNo', 'EmployeeNo')
    const workDate = getCellValue(row, '工作日期', 'date', 'Date')
    const actualOrgUnitCode = getCellValue(row, '实际班组编码', 'actualOrgUnitCode', 'ActualOrgUnitCode')
    const workingHours = getCellValue(row, '工作时长', 'workingHours', 'WorkingHours')
    const shiftTypeRaw = getCellValue(row, '班次类型', 'shiftType', 'ShiftType')
    const remark = getCellValue(row, '备注', 'remark', 'Remark')
    const shiftType = parseShiftType(shiftTypeRaw)

    const errors = []
    if (!employeeNo) {
      errors.push('员工工号不能为空')
    }
    if (!workDate || !dayjs(workDate).isValid()) {
      errors.push('工作日期格式不正确')
    }
    if (!actualOrgUnitCode) {
      errors.push('实际班组编码不能为空')
    }
    if (!workingHours || Number(workingHours) <= 0) {
      errors.push('工作时长必须大于 0')
    }

    return {
      rowNum: index + 2,
      employeeNo,
      workDate: workDate ? dayjs(workDate).format('YYYY-MM-DD') : '',
      workDateError: !workDate || !dayjs(workDate).isValid(),
      actualOrgUnitCode,
      actualOrgUnitCodeError: !actualOrgUnitCode,
      workingHours,
      workingHoursError: !workingHours || Number(workingHours) <= 0,
      shiftType,
      shiftTypeText: getShiftTypeText(shiftType),
      remark,
      employeeNoError: !employeeNo,
      error: errors.join('；')
    }
  })
}

const clearUpload = () => {
  uploadedFile.value = null
  importPreview.value = []
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

const buildTemplateRows = () => [
  {
    员工工号: '10001',
    工作日期: dayjs().format('YYYY-MM-DD'),
    实际班组编码: 'TEAM001',
    工作时长: 8,
    班次类型: '工作日',
    备注: ''
  }
]

const exportWorkbook = (fileName, rows) => {
  const worksheet = XLSX.utils.json_to_sheet(rows)
  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1')
  XLSX.writeFile(workbook, fileName)
}

const downloadTemplate = () => {
  exportWorkbook('工时导入模板.xlsx', buildTemplateRows().map(item => ({
    ...item,
    备注: '列名请勿修改，可删除示例内容后填写正式数据'
  })))
}

const downloadSample = () => {
  exportWorkbook('工时导入示例.xlsx', [
    { 员工工号: '10001', 工作日期: '2026-05-01', 实际班组编码: 'TEAM001', 工作时长: 8, 班次类型: '工作日', 备注: '白班' },
    { 员工工号: '10002', 工作日期: '2026-05-02', 实际班组编码: 'TEAM002', 工作时长: 10, 班次类型: '周末', 备注: '加班' }
  ])
}

const handleImport = async () => {
  if (!validPayload.value.length) {
    message.error('没有可导入的数据')
    return
  }

  uploading.value = true
  try {
    const result = await timesheetApi.importBatch(validPayload.value)
    const importedCount = Number(result?.count || validPayload.value.length)
    saveHistory({
      id: `${Date.now()}`,
      fileName: uploadedFile.value?.name || '未命名文件',
      importTime: dayjs().format('YYYY-MM-DD HH:mm:ss'),
      importBy: userStore.userInfo?.name || '当前用户',
      totalCount: importPreview.value.length,
      successCount: importedCount,
      failureCount: importPreview.value.length - importedCount
    })
    message.success(`导入成功，共导入 ${importedCount} 条`)
    clearUpload()
    activeTab.value = 'history'
  } catch (error) {
    message.error(error?.response?.data?.message || '导入失败')
  } finally {
    uploading.value = false
  }
}

const downloadErrorData = () => {
  if (!errorRows.value.length) {
    return
  }

  exportWorkbook('工时导入错误数据.xlsx', errorRows.value.map(item => ({
    行号: item.rowNum,
    员工工号: item.employeeNo,
    工作日期: item.workDate,
    实际班组编码: item.actualOrgUnitCode,
    工作时长: item.workingHours,
    班次类型: item.shiftTypeText,
    备注: item.remark,
    错误信息: item.error
  })))
}
</script>

<style scoped>
.timesheet-import {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.upload-area {
  border: 2px dashed #d9d9d9;
  border-radius: 8px;
  padding: 40px;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
}

.upload-area:hover {
  border-color: #4096ff;
  background: #fafafa;
}

.upload-icon {
  font-size: 48px;
  color: #999;
  margin-bottom: 12px;
}

.upload-hint {
  font-size: 12px;
  color: #999;
  margin-top: 8px;
}

.file-input {
  display: none;
}

.file-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px;
  background: #fafafa;
  border-radius: 6px;
}

.import-preview {
  margin-top: 16px;
}

.error-cell {
  color: #ff4d4f;
}

.error-count {
  margin-top: 12px;
  color: #ff4d4f;
}

.success-count {
  margin-top: 8px;
  color: #52c41a;
}

.import-actions {
  margin-top: 16px;
}
</style>
