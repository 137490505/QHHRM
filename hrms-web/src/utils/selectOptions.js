import { settingsApi } from '../api'
import { configTable } from '../config/configTable'

const OPTION_VALUE_TYPES = {
  orgLevel: 'number',
  employeeType: 'number',
  salaryMode: 'number',
  contractType: 'number',
  employeeTag: 'string'
}

const cloneOptions = (options = []) => options.map(option => ({ ...option }))
const isEmptyValue = (value) => value === null || value === undefined || `${value}`.trim() === ''
const sortSelectOptions = (options = []) => {
  return [...options].sort((a, b) => {
    const left = Number(a.sortOrder ?? Number.MAX_SAFE_INTEGER)
    const right = Number(b.sortOrder ?? Number.MAX_SAFE_INTEGER)
    if (left !== right) {
      return left - right
    }

    return `${a.label ?? ''}`.localeCompare(`${b.label ?? ''}`, 'zh-CN')
  })
}

export const getSelectOptionValueType = (category) => OPTION_VALUE_TYPES[category] || 'string'

export const getDefaultSelectOptions = (category) => {
  return cloneOptions(configTable.selectOptions[category] || []).map((option, index) => ({
    ...option,
    sortOrder: Number(option.sortOrder) > 0 ? Number(option.sortOrder) : index + 1
  }))
}

export const normalizeSelectOptions = (category, options = []) => {
  const defaultOptions = getDefaultSelectOptions(category)
  const defaultByLabel = new Map(defaultOptions.map(option => [option.label, option]))

  const normalizedOptions = options
    .map((option, index) => {
      const rawValue = option?.value ?? option?.Value
      const label = (option?.label ?? option?.Label ?? '').trim()
      const labelEn = (option?.labelEn ?? option?.LabelEn ?? '').trim() || null
      const rawSortOrder = option?.sortOrder ?? option?.SortOrder
      let value = rawValue
      let sortOrder = Number(rawSortOrder)

      if (isEmptyValue(value) && label && defaultByLabel.has(label)) {
        value = defaultByLabel.get(label).value
        sortOrder = Number(defaultByLabel.get(label).sortOrder)
      }

      if (isEmptyValue(value) && getSelectOptionValueType(category) === 'string' && label) {
        value = label
      }

      if (isEmptyValue(value) && defaultOptions[index]) {
        value = defaultOptions[index].value
      }

      if ((!Number.isFinite(sortOrder) || sortOrder <= 0) && defaultOptions[index]) {
        sortOrder = Number(defaultOptions[index].sortOrder)
      }

      if (!Number.isFinite(sortOrder) || sortOrder <= 0) {
        sortOrder = index + 1
      }

      if (!label || isEmptyValue(value)) {
        return null
      }

      return {
        value,
        label,
        labelEn,
        sortOrder
      }
    })
    .filter(Boolean)

  return sortSelectOptions(normalizedOptions)
}

export const loadSelectOptions = async (category) => {
  try {
    const options = await settingsApi.getSelectOptions(category)
    if (Array.isArray(options)) {
      return normalizeSelectOptions(category, options)
    }
  } catch (error) {
  }

  return getDefaultSelectOptions(category)
}

export const buildOptionLabelMap = (options = []) => {
  return options.reduce((result, option) => {
    result[String(option.value)] = option.label
    return result
  }, {})
}
