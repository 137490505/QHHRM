import { defineStore } from 'pinia'
import { ref } from 'vue'
import { orgUnitApi } from '../api'

export const useOrgUnitStore = defineStore('orgUnit', () => {
  const treeData = ref([])
  const listData = ref([])
  const lastUpdateTime = ref(null)

  const loadTreeData = async () => {
    try {
      const response = await orgUnitApi.getTree()
      treeData.value = response || []
      lastUpdateTime.value = Date.now()
      return treeData.value
    } catch (error) {
      console.error('Failed to load org unit tree:', error)
      return []
    }
  }

  const loadListData = async () => {
    try {
      const response = await orgUnitApi.getAll()
      listData.value = response || []
      lastUpdateTime.value = Date.now()
      return listData.value
    } catch (error) {
      console.error('Failed to load org unit list:', error)
      return []
    }
  }

  const refreshData = async () => {
    await Promise.all([loadTreeData(), loadListData()])
  }

  const addOrgUnit = async (data) => {
    const response = await orgUnitApi.create(data)
    await refreshData()
    return response
  }

  const updateOrgUnit = async (data) => {
    const response = await orgUnitApi.update(data)
    await refreshData()
    return response
  }

  const deleteOrgUnit = async (id) => {
    const response = await orgUnitApi.delete(id)
    await refreshData()
    return response
  }

  return {
    treeData,
    listData,
    lastUpdateTime,
    loadTreeData,
    loadListData,
    refreshData,
    addOrgUnit,
    updateOrgUnit,
    deleteOrgUnit
  }
})
