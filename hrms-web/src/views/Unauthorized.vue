<template>
  <div class="unauthorized-page">
    <a-result
      status="403"
      title="403"
      sub-title="当前账号没有访问该页面的权限"
    >
      <template #extra>
        <a-button type="primary" @click="$router.push('/dashboard')">返回首页</a-button>
      </template>
    </a-result>
  </div>
</template>

<style scoped>
.unauthorized-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 120px);
  background: #fff;
  border-radius: 12px;
}
</style>
