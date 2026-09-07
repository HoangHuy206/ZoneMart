<script setup lang="ts">
import { useRouter } from "vue-router";

const props = defineProps<{
  title: string;
  assignee: string;
  description: string;
}>();

const router = useRouter();
</script>

<template>
  <div class="placeholder-container">
    <div class="assignee-tag">Phụ trách: {{ props.assignee }}</div>
    <h2>{{ props.title }}</h2>
    <p>{{ props.description }}</p>
    <div class="status">🚧 Đang được phát triển theo phân công nhóm ZoneMart</div>
    <button class="back-btn" @click="router.push('/products')">Xem Task của Huy (Sản phẩm, Map, Profile)</button>
  </div>
</template>

<style scoped>
.placeholder-container {
  max-width: 650px;
  margin: 50px auto;
  padding: 30px;
  text-align: center;
  background: #fff;
  border-radius: 14px;
  border: 1px dashed #cbd5e1;
}
.assignee-tag {
  display: inline-block;
  background: #f1f5f9;
  color: #475569;
  font-weight: 700;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 13px;
  margin-bottom: 12px;
}
h2 { margin: 0 0 10px 0; color: #1e293b; }
p { color: #64748b; font-size: 14px; margin-bottom: 20px; }
.status { color: #d97706; font-weight: 600; font-size: 14px; margin-bottom: 20px; }
.back-btn {
  background: #2563eb;
  color: #fff;
  border: none;
  padding: 10px 18px;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
}
</style>
