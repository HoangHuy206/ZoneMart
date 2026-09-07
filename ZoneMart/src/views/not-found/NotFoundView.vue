<script setup lang="ts">
import { useRouter } from "vue-router";

const router = useRouter();

const goHome = () => {
  router.push("/");
};
</script>

<template>
  <div class="demo-container">
    <div class="demo-card">
      <span class="badge badge-huy">Phụ trách: Huy</span>
      <h2>Demo Trang 404 (Không Tìm Thấy Trang)</h2>
      <p>Trang thông báo lỗi 404 thân thiện khi người dùng truy cập vào đường dẫn không tồn tại trên hệ thống ZoneMart.</p>
      <div style="margin-top: 20px;">
        <button class="btn btn-primary" @click="goHome">⬅ Quay Về Trang Chủ</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo-container { max-width: 900px; margin: 60px auto; padding: 0 20px; }
.demo-card { background: #fff; border-radius: 14px; border: 2px dashed #cbd5e1; padding: 50px 30px; text-align: center; }
.badge { font-size: 13px; font-weight: 700; padding: 6px 14px; border-radius: 20px; display: inline-block; }
.badge-huy { background: #dcfce7; color: #15803d; }
h2 { margin: 18px 0 8px 0; font-size: 24px; color: #0f172a; }
p { margin: 0; color: #64748b; font-size: 15px; }
.btn {
  border: none;
  cursor: pointer;
  padding: 10px 20px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 14px;
  background: #2563eb;
  color: #fff;
}
.btn:hover { background: #1d4ed8; }
</style>
