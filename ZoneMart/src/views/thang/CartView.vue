<script setup lang="ts">
// Code logic Giỏ Hàng của Thắng viết tại đây
</script>

<template>
  <div class="demo-container">
    <div class="demo-card">
      <span class="badge badge-thang">Phụ trách: Thắng</span>
      <h2>Demo Trang Giỏ Hàng</h2>
      <p>Khu vực phát triển Giỏ Hàng (Gom món theo Store, tăng/giảm số lượng, tạm tính phí ship theo Luồng 3).</p>
    </div>
  </div>
</template>

<style scoped>
.demo-container { max-width: 900px; margin: 60px auto; padding: 0 20px; }
.demo-card { background: #fff; border-radius: 14px; border: 2px dashed #cbd5e1; padding: 50px 30px; text-align: center; }
.badge { font-size: 13px; font-weight: 700; padding: 6px 14px; border-radius: 20px; display: inline-block; }
.badge-thang { background: #dbeafe; color: #1e40af; }
h2 { margin: 18px 0 8px 0; font-size: 24px; color: #0f172a; }
p { margin: 0; color: #64748b; font-size: 15px; }
</style>
