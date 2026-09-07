<script setup lang="ts">
// Code logic Bản Đồ Map của Huy viết tại đây
</script>

<template>
  <div class="demo-container">
    <div class="demo-card">
      <span class="badge badge-huy">Phụ trách: Huy</span>
      <h2>Demo Trang Map (Bản Đồ & Định Vị)</h2>
      <p>Khu vực phát triển Bản Đồ định vị, lọc các cửa hàng trong bán kính 10km (Luồng 1), tính khoảng cách &le; 3km đủ điều kiện giao Hỏa Tốc và vẽ đường đi giao hàng (Luồng 4).</p>
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
</style>
