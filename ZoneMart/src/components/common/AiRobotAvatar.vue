<template>
  <div class="ai-robot-avatar" :class="customClass">
    <svg
      viewBox="0 0 100 100"
      width="100%"
      height="100%"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      class="robot-line-svg"
      aria-label="AI Tư Vấn"
    >
      <!-- 1. Nền trắng tròn tinh khôi (Hoàn toàn không có viền đen để không bị viền đen lồng trong vòng cam) -->
      <circle
        cx="50"
        cy="50"
        r="50"
        fill="#FFFFFF"
      />

      <!-- 2. Vòng tai nghe tư vấn (Headset Arc - Cân đối chính giữa) -->
      <path
        d="M 26 55 C 26 31 74 31 74 55"
        stroke="#1E293B"
        stroke-width="3"
        stroke-linecap="round"
      />

      <!-- 3. Hai ốp tai nghe 2 bên (Ear Cushions) -->
      <rect
        x="22"
        y="47"
        width="6"
        height="16"
        rx="3"
        fill="#FFFFFF"
        stroke="#1E293B"
        stroke-width="2.4"
      />
      <rect
        x="72"
        y="47"
        width="6"
        height="16"
        rx="3"
        fill="#FFFFFF"
        stroke="#1E293B"
        stroke-width="2.4"
      />

      <!-- 4. Ăng-ten thu phát tín hiệu AI tối giản -->
      <line
        x1="50"
        y1="37"
        x2="50"
        y2="27"
        stroke="#1E293B"
        stroke-width="2.2"
        stroke-linecap="round"
      />
      <circle
        cx="50"
        cy="25"
        r="2.5"
        fill="#FFFFFF"
        stroke="#1E293B"
        stroke-width="2"
      />

      <!-- 5. Đầu Robot AI (Nét bo tròn thân thiện, căn giữa hoàn hảo) -->
      <rect
        x="28"
        y="37"
        width="44"
        height="36"
        rx="14"
        fill="#FFFFFF"
        stroke="#1E293B"
        stroke-width="2.6"
        stroke-linejoin="round"
      />

      <!-- 6. Mắt cười nét vẽ thân thiện (^ ^) -->
      <path
        d="M 38 54 C 40 50 43.5 50 45.5 54"
        stroke="#1E293B"
        stroke-width="2.5"
        stroke-linecap="round"
      />
      <path
        d="M 54.5 54 C 56.5 50 60 50 62 54"
        stroke="#1E293B"
        stroke-width="2.5"
        stroke-linecap="round"
      />

      <!-- 7. Miệng cười mỉm giải đáp -->
      <path
        d="M 47 60.5 C 48.5 63 51.5 63 53 60.5"
        stroke="#1E293B"
        stroke-width="2"
        stroke-linecap="round"
      />

      <!-- 8. Cần micro tư vấn (Biểu tượng rõ ràng của tư vấn viên giải đáp) -->
      <path
        d="M 25 61 C 24 72 36 75 44 72"
        stroke="#1E293B"
        stroke-width="2.2"
        stroke-linecap="round"
      />
      <!-- Đầu micro -->
      <circle
        cx="45"
        cy="71.5"
        r="2.5"
        fill="#1E293B"
      />

      <!-- 9. Ngôi sao AI nét vẽ tối giản (✨) -->
      <path
        d="M 77 21 L 77 29 M 73 25 L 81 25"
        stroke="#1E293B"
        stroke-width="2"
        stroke-linecap="round"
      />
      <circle
        cx="82.5"
        cy="31"
        r="1"
        fill="#1E293B"
      />
    </svg>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  customClass?: string;
}>();
</script>

<style scoped>
.ai-robot-avatar {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  overflow: hidden;
  user-select: none;
  background: #FFFFFF;
  box-sizing: border-box;
}

.robot-line-svg {
  width: 100%;
  height: 100%;
  display: block;
}
</style>
