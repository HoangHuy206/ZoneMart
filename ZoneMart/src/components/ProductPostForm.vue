<template>
  <div class="product-post-container">
    <div class="form-card">
      <div class="form-header">
        <h2 class="title">Đăng Sản Phẩm Mới</h2>
        <p class="subtitle">Hệ thống AI ZoneMart sẽ hỗ trợ kiểm tra hình ảnh và tên sản phẩm theo thời gian thực.</p>
      </div>

      <form @submit.prevent="handleFormSubmit" class="form-body">
        <!-- 1. Tên sản phẩm -->
        <div class="form-group">
          <label for="productName" class="form-label">
            Tên sản phẩm <span class="required">*</span>
          </label>
          <input
            id="productName"
            v-model.trim="form.productName"
            type="text"
            placeholder="VD: Rau muống sạch Ba Vì, Xoài cát Chu..."
            class="form-input"
            :disabled="isLoading"
            required
          />
        </div>

        <!-- 2. Danh mục & Giá bán -->
        <div class="form-row">
          <div class="form-group flex-1">
            <label for="category" class="form-label">Danh mục</label>
            <select id="category" v-model="form.category" class="form-input" :disabled="isLoading">
              <option value="Rau củ quả">Rau củ quả</option>
              <option value="Trái cây tươi">Trái cây tươi</option>
              <option value="Thịt cá tươi">Thịt cá tươi</option>
              <option value="Thực phẩm bổ dưỡng">Thực phẩm bổ dưỡng</option>
              <option value="Gia vị - Đồ khô">Gia vị - Đồ khô</option>
            </select>
          </div>

          <div class="form-group flex-1">
            <label for="price" class="form-label">Giá bán (VNĐ) <span class="required">*</span></label>
            <input
              id="price"
              v-model.number="form.price"
              type="number"
              min="1000"
              step="1000"
              placeholder="VD: 25000"
              class="form-input"
              :disabled="isLoading"
              required
            />
          </div>
        </div>

        <!-- 3. Tải lên hình ảnh sản phẩm -->
        <div class="form-group">
          <label class="form-label">Hình ảnh sản phẩm <span class="required">*</span></label>
          
          <div 
            class="image-upload-zone"
            :class="{ 'has-image': !!imagePreview, 'drag-over': isDragging }"
            @dragover.prevent="isDragging = true"
            @dragleave.prevent="isDragging = false"
            @drop.prevent="handleFileDrop"
            @click="triggerFileInput"
          >
            <input
              ref="fileInputRef"
              type="file"
              accept="image/png, image/jpeg, image/webp"
              class="hidden-input"
              @change="handleFileChange"
            />

            <!-- Chưa chọn ảnh -->
            <div v-if="!imagePreview" class="upload-placeholder">
              <div class="upload-icon">
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                  <circle cx="8.5" cy="8.5" r="1.5"/>
                  <polyline points="21 15 16 10 5 21"/>
                </svg>
              </div>
              <p class="upload-text">Nhấp để tải ảnh lên hoặc kéo thả vào đây</p>
              <span class="upload-hint">PNG, JPG, WEBP tối đa 5MB</span>
            </div>

            <!-- Đã chọn ảnh: Hiển thị Preview -->
            <div v-else class="preview-container" @click.stop>
              <img :src="imagePreview" alt="Ảnh sản phẩm preview" class="preview-img" />
              <button 
                type="button" 
                class="remove-image-btn" 
                title="Chọn ảnh khác"
                @click="clearImage"
                :disabled="isLoading"
              >
                ✕
              </button>
            </div>
          </div>
        </div>

        <!-- 4. Nút bấm submit / kiểm duyệt -->
        <div class="form-actions">
          <button 
            type="submit" 
            class="submit-btn" 
            :disabled="isLoading || !form.productName || !imageFile"
          >
            <span v-if="isLoading" class="spinner"></span>
            <span>{{ loadingText }}</span>
          </button>
        </div>
      </form>
    </div>

    <!-- MODAL CẢNH BÁO MÀU VÀNG KHI PHÁT HIỆN LỆCH ẢNH HOẶC TỪ NGỮ -->
    <Teleport to="body">
      <div v-if="showWarningModal" class="modal-backdrop" @click.self="closeModal">
        <div class="warning-modal">
          <div class="warning-icon-wrapper">
            <svg class="warning-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
              <line x1="12" y1="9" x2="12" y2="13"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
          </div>

          <h3 class="warning-title">Cảnh báo kiểm duyệt nội dung</h3>
          
          <p class="warning-desc">
            Hệ thống AI phát hiện một số điểm chưa đồng nhất giữa ảnh và thông tin bạn cung cấp:
          </p>

          <div class="warning-list-box">
            <ul>
              <li v-for="(msg, index) in moderationWarnings" :key="index">
                {{ msg }}
              </li>
            </ul>
          </div>

          <p class="warning-note">
            Bạn có thể chỉnh sửa lại thông tin cho chính xác, hoặc bấm <strong>"Vẫn tiếp tục đăng"</strong> nếu bạn chắc chắn thông tin này là đúng.
          </p>

          <div class="modal-actions">
            <button 
              type="button" 
              class="btn-secondary" 
              @click="closeModal" 
              :disabled="isLoading"
            >
              Sửa lại thông tin
            </button>
            <button 
              type="button" 
              class="btn-warning-submit" 
              @click="forceSubmit" 
              :disabled="isLoading"
            >
              <span v-if="isLoading" class="spinner-small"></span>
              <span>Vẫn tiếp tục đăng</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';

interface ProductModerationResponse {
  isApproved: boolean;
  hasWarnings: boolean;
  warnings: string[];
  textResult?: {
    isClean: boolean;
    flaggedWords: string[];
    warningMessage?: string;
  };
  visionResult?: {
    isMatch: boolean;
    detectedItem: string;
    warningMessage: string;
  };
}

const form = reactive({
  productName: '',
  category: 'Rau củ quả',
  price: 25000,
  unit: 'Kg'
});

const fileInputRef = ref<HTMLInputElement | null>(null);
const imageFile = ref<File | null>(null);
const imagePreview = ref<string>('');
const isDragging = ref<boolean>(false);

const isModerating = ref<boolean>(false);
const isSubmitting = ref<boolean>(false);
const isLoading = computed(() => isModerating.value || isSubmitting.value);

const loadingText = computed(() => {
  if (isModerating.value) return 'Đang quét kiểm duyệt AI...';
  if (isSubmitting.value) return 'Đang đăng sản phẩm...';
  return 'Đăng sản phẩm';
});

const showWarningModal = ref<boolean>(false);
const moderationWarnings = ref<string[]>([]);

const triggerFileInput = () => {
  if (!isLoading.value) {
    fileInputRef.value?.click();
  }
};

const handleFileChange = (e: Event) => {
  const target = e.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    processSelectedFile(target.files[0]);
  }
};

const handleFileDrop = (e: DragEvent) => {
  isDragging.value = false;
  if (e.dataTransfer && e.dataTransfer.files[0]) {
    processSelectedFile(e.dataTransfer.files[0]);
  }
};

const processSelectedFile = (file: File) => {
  if (!file.type.startsWith('image/')) {
    alert('Vui lòng chọn file hình ảnh hợp lệ (JPG, PNG, WEBP).');
    return;
  }
  imageFile.value = file;
  imagePreview.value = URL.createObjectURL(file);
};

const clearImage = () => {
  imageFile.value = null;
  if (imagePreview.value) {
    URL.revokeObjectURL(imagePreview.value);
    imagePreview.value = '';
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = '';
  }
};

const handleFormSubmit = async () => {
  if (!form.productName || !imageFile.value) {
    alert('Vui lòng nhập tên sản phẩm và chọn ảnh.');
    return;
  }

  isModerating.value = true;

  try {
    const formData = new FormData();
    formData.append('productName', form.productName);
    formData.append('productImage', imageFile.value);

    const res = await fetch('http://localhost:5000/api/moderation/check-product', {
      method: 'POST',
      body: formData
    });

    if (!res.ok) {
      throw new Error(`HTTP ${res.status}`);
    }

    const data: ProductModerationResponse = await res.json();

    if (data.hasWarnings || !data.isApproved) {
      moderationWarnings.value = data.warnings && data.warnings.length > 0
        ? data.warnings
        : ['Phát hiện độ lệch giữa hình ảnh và tên sản phẩm đã nhập.'];
      
      showWarningModal.value = true;
      return;
    }

    await executeFinalSubmit();
  } catch (error: any) {
    console.error('Lỗi khi gọi API kiểm duyệt:', error);
    const proceed = confirm('Dịch vụ AI kiểm duyệt tạm thời bận. Bạn có muốn tiếp tục đăng sản phẩm không?');
    if (proceed) {
      await executeFinalSubmit();
    }
  } finally {
    isModerating.value = false;
  }
};

const closeModal = () => {
  showWarningModal.value = false;
};

const forceSubmit = async () => {
  showWarningModal.value = false;
  await executeFinalSubmit();
};

const executeFinalSubmit = async () => {
  isSubmitting.value = true;
  try {
    const finalData = new FormData();
    finalData.append('productName', form.productName);
    finalData.append('category', form.category);
    finalData.append('price', form.price.toString());
    finalData.append('unit', form.unit);
    if (imageFile.value) {
      finalData.append('image', imageFile.value);
    }

    await fetch('http://localhost:5000/api/products', {
      method: 'POST',
      body: finalData
    });

    alert('🎉 Đăng sản phẩm thành công lên ZoneMart!');
    
    form.productName = '';
    form.price = 25000;
    clearImage();
  } catch (error) {
    console.error('Lỗi khi lưu sản phẩm:', error);
    alert('Có lỗi xảy ra khi lưu sản phẩm. Vui lòng thử lại!');
  } finally {
    isSubmitting.value = false;
  }
};
</script>

<style scoped>
.product-post-container {
  max-width: 620px;
  margin: 2rem auto;
  padding: 0 1rem;
  font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.form-card {
  background: #ffffff;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f0f0;
}

.form-header {
  margin-bottom: 1.5rem;
}

.title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.subtitle {
  font-size: 0.875rem;
  color: #64748b;
  margin: 0;
}

.form-body {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-row {
  display: flex;
  gap: 1rem;
}

.flex-1 {
  flex: 1;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #334155;
}

.required {
  color: #ef4444;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1.5px solid #cbd5e1;
  border-radius: 8px;
  font-size: 0.95rem;
  color: #1e293b;
  transition: border-color 0.2s, box-shadow 0.2s;
  box-sizing: border-box;
}

.form-input:focus {
  outline: none;
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.15);
}

.hidden-input {
  display: none;
}

.image-upload-zone {
  border: 2px dashed #cbd5e1;
  border-radius: 12px;
  padding: 1.5rem;
  text-align: center;
  cursor: pointer;
  background: #f8fafc;
  transition: all 0.2s ease;
  min-height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.image-upload-zone:hover,
.image-upload-zone.drag-over {
  border-color: #10b981;
  background: #f0fdf4;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
}

.upload-icon {
  color: #64748b;
  margin-bottom: 0.25rem;
}

.upload-text {
  font-size: 0.95rem;
  font-weight: 600;
  color: #334155;
  margin: 0;
}

.upload-hint {
  font-size: 0.75rem;
  color: #94a3b8;
}

.preview-container {
  position: relative;
  max-width: 220px;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.preview-img {
  width: 100%;
  height: 160px;
  object-fit: cover;
  display: block;
}

.remove-image-btn {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.65);
  color: #ffffff;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  transition: background 0.2s;
}

.remove-image-btn:hover {
  background: #ef4444;
}

.form-actions {
  margin-top: 0.5rem;
}

.submit-btn {
  width: 100%;
  padding: 0.9rem;
  border: none;
  border-radius: 8px;
  background: #10b981;
  color: white;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: background 0.2s, transform 0.1s;
}

.submit-btn:hover:not(:disabled) {
  background: #059669;
}

.submit-btn:disabled {
  background: #94a3b8;
  cursor: not-allowed;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2.5px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

.spinner-small {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

.warning-modal {
  background: #ffffff;
  border-radius: 16px;
  max-width: 480px;
  width: 100%;
  padding: 1.75rem;
  text-align: center;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
  border-top: 6px solid #f59e0b;
  animation: modalEnter 0.25s ease-out;
}

@keyframes modalEnter {
  from {
    opacity: 0;
    transform: translateY(12px) scale(0.97);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

.warning-icon-wrapper {
  width: 56px;
  height: 56px;
  background: #fef3c7;
  color: #d97706;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1rem auto;
}

.warning-svg {
  width: 32px;
  height: 32px;
}

.warning-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: #92400e;
  margin: 0 0 0.5rem 0;
}

.warning-desc {
  font-size: 0.9rem;
  color: #4b5563;
  margin: 0 0 1rem 0;
}

.warning-list-box {
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: 8px;
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;
  text-align: left;
}

.warning-list-box ul {
  margin: 0;
  padding-left: 1.2rem;
  color: #b45309;
  font-size: 0.875rem;
  line-height: 1.5;
}

.warning-note {
  font-size: 0.8rem;
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.modal-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
}

.btn-secondary {
  flex: 1;
  padding: 0.75rem 1rem;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background: #ffffff;
  color: #475569;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-secondary:hover:not(:disabled) {
  background: #f1f5f9;
}

.btn-warning-submit {
  flex: 1;
  padding: 0.75rem 1rem;
  border: none;
  border-radius: 8px;
  background: #f59e0b;
  color: #ffffff;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  transition: background 0.15s;
}

.btn-warning-submit:hover:not(:disabled) {
  background: #d97706;
}

.btn-secondary:disabled,
.btn-warning-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>

