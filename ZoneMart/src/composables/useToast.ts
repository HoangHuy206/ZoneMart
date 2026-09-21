import { ref } from 'vue';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastItem {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number; // milliseconds, default: 3500ms
  createdAt: number;
  icon?: string;
}

const toasts = ref<ToastItem[]>([]);

export function useToast() {
  const show = (options: {
    type?: ToastType;
    title?: string;
    message: string;
    duration?: number;
    icon?: string;
  }) => {
    const id = `toast_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
    const newToast: ToastItem = {
      id,
      type: options.type || 'info',
      title: options.title,
      message: options.message,
      duration: options.duration ?? 3800,
      createdAt: Date.now(),
      icon: options.icon,
    };

    toasts.value.push(newToast);

    // Tự động đóng sau khi hết duration nếu duration > 0
    if (newToast.duration && newToast.duration > 0) {
      setTimeout(() => {
        remove(id);
      }, newToast.duration);
    }

    return id;
  };

  const remove = (id: string) => {
    const index = toasts.value.findIndex((t) => t.id === id);
    if (index !== -1) {
      toasts.value.splice(index, 1);
    }
  };

  const clearAll = () => {
    toasts.value = [];
  };

  const success = (message: string, title: string = 'Thành công', duration?: number) => {
    return show({ type: 'success', title, message, duration });
  };

  const error = (message: string, title: string = 'Đã có lỗi', duration?: number) => {
    return show({ type: 'error', title, message, duration });
  };

  const warning = (message: string, title: string = 'Lưu ý', duration?: number) => {
    return show({ type: 'warning', title, message, duration });
  };

  const info = (message: string, title: string = 'Thông báo', duration?: number) => {
    return show({ type: 'info', title, message, duration });
  };

  return {
    toasts,
    show,
    remove,
    clearAll,
    success,
    error,
    warning,
    info,
  };
}

// Lắng nghe sự kiện toàn cục window 'zonemart:toast' để bất kỳ file script nào cũng phát được thông báo
if (typeof window !== 'undefined') {
  window.addEventListener('zonemart:toast', (e: any) => {
    const detail = e?.detail;
    if (!detail || !detail.message) return;
    const toast = useToast();
    toast.show({
      type: detail.type || 'info',
      title: detail.title,
      message: detail.message,
      duration: detail.duration,
      icon: detail.icon,
    });
  });
}
