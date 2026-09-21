/**
 * ============================================================================
 * CART SERVICE - KẾT NỐI GIỎ HÀNG VỚI DATABASE MONGODB ATLAS
 * Backend: ASP.NET Core (.NET 9 - C#) tại http://localhost:5000/api/cart
 * ============================================================================
 */
import type { CartStoreGroup } from '../composables/useCart';

const API_BASE = '/api/cart';

export interface SuggestedProductItem {
  id: string;
  name: string;
  price: number;
  originalPrice?: number;
  image: string;
  unit?: string;
  storeId: string;
  storeName: string;
  distanceKm: number;
  deliveryTime: string;
}

export const cartService = {
  /**
   * Lấy giỏ hàng của người dùng trực tiếp từ Database MongoDB Atlas
   */
  async fetchUserCart(userId: string): Promise<{
    stores: CartStoreGroup[];
    voucherCode?: string;
  } | null> {
    try {
      const res = await fetch(`${API_BASE}/${encodeURIComponent(userId)}`);
      if (res.ok) {
        const data = await res.json();
        if (data.success && data.data) {
          const stores: CartStoreGroup[] = (data.data.stores || []).map(
            (s: any) => ({
              storeId: s.storeId,
              storeName: s.storeName,
              distanceKm: s.distanceKm,
              deliveryTime: s.deliveryTime,
              note: s.note,
              items: (s.items || []).map((i: any) => ({
                id: i.id,
                name: i.name,
                price: i.price,
                originalPrice: i.originalPrice,
                quantity: i.quantity,
                image: i.image,
                unit: i.unit,
                selected: i.selected ?? true,
              })),
            }),
          );
          return {
            stores,
            voucherCode: data.data.voucherCode || '',
          };
        }
      }
    } catch (e) {
      console.warn('Lỗi kết nối Cart API, sử dụng dữ liệu cục bộ:', e);
    }
    return null;
  },

  /**
   * Lưu & đồng bộ giỏ hàng vào Database MongoDB Atlas
   */
  async saveUserCart(
    userId: string,
    stores: CartStoreGroup[],
    voucherCode?: string,
  ): Promise<boolean> {
    try {
      const res = await fetch(`${API_BASE}/${encodeURIComponent(userId)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          stores: stores.map((s) => ({
            storeId: s.storeId,
            storeName: s.storeName,
            distanceKm: s.distanceKm,
            deliveryTime: s.deliveryTime,
            note: s.note,
            items: s.items.map((i) => ({
              id: i.id,
              name: i.name,
              price: i.price,
              originalPrice: i.originalPrice,
              quantity: i.quantity,
              image: i.image,
              unit: i.unit,
              selected: i.selected,
            })),
          })),
          voucherCode,
        }),
      });
      if (res.ok) {
        const data = await res.json();
        return data.success;
      }
    } catch (e) {
      console.warn('Không thể đồng bộ giỏ hàng lên database lúc này:', e);
    }
    return false;
  },

  /**
   * Lấy danh sách sản phẩm gợi ý mua kèm (Cross-sell) từ Database thật
   */
  async fetchSuggestedProducts(): Promise<SuggestedProductItem[]> {
    try {
      const res = await fetch(`${API_BASE}/suggested`);
      if (res.ok) {
        const data = await res.json();
        if (data.success && Array.isArray(data.data) && data.data.length > 0) {
          return data.data;
        }
      }
    } catch (e) {
      console.warn(
        'Lỗi tải sản phẩm gợi ý từ API, sử dụng danh sách chuẩn:',
        e,
      );
    }

    // Fallback chuẩn nếu ngắt kết nối
    return [
      {
        storeId: '65f01234567890abcdef0001',
        storeName: 'ZoneMart Bách Hóa Cầu Giấy',
        distanceKm: 1.2,
        deliveryTime: '15 - 20 phút',
        id: '65f099887766554433221101',
        name: 'Thịt Bò Mỹ Nhập Khẩu Thượng Hạng (Khay 500g)',
        price: 185000,
        originalPrice: 220000,
        unit: 'Khay 500g',
        image:
          'https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=400&q=80',
      },
      {
        storeId: '65f01234567890abcdef0001',
        storeName: 'ZoneMart Bách Hóa Cầu Giấy',
        distanceKm: 1.2,
        deliveryTime: '15 - 20 phút',
        id: '65f099887766554433221102',
        name: 'Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo',
        price: 190000,
        originalPrice: 225000,
        unit: 'Túi 5kg',
        image:
          'https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=400&q=80',
      },
      {
        storeId: '65f01234567890abcdef0002',
        storeName: 'Siêu Thị Trái Cây Xanh VietGAP',
        distanceKm: 2.5,
        deliveryTime: '20 - 25 phút',
        id: '65f099887766554433221103',
        name: 'Hộp Dâu Tây Đà Lạt Tươi Ngọt Chuẩn VietGAP (500g)',
        price: 95000,
        originalPrice: 125000,
        unit: 'Hộp 500g',
        image:
          'https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=400&q=80',
      },
      {
        storeId: '65f01234567890abcdef0003',
        storeName: 'Tiệm Bánh Mì Zone & Nước Ép',
        distanceKm: 2.8,
        deliveryTime: '15 - 20 phút',
        id: '65f099887766554433221104',
        name: 'Combo Bánh Mì Chảo Nóng Hổi Kèm Pate Đặc Biệt',
        price: 45000,
        originalPrice: 55000,
        unit: 'Phần 1 người',
        image:
          'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80',
      },
    ];
  },
};
