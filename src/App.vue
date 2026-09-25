<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import ChannelTable from './components/ChannelTable.vue'
import ChannelManager from './components/ChannelManager.vue'
import { platforms, getPlatform } from './platforms'
import { useLocalStorage } from './composables/useLocalStorage'
import { downloadText } from './components/ChannelManager.vue'

const DEFAULT_PLATFORM = platforms[0]?.id

const themeOptions = [
  { value: 'system', label: 'Система' },
  { value: 'light', label: 'Светлая' },
  { value: 'dark', label: 'Тёмная' },
]

const channels = useLocalStorage('tsc:channels', () => [])
const settings = useLocalStorage('tsc:settings', () => ({
  intervalSeconds: 60,
  theme: 'system',
}))

function applyTheme(theme) {
  const root = document.documentElement
  if (theme === 'light' || theme === 'dark') root.dataset.theme = theme
  else delete root.dataset.theme
}

watch(() => settings.value.theme, applyTheme, { immediate: true })

channels.value = normalizeChannels(channels.value)

function normalizeChannels(input) {
  const seen = new Set()
  const out = []

  const push = (platformId, name) => {
    const platform = getPlatform(platformId)?.id || DEFAULT_PLATFORM
    const trimmed = String(name ?? '').trim()
    if (!trimmed) return
    const key = `${platform}:${trimmed.toLowerCase()}`
    if (seen.has(key)) return
    seen.add(key)
    out.push({ platform, name: trimmed })
  }

  for (const raw of input || []) {
    if (typeof raw === 'string') {
      push(DEFAULT_PLATFORM, raw)
    } else if (raw && typeof raw === 'object') {
      push(raw.platform, raw.name)
    }
  }

  return out
}

function keyOf(entry) {
  return `${entry.platform}:${entry.name.toLowerCase()}`
}

const status = reactive({})
const updating = ref(false)
const lastUpdated = ref(null)
const allFailed = ref(false)
const lastError = ref('')
const showSettings = ref(false)

const emptyStatus = {
  online: null,
  error: false,
  viewers: null,
  title: null,
  game: null,
  startedAt: null,
  avatar: null,
}

const rows = computed(() =>
  channels.value.map((entry) => {
    const st = status[keyOf(entry)] || { ...emptyStatus }
    return { ...entry, ...st }
  }),
)

const sortedRows = computed(() =>
  [...rows.value].sort((a, b) => {
    const rankA = a.error ? 2 : a.online === true ? 0 : a.online === false ? 1 : 2
    const rankB = b.error ? 2 : b.online === true ? 0 : b.online === false ? 1 : 2
    if (rankA !== rankB) return rankA - rankB
    if (rankA === 0) {
      const va = a.viewers ?? 0
      const vb = b.viewers ?? 0
      if (va !== vb) return vb - va
    }
    return a.name.localeCompare(b.name, 'ru')
  }),
)

function applyResult(entry, r) {
  if (!r) return
  status[keyOf(entry)] = r.exists
    ? {
        online: r.online,
        error: false,
        viewers: r.viewers,
        title: r.title,
        game: r.game,
        startedAt: r.startedAt ?? null,
        avatar: r.avatar ?? null,
      }
    : { ...emptyStatus, error: true }
}

async function checkOne(entry) {
  const platform = getPlatform(entry.platform)
  if (!platform) return
  status[keyOf(entry)] = { ...emptyStatus }
  try {
    const results = await platform.checkChannels([entry.name])
    const r = results[0]
    if (r) applyResult(entry, r)
  } catch {
    status[keyOf(entry)] = { ...emptyStatus, error: true }
  }
}

async function refreshAll() {
  const list = [...channels.value]
  if (!list.length) return

  updating.value = true
  allFailed.value = false
  lastError.value = ''

  const byPlatform = new Map()
  for (const entry of list) {
    if (!byPlatform.has(entry.platform)) byPlatform.set(entry.platform, [])
    byPlatform.get(entry.platform).push(entry)
  }

  const errors = []
  let okCount = 0

  for (const [platformId, entries] of byPlatform) {
    const platform = getPlatform(platformId)
    try {
      const results = await platform.checkChannels(entries.map((e) => e.name))
      for (let i = 0; i < entries.length; i++) {
        applyResult(entries[i], results[i])
      }
      okCount += 1
    } catch (e) {
      errors.push(`${platform.name}: ${e?.message || 'ошибка запроса'}`)
    }
  }

  allFailed.value = okCount === 0 && byPlatform.size > 0
  lastError.value = errors.join('; ')
  lastUpdated.value = new Date()
  updating.value = false
}

function addChannel(name, platformId) {
  const trimmed = String(name ?? '').trim()
  if (!trimmed) return
  const platform = getPlatform(platformId)?.id || DEFAULT_PLATFORM
  const key = `${platform}:${trimmed.toLowerCase()}`
  if (channels.value.some((c) => keyOf(c) === key)) return
  const entry = { platform, name: trimmed }
  channels.value.push(entry)
  checkOne(entry)
}

function removeChannel(platformId, name) {
  if (!window.confirm(`Удалить канал «${name}»?`)) return
  const idx = channels.value.findIndex(
    (c) => c.platform === platformId && c.name.toLowerCase() === String(name).toLowerCase(),
  )
  if (idx !== -1) channels.value.splice(idx, 1)
  delete status[`${platformId}:${String(name).toLowerCase()}`]
}

function importChannels(text) {
  let parsed
  try {
    parsed = JSON.parse(text)
  } catch {
    return
  }
  if (!Array.isArray(parsed)) return

  for (const item of parsed) {
    if (item && typeof item === 'object') {
      addChannel(item.name, item.platform)
    } else if (typeof item === 'string') {
      addChannel(item, DEFAULT_PLATFORM)
    }
  }
}

function exportChannelsJson() {
  return JSON.stringify(channels.value, null, 2)
}

let timer = null

function schedule() {
  clearInterval(timer)
  timer = setInterval(refreshAll, Math.max(5, Number(settings.value.intervalSeconds) || 60) * 1000)
}

watch(() => settings.value.intervalSeconds, schedule)

onMounted(() => {
  schedule()
  refreshAll()
})

onBeforeUnmount(() => clearInterval(timer))

const lastUpdatedText = computed(() =>
  lastUpdated.value
    ? lastUpdated.value.toLocaleTimeString('ru-RU')
    : updating.value
      ? 'обновляю…'
      : 'ещё не обновлялось',
)
</script>

<template>
  <div class="layout">
    <header>
      <h1>Стримеры онлайн</h1>
      <div class="toolbar">
        <span class="status" :class="{ updating }">
          <span v-if="updating" class="spinner" />{{ lastUpdatedText }}
        </span>
        <button
          type="button"
          class="btn btn-secondary icon-btn"
          title="Обновить сейчас"
          :disabled="updating || !channels.length"
          @click="refreshAll"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
          </svg>
        </button>
        <div class="settings-wrap">
          <button
            type="button"
            class="btn btn-secondary icon-btn"
            :class="{ active: showSettings }"
            title="Настройки"
            @click="showSettings = !showSettings"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="3" />
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
            </svg>
          </button>
          <div v-if="showSettings" class="settings-menu">
            <label class="field settings-refresh">
              <span>Обновлять каждые</span>
              <input v-model.number="settings.intervalSeconds" type="number" min="5" step="5" />
              <span>сек</span>
            </label>
            <div class="theme-switch">
              <button
                v-for="option in themeOptions"
                :key="option.value"
                type="button"
                class="theme-btn"
                :class="{ active: settings.theme === option.value }"
                @click="settings.theme = option.value"
              >
                {{ option.label }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </header>
    <div v-if="showSettings" class="settings-backdrop" @click="showSettings = false"></div>

    <ChannelManager
      :channels="channels"
      @add="addChannel"
      @remove="removeChannel"
      @import="importChannels"
      @export="downloadText('channels.json', exportChannelsJson())"
    />

    <ChannelTable :rows="sortedRows" @remove="removeChannel" />
  </div>
</template>