<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import ChannelTable from './components/ChannelTable.vue'
import ChannelManager from './components/ChannelManager.vue'
import SvgIcon from './components/SvgIcon.vue'
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

function resolveEntry(name, platformId) {
  const trimmed = String(name ?? '').trim()
  if (!trimmed) return null
  return { platform: getPlatform(platformId)?.id || DEFAULT_PLATFORM, name: trimmed }
}

function normalizeChannels(input) {
  const seen = new Set()
  const out = []

  for (const raw of input || []) {
    const entry = typeof raw === 'string' ? resolveEntry(raw, DEFAULT_PLATFORM) : resolveEntry(raw?.name, raw?.platform)
    if (!entry) continue
    const key = keyOf(entry)
    if (seen.has(key)) continue
    seen.add(key)
    out.push(entry)
  }

  return out
}

function keyOf(entry) {
  return `${entry.platform}:${entry.name.toLowerCase()}`
}

const status = reactive({})
const updating = ref(false)
const lastUpdated = ref(null)
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
  channels.value.map((entry) => ({ ...entry, ...(status[keyOf(entry)] || { ...emptyStatus }) })),
)

function rank(row) {
  if (row.error || row.online === null) return 2
  return row.online ? 0 : 1
}

const sortedRows = computed(() =>
  [...rows.value].sort((a, b) => {
    const rankA = rank(a)
    const rankB = rank(b)
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

  const byPlatform = new Map()
  for (const entry of list) {
    if (!byPlatform.has(entry.platform)) byPlatform.set(entry.platform, [])
    byPlatform.get(entry.platform).push(entry)
  }

  for (const [platformId, entries] of byPlatform) {
    const platform = getPlatform(platformId)
    try {
      const results = await platform.checkChannels(entries.map((e) => e.name))
      for (let i = 0; i < entries.length; i++) {
        applyResult(entries[i], results[i])
      }
    } catch {
      // ошибка запроса — старые данные остаются в кэше
    }
  }

  lastUpdated.value = new Date()
  updating.value = false
}

function addChannel(name, platformId) {
  const entry = resolveEntry(name, platformId)
  if (!entry) return
  const key = keyOf(entry)
  if (channels.value.some((c) => keyOf(c) === key)) return
  channels.value.push(entry)
  checkOne(entry)
}

function removeChannel(platformId, name) {
  if (!window.confirm(`Удалить канал «${name}»?`)) return
  const key = keyOf({ platform: platformId, name })
  const idx = channels.value.findIndex((c) => keyOf(c) === key)
  if (idx !== -1) channels.value.splice(idx, 1)
  delete status[key]
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
          <SvgIcon name="refresh" />
        </button>
        <div class="settings-wrap">
          <button
            type="button"
            class="btn btn-secondary icon-btn"
            :class="{ active: showSettings }"
            title="Настройки"
            @click="showSettings = !showSettings"
          >
            <SvgIcon name="settings" />
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