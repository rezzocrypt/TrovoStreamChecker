<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import ChannelTable from './components/ChannelTable.vue'
import ChannelManager from './components/ChannelManager.vue'
import { platforms, getPlatform } from './platforms'
import { useLocalStorage } from './composables/useLocalStorage'
import { downloadText } from './utils/files'

const DEFAULT_PLATFORM = platforms[0]?.id

const channels = useLocalStorage('tsc:channels', () => [])
const settings = useLocalStorage('tsc:settings', () => ({
  intervalSeconds: 60,
  autoRefresh: true,
}))

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
  if (settings.value.autoRefresh) {
    timer = setInterval(refreshAll, Math.max(5, Number(settings.value.intervalSeconds) || 60) * 1000)
  }
}

watch(() => settings.value.autoRefresh, schedule)
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
        <button type="button" class="btn btn-primary" :disabled="updating || !channels.length" @click="refreshAll">
          {{ updating ? 'Обновляю…' : 'Обновить сейчас' }}
        </button>
        <label class="checkbox">
          <input v-model="settings.autoRefresh" type="checkbox" />
          Автообновление
        </label>
        <label class="field">
          каждые
          <input
            v-model.number="settings.intervalSeconds"
            type="number"
            min="5"
            step="5"
            :disabled="!settings.autoRefresh"
          />
          <span>сек</span>
        </label>
      </div>
    </header>

    <div class="bar">
      <span class="status" :class="{ updating }">
        <span v-if="updating" class="spinner" />{{ lastUpdatedText }}
      </span>
      <span v-if="allFailed" class="alert">{{ lastError }}</span>
    </div>

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