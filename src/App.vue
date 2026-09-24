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

const rows = computed(() =>
  channels.value.map((entry) => {
    const st = status[keyOf(entry)] || {
      online: null,
      error: false,
      viewers: null,
      title: null,
      game: null,
      avatar: null,
    }
    return { ...entry, ...st }
  }),
)

const sortedRows = computed(() =>
  [...rows.value].sort((a, b) => {
    const oa = a.error ? -1 : a.online === true ? 1 : a.online === false ? 0 : -1
    const ob = b.error ? -1 : b.online === true ? 1 : b.online === false ? 0 : -1
    if (oa !== ob) return ob - oa
    return a.name.localeCompare(b.name, 'ru')
  }),
)

async function refreshAll() {
  const list = [...channels.value]
  if (!list.length) return

  updating.value = true
  allFailed.value = false
  lastError.value = ''

  const empty = { online: null, error: false, viewers: null, title: null, game: null, avatar: null }
  for (const entry of list) {
    status[keyOf(entry)] = { ...empty }
  }

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
        const r = results[i]
        status[keyOf(entries[i])] = r.exists
          ? {
              online: r.online,
              error: false,
              viewers: r.viewers,
              title: r.title,
              game: r.game,
              avatar: r.avatar ?? null,
            }
          : { online: null, error: true, viewers: null, title: null, game: null, avatar: null }
      }
      okCount += 1
    } catch (e) {
      errors.push(`${platform.name}: ${e?.message || 'ошибка запроса'}`)
      for (const entry of entries) {
        status[keyOf(entry)] = { online: null, error: true, viewers: null, title: null, game: null, avatar: null }
      }
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
  channels.value.push({ platform, name: trimmed })
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