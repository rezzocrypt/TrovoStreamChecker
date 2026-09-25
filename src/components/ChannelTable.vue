<script setup>
import { getPlatform } from '../platforms'

defineProps({
  rows: { type: Array, required: true },
})

defineEmits(['remove'])

function formatViewers(v) {
  if (v === null || v === undefined) return '—'
  return Number(v).toLocaleString('ru-RU')
}

function formatText(v) {
  return v ?? '—'
}

function parseKickTime(v) {
  return /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}/.test(v) ? v.replace(' ', 'T') + 'Z' : v
}

function formatStart(v) {
  if (!v) return '—'
  const date = new Date(parseKickTime(v))
  if (Number.isNaN(date.getTime())) return '—'
  const time = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  if (date.toDateString() === new Date().toDateString()) return time
  const opts = { day: 'numeric', month: 'short' }
  if (date.getFullYear() !== new Date().getFullYear()) opts.year = 'numeric'
  return `${time}, ${date.toLocaleDateString('ru-RU', opts)}`
}

function initial(name) {
  return String(name).charAt(0).toUpperCase()
}

function platformById(id) {
  return getPlatform(id)
}
</script>

<template>
  <table>
    <thead>
      <tr>
        <th class="col-platform"></th>
        <th class="col-status">
          <svg class="th-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4.9 19.1C1 15.2 1 8.8 4.9 4.9" />
            <path d="M7.8 16.2c-2.3-2.3-2.3-6.1 0-8.5" />
            <path d="M16.2 7.8c2.3 2.3 2.3 6.1 0 8.5" />
            <path d="M19.1 4.9C23 8.8 23 15.2 19.1 19.1" />
            <path d="M12 22v-12" />
            <circle cx="12" cy="8" r="2" />
          </svg>
        </th>
        <th class="col-channel">Стример</th>
        <th class="col-viewers">
          <svg class="th-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
            <path d="M16 3.13a4 4 0 0 1 0 7.75" />
          </svg>
        </th>
        <th class="col-start">Начало</th>
        <th class="col-game">Игра</th>
        <th class="col-title">Название</th>
        <th class="col-remove"></th>
      </tr>
    </thead>
    <tbody>
      <tr v-if="!rows.length">
        <td colspan="7" class="empty">
          Каналов нет — добавьте их в списке выше или импортируйте channels.json.
        </td>
      </tr>
      <tr v-for="row in rows" :key="`${row.platform}:${row.name.toLowerCase()}`">
        <td class="col-platform" :title="platformById(row.platform)?.name">
          <span :class="`platform-icon platform-${row.platform}`"></span>
        </td>
        <td class="col-status">
          <span v-if="row.error" class="dot dot-error" title="Канал не найден"></span>
          <span v-else-if="row.online === null" class="dot dot-updating" title="обновляю…"></span>
          <span v-else-if="row.online" class="dot dot-live" title="Онлайн"></span>
          <span v-else class="dot dot-offline" title="Оффлайн"></span>
        </td>
        <td class="col-channel" :title="row.name">
          <img v-if="row.avatar" :src="row.avatar" class="avatar" />
          <span v-else class="avatar avatar-fallback">{{ initial(row.name) }}</span>
          <a
            :href="platformById(row.platform)?.channelUrl?.(row.name) || '#'"
            target="_blank"
            rel="noopener"
          >
            {{ row.name }}
          </a>
        </td>
        <td class="col-viewers" :title="row.viewers != null ? formatViewers(row.viewers) : null">{{ formatViewers(row.viewers) }}</td>
        <td class="col-start" :title="row.startedAt ? formatStart(row.startedAt) : null">{{ formatStart(row.startedAt) }}</td>
        <td class="col-game" :title="row.game ? formatText(row.game) : null">{{ formatText(row.game) }}</td>
        <td class="col-title" :title="row.title ? formatText(row.title) : null">{{ formatText(row.title) }}</td>
        <td class="col-remove">
          <button type="button" class="tag-remove" title="Убрать канал" @click="$emit('remove', row.platform, row.name)">
            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 6h18" />
              <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
              <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
              <path d="M10 11v6" />
              <path d="M14 11v6" />
            </svg>
          </button>
        </td>
      </tr>
    </tbody>
  </table>
</template>