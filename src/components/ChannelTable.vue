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
  const sameDay = date.toDateString() === new Date().toDateString()
  return sameDay
    ? date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
    : date.toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
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
        <th class="col-platform">Площадка</th>
        <th class="col-channel">Стример</th>
        <th class="col-status">Онлайн?</th>
        <th class="col-viewers">Зрителей</th>
        <th class="col-start">Начало</th>
        <th class="col-game">Игра</th>
        <th class="col-title">Название трансляции</th>
      </tr>
    </thead>
    <tbody>
      <tr v-if="!rows.length">
        <td colspan="7" class="empty">
          Каналов нет — добавьте их в списке выше или импортируйте channels.json.
        </td>
      </tr>
      <tr v-for="row in rows" :key="`${row.platform}:${row.name.toLowerCase()}`">
        <td class="col-platform">
          <span :class="`platform-icon platform-${row.platform}`"></span>
        </td>
        <td class="col-channel">
          <img v-if="row.avatar" :src="row.avatar" class="avatar" alt="" />
          <span v-else class="avatar avatar-fallback">{{ initial(row.name) }}</span>
          <a
            :href="platformById(row.platform)?.channelUrl?.(row.name) || '#'"
            target="_blank"
            rel="noopener"
          >
            {{ row.name }}
          </a>
          <button type="button" class="tag-remove" title="Убрать канал" @click="$emit('remove', row.platform, row.name)">
            ×
          </button>
        </td>
        <td class="col-status">
          <span v-if="row.error" class="badge badge-na">Канал не найден</span>
          <span v-else-if="row.online === null" class="badge badge-updating">обновляю…</span>
          <span v-else-if="row.online" class="badge badge-live">● онлайн</span>
          <span v-else class="badge badge-offline">✕ оффлайн</span>
        </td>
        <td class="col-viewers">{{ formatViewers(row.viewers) }}</td>
        <td class="col-start">{{ formatStart(row.startedAt) }}</td>
        <td class="col-game">{{ formatText(row.game) }}</td>
        <td class="col-title" :title="formatText(row.title)">{{ formatText(row.title) }}</td>
      </tr>
    </tbody>
  </table>
</template>